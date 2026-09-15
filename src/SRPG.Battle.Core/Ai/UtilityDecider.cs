using System.Collections.Generic;
using SRPG.Battle.Core.Commands;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Loop;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 유틸리티 AI 의사결정기. 공격 경로(스킬 궁극▶액티브▶평타 → 후보 생성 → 영향력 맵 점수 → 상위 K 1-ply 가산)와
    /// 지원 경로(SupportPlanner: 힐/버프/실드)를 각각 평가해 같은 척도로 비교, 높은 쪽을 채택한다.
    /// 동점·기본은 공격 경로(결정론). 결정 단계는 난수 없음. (참조: 04_전투_시스템.md AI 의사코드, 17 스킬, AI-030/031/120/140/150)
    /// </summary>
    public sealed class UtilityDecider : IActionDecider
    {
        /// <summary>행동자의 행동을 결정해 커맨드 목록으로 반환한다.</summary>
        public IReadOnlyList<ICommand> Decide(BattleState state, Unit actor)
        {
            Blackboard blackboard = new Blackboard(state, actor); // 위협 영향력맵: 공격·지원 평가가 공유(턴당 1회)

            // 제어 상태: 침묵=액티브/궁극 사용 불가(평타만) → 지원(힐/버프=스킬)도 불가.
            // 도발=공격 대상만 도발자로 강제(CandidateGenerator 내부에서 처리). 회복/버프 등 비공격 행동은 도발 제약 없음(AI-171).
            bool silenced = ControlStatus.IsSilenced(actor);

            SkillDef offensiveSkill = silenced ? null : ChooseOffensiveSkill(actor);
            int range = offensiveSkill == null ? actor.EntryStat.Get(StatType.Rng) : offensiveSkill.Range;
            int skillPower = offensiveSkill == null ? Permille.BASE : PrimaryDamagePower(offensiveSkill);

            List<Candidate> candidates = CandidateGenerator.Generate(state, actor, range);
            ScoredCandidate bestOffense = SelectBest(state, actor, candidates, blackboard, skillPower);

            SupportPlan support = silenced ? null : SupportPlanner.Plan(state, actor, blackboard);
            if (PrefersSupport(support, bestOffense))
            {
                return SupportCommands(actor, support);
            }

            return BuildCommands(actor, bestOffense, offensiveSkill);
        }

        // 지원이 공격보다 점수가 높을 때만 지원 채택(동점·공격없음 처리 포함, 결정론).
        private static bool PrefersSupport(SupportPlan support, ScoredCandidate offense)
        {
            if (support == null)
            {
                return false;
            }

            if (offense == null)
            {
                return true;
            }

            return support.Score > offense.Score;
        }

        // 궁극(게이지 충전·공격형) ▶ 액티브(쿨다운 0·공격형) ▶ 평타(null)
        private static SkillDef ChooseOffensiveSkill(Unit actor)
        {
            if (actor.IsUltReady() && actor.UltSkill.IsOffensive)
            {
                return actor.UltSkill;
            }

            SkillDef active = actor.ActiveSkill;
            if (active != null && active.IsOffensive && actor.GetCooldown(active.Id) == 0)
            {
                return active;
            }

            return null; // 평타
        }

        private static int PrimaryDamagePower(SkillDef skill)
        {
            IReadOnlyList<SkillEffect> effects = skill.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                if (effects[index].Type == EffectType.Damage)
                {
                    return effects[index].Power;
                }
            }

            return Permille.BASE;
        }

        private static ScoredCandidate SelectBest(BattleState state, Unit actor, List<Candidate> candidates, Blackboard blackboard, int skillPower)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            int[] scores = new int[candidates.Count];
            for (int index = 0; index < candidates.Count; index++)
            {
                scores[index] = UtilityScorer.Score(state, actor, candidates[index], blackboard, skillPower);
            }

            List<int> ranked = RankIndices(candidates, scores);
            int lookaheadCount = ranked.Count < AiConst.LookaheadK ? ranked.Count : AiConst.LookaheadK;
            for (int order = 0; order < lookaheadCount; order++)
            {
                int index = ranked[order];
                int delta = LookaheadEvaluator.BoardDelta(state, actor, candidates[index]);
                scores[index] += AiConst.LookaheadWeight * delta / Permille.BASE;
            }

            return PickHighest(candidates, scores);
        }

        private static List<int> RankIndices(List<Candidate> candidates, int[] scores)
        {
            List<int> indices = new List<int>(candidates.Count);
            for (int index = 0; index < candidates.Count; index++)
            {
                indices.Add(index);
            }

            indices.Sort((a, b) =>
            {
                if (scores[a] != scores[b])
                {
                    return scores[a] > scores[b] ? -1 : 1;
                }

                return candidates[a].GenerationIndex.CompareTo(candidates[b].GenerationIndex);
            });

            return indices;
        }

        private static ScoredCandidate PickHighest(List<Candidate> candidates, int[] scores)
        {
            Candidate best = null;
            int bestScore = int.MinValue;
            for (int index = 0; index < candidates.Count; index++)
            {
                Candidate candidate = candidates[index];
                bool isBetter = best == null
                    || scores[index] > bestScore
                    || (scores[index] == bestScore && candidate.GenerationIndex < best.GenerationIndex);

                if (isBetter)
                {
                    best = candidate;
                    bestScore = scores[index];
                }
            }

            return new ScoredCandidate(best, bestScore);
        }

        private static IReadOnlyList<ICommand> SupportCommands(Unit actor, SupportPlan support)
        {
            List<ICommand> commands = new List<ICommand>();
            commands.Add(new SkillCommand(actor, support.Skill, support.Target));
            return commands;
        }

        private static IReadOnlyList<ICommand> BuildCommands(Unit actor, ScoredCandidate scored, SkillDef chosenSkill)
        {
            List<ICommand> commands = new List<ICommand>();
            Candidate best = scored == null ? null : scored.Candidate;
            if (best == null)
            {
                commands.Add(new WaitCommand());
                return commands;
            }

            if (best.Destination != actor.Pos)
            {
                commands.Add(new MoveCommand(actor, best.Destination));
            }

            if (best.Target != null)
            {
                if (chosenSkill == null)
                {
                    commands.Add(new AttackCommand(actor, best.Target));
                }
                else
                {
                    commands.Add(new SkillCommand(actor, chosenSkill, best.Target));
                }
            }

            if (commands.Count == 0)
            {
                commands.Add(new WaitCommand());
            }

            return commands;
        }
    }
}
