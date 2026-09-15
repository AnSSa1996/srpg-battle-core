using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 지원 스킬(힐/버프/실드) 자동화. 사용 가능한 비공격·아군대상 스킬(궁극▶액티브)을 골라,
    /// 사거리 안 아군을 1차 대상 후보로 점수화해 최고점 1건을 계획한다. 난수 없음·고정 순회(슬롯). (AI-020/120)
    /// 회복은 과회복(overheal)을 가치로 치지 않아 다친 아군을 우선한다.
    /// v1: 제자리 시전(이동 후 지원은 후속), 아군 대상(Self/SingleAlly/AoeAlly)만(적 대상 순수 디버프는 후속).
    /// (참조: Docs/01_전투/17_스킬_시스템.md, 25_AI_스코어링.md)
    /// </summary>
    public static class SupportPlanner
    {
        /// <summary>행동자의 최선 지원 행동을 계획한다(쓸 만한 지원 스킬·대상이 없으면 null).</summary>
        public static SupportPlan Plan(BattleState state, Unit actor, Blackboard blackboard)
        {
            SkillDef skill = ChooseSupportSkill(actor);
            if (skill == null)
            {
                return null;
            }

            List<Unit> primaries = AllyTargetsInRange(state, actor, skill);
            SupportPlan best = null;
            for (int index = 0; index < primaries.Count; index++)
            {
                Unit primary = primaries[index];
                int score = ScorePrimary(state, actor, skill, primary, blackboard);
                if (best == null || score > best.Score)
                {
                    best = new SupportPlan(skill, primary, score);
                }
            }

            return best;
        }

        // 궁극(게이지 충전·비공격·아군대상) ▶ 액티브(쿨다운 0·비공격·아군대상)
        private static SkillDef ChooseSupportSkill(Unit actor)
        {
            if (actor.IsUltReady() && IsAllySupport(actor.UltSkill))
            {
                return actor.UltSkill;
            }

            SkillDef active = actor.ActiveSkill;
            if (active != null && IsAllySupport(active) && actor.GetCooldown(active.Id) == 0)
            {
                return active;
            }

            return null;
        }

        private static bool IsAllySupport(SkillDef skill)
        {
            if (skill == null || skill.IsOffensive)
            {
                return false;
            }

            return skill.Targeting == TargetingType.Self
                || skill.Targeting == TargetingType.SingleAlly
                || skill.Targeting == TargetingType.AoeAlly;
        }

        // 1차 대상 후보: Self 면 시전자만, 그 외엔 사거리 안 생존 아군(슬롯 순서). 시전자 자신도 포함(자가 지원).
        private static List<Unit> AllyTargetsInRange(BattleState state, Unit actor, SkillDef skill)
        {
            List<Unit> allies = new List<Unit>();
            if (skill.Targeting == TargetingType.Self)
            {
                allies.Add(actor);
                return allies;
            }

            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.IsAlive() == false || unit.Side != actor.Side)
                {
                    continue;
                }

                if (GridDistance.Manhattan(actor.Pos, unit.Pos) <= skill.Range)
                {
                    allies.Add(unit);
                }
            }

            return allies;
        }

        private static int ScorePrimary(BattleState state, Unit actor, SkillDef skill, Unit primary, Blackboard blackboard)
        {
            List<Unit> targets = ResolveSupportTargets(state, actor, skill, primary);
            int value = 0;
            for (int index = 0; index < targets.Count; index++)
            {
                value += EffectsValue(actor, targets[index], skill);
            }

            // 제자리 시전이므로 시전자 현재 위치의 위협을 자기위험으로 감점(공격 후보와 동일 척도).
            int actorMaxHp = actor.EntryStat.Get(StatType.Hp);
            int riskNorm = NormalizeByMax(blackboard.ThreatAt(actor.Pos), actorMaxHp);
            value -= Weighted(actor, Consideration.SelfRisk, riskNorm);
            return value;
        }

        // SkillCommand 의 대상 해석과 동일: Self/SingleAlly 는 1차 대상, AoeAlly 는 범위 내 아군 전부.
        private static List<Unit> ResolveSupportTargets(BattleState state, Unit actor, SkillDef skill, Unit primary)
        {
            List<Unit> targets = new List<Unit>();
            if (skill.Targeting == TargetingType.AoeAlly)
            {
                List<Coord> tiles = AoeShape.GetTiles(skill.Aoe, primary.Pos, actor.Facing, skill.AoeSize);
                HashSet<Coord> tileSet = new HashSet<Coord>(tiles);
                IReadOnlyList<Unit> units = state.Units;
                for (int index = 0; index < units.Count; index++)
                {
                    Unit unit = units[index];
                    if (unit.IsAlive() && unit.Side == actor.Side && tileSet.Contains(unit.Pos))
                    {
                        targets.Add(unit);
                    }
                }

                return targets;
            }

            targets.Add(primary); // Self / SingleAlly
            return targets;
        }

        private static int EffectsValue(Unit actor, Unit target, SkillDef skill)
        {
            StatBlock casterStat = StatResolver.EffectiveStat(actor);
            int targetMaxHp = target.EntryStat.Get(StatType.Hp);
            int sum = 0;
            IReadOnlyList<SkillEffect> effects = skill.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                sum += EffectValue(actor, casterStat, target, targetMaxHp, effects[index]);
            }

            return sum;
        }

        private static int EffectValue(Unit actor, StatBlock casterStat, Unit target, int targetMaxHp, SkillEffect effect)
        {
            if (effect.Type == EffectType.Heal)
            {
                return HealValue(actor, casterStat, target, targetMaxHp, effect);
            }

            if (effect.Type == EffectType.Shield)
            {
                return Weighted(actor, Consideration.Defend, NormalizeByMax(effect.Value, targetMaxHp));
            }

            if (effect.Type == EffectType.ApplyStatus)
            {
                return Weighted(actor, Consideration.Defend, AiConst.SupportBuffNorm);
            }

            if (effect.Type == EffectType.GaugeMod)
            {
                return Weighted(actor, Consideration.Defend, NormalizeByMax(effect.Value, CombatConst.ULT_GAUGE_MAX));
            }

            return 0;
        }

        // 회복 가치는 "실제로 채우는 양"만 센다(과회복=0) → 다친 아군 우선·풀피 아군 회피. SkillCommand 의 힐 산식과 일치.
        private static int HealValue(Unit actor, StatBlock casterStat, Unit target, int targetMaxHp, SkillEffect effect)
        {
            long magic = casterStat.Get(StatType.Mag);
            int heal = (int)(magic * effect.Power / Permille.BASE);
            if (heal < CombatConst.MIN_HEAL)
            {
                heal = CombatConst.MIN_HEAL;
            }

            int missing = targetMaxHp - target.Hp;
            int effective = heal < missing ? heal : missing;
            if (effective < 0)
            {
                effective = 0;
            }

            return Weighted(actor, Consideration.Heal, NormalizeByMax(effective, targetMaxHp));
        }

        private static int Weighted(Unit actor, Consideration consideration, int norm)
        {
            return AiWeights.FinalWeight(actor.Class, actor.Stance, consideration) * norm / Permille.BASE;
        }

        private static int NormalizeByMax(int value, int maxValue)
        {
            if (maxValue <= 0)
            {
                return 0;
            }

            int norm = value * Permille.BASE / maxValue;
            return norm > Permille.BASE ? Permille.BASE : norm;
        }
    }
}
