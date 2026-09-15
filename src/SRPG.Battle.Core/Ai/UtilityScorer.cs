using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Loop;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 후보 점수 = Σ(가중치 × 정규화 고려요소) / 1000 (정수). 자기위협(SelfRisk)은 감점.
    /// 전부 정수·고정 순회·난수 없음 → 같은 상태면 같은 점수. (참조: AI-100~130)
    /// M6a 고려요소: Damage·Kill·Flank·Focus·Approach·SelfRisk. (위치/보호/회복·1-ply 는 M6b)
    /// 목표 추구(거점 점령·호위 추출)는 목표 타일이 있는 스테이지에서만 가산(없으면 0 → 일반 전투 점수 불변). (CMB-512)
    /// </summary>
    public static class UtilityScorer
    {
        /// <summary>후보의 유틸리티 점수를 산출한다(위협은 영향력 맵 Blackboard 에서 읽음). skillPower 는 평가할 스킬 배율(‰).</summary>
        public static int Score(BattleState state, Unit actor, Candidate candidate, Blackboard blackboard, int skillPowerPerMille = Permille.BASE)
        {
            StatBlock actorStat = StatResolver.EffectiveStat(actor);
            int actorMaxHp = actor.EntryStat.Get(StatType.Hp);
            HeroClass heroClass = actor.Class;
            Stance stance = actor.Stance;

            int score = 0;

            if (candidate.Target != null)
            {
                score += ScoreAttack(actorStat, candidate, heroClass, stance, skillPowerPerMille);
            }

            Unit nearest = NearestEnemy(state, actor);
            if (nearest != null)
            {
                int approachNorm = ApproachNorm(candidate.Destination, nearest.Pos, state.Map);
                score += Weighted(heroClass, stance, Consideration.Approach, approachNorm);
            }

            int riskNorm = NormalizeByMax(blackboard.ThreatAt(candidate.Destination), actorMaxHp);
            score -= Weighted(heroClass, stance, Consideration.SelfRisk, riskNorm);

            score += ScoreObjective(state, actor, candidate); // 거점 점령/호위 추출 추구(목표 타일 있을 때만)

            return score;
        }

        // 목표 타일 추구 가산점: Capture=모든 아군이 거점 타일로(누군가 서야 점령), Escort=리더만 추출 타일로.
        // 타일에 설수록(거리 0=최대) 높다. 목표 타일이 없으면(섬멸/보스/생존) 0 → 일반 전투 점수에 영향 없음.
        private static int ScoreObjective(BattleState state, Unit actor, Candidate candidate)
        {
            IReadOnlyList<Coord> tiles = ObjectiveResolver.PullTilesFor(state, actor); // 후보 생성과 동일 SSOT
            if (tiles.Count == 0)
            {
                return 0;
            }

            int norm = TileProximityNorm(candidate.Destination, tiles, state.Map);
            return AiConst.ObjectivePullWeight * norm / Permille.BASE; // 데이터 주도(ai_const), 목표 타일 없으면 호출 안 됨
        }

        // 목적지가 목표 타일군에 가까울수록 1000 에 근접(타일 위=1000). Approach 와 동형이나 대상이 적이 아니라 거점/추출 타일.
        private static int TileProximityNorm(Coord destination, IReadOnlyList<Coord> tiles, Data.BattleMap map)
        {
            int maxDistance = map.Width + map.Height;
            if (maxDistance <= 0)
            {
                return 0;
            }

            int nearest = int.MaxValue;
            for (int index = 0; index < tiles.Count; index++)
            {
                int distance = GridDistance.Manhattan(destination, tiles[index]);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            int norm = Permille.BASE - (nearest * Permille.BASE / maxDistance);
            return IntMath.Clamp(norm, 0, Permille.BASE);
        }

        private static int ScoreAttack(StatBlock actorStat, Candidate candidate, HeroClass heroClass, Stance stance, int skillPowerPerMille)
        {
            Unit target = candidate.Target;
            StatBlock targetStat = StatResolver.EffectiveStat(target);
            int targetMaxHp = target.EntryStat.Get(StatType.Hp);

            FaceType face = FacingMath.ClassifyFace(target.Facing, candidate.Destination, target.Pos);
            DamageRequest request = new DamageRequest(actorStat, targetStat, true, skillPowerPerMille, face, Permille.BASE);
            int expected = DamageCalculator.EstimateDamage(request);

            int damageNorm = NormalizeByMax(expected, targetMaxHp);
            int killNorm = expected >= target.Hp ? Permille.BASE : 0;
            int flankNorm = FlankNorm(face);
            int focusNorm = Permille.BASE - HpRatio(target.Hp, targetMaxHp); // 피 깎인 적일수록↑

            int attackScore = 0;
            attackScore += Weighted(heroClass, stance, Consideration.Damage, damageNorm);
            attackScore += Weighted(heroClass, stance, Consideration.Kill, killNorm);
            attackScore += Weighted(heroClass, stance, Consideration.Flank, flankNorm);
            attackScore += Weighted(heroClass, stance, Consideration.Focus, focusNorm);
            return attackScore;
        }

        private static Unit NearestEnemy(BattleState state, Unit actor)
        {
            Unit nearest = null;
            int nearestDistance = 0;
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.IsAlive() == false || unit.Side == actor.Side)
                {
                    continue;
                }

                int distance = GridDistance.Manhattan(actor.Pos, unit.Pos);
                if (nearest == null || distance < nearestDistance)
                {
                    nearest = unit;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static int ApproachNorm(Coord destination, Coord enemyPos, Data.BattleMap map)
        {
            int maxDistance = map.Width + map.Height;
            if (maxDistance <= 0)
            {
                return 0;
            }

            int distance = GridDistance.Manhattan(destination, enemyPos);
            int norm = Permille.BASE - (distance * Permille.BASE / maxDistance);
            return IntMath.Clamp(norm, 0, Permille.BASE);
        }

        private static int FlankNorm(FaceType face)
        {
            if (face == FaceType.Back)
            {
                return AiConst.FlankBackNorm;
            }

            if (face == FaceType.Side)
            {
                return AiConst.FlankSideNorm;
            }

            return AiConst.FlankFrontNorm;
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

        private static int HpRatio(int hp, int maxHp)
        {
            if (maxHp <= 0)
            {
                return 0;
            }

            return IntMath.Clamp(hp * Permille.BASE / maxHp, 0, Permille.BASE);
        }

        private static int Weighted(HeroClass heroClass, Stance stance, Consideration consideration, int norm)
        {
            return AiWeights.FinalWeight(heroClass, stance, consideration) * norm / Permille.BASE;
        }

    }
}
