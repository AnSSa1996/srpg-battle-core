using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 행동자 1명 기준의 읽기 전용 상황 인지(영향력 맵). 턴당 1회 계산해 캐시한다(행동 중 불변).
    /// 위협 맵(Threat): 각 타일에 섰을 때 적들에게 받을 예상 피해 합. 정수·고정 순회. (AI-160~164)
    /// </summary>
    public sealed class Blackboard
    {
        private readonly int[,] _threat;
        private readonly int _width;
        private readonly int _height;

        /// <summary>행동자 기준 위협 맵을 계산한다.</summary>
        public Blackboard(BattleState state, Unit actor)
        {
            _width = state.Map.Width;
            _height = state.Map.Height;
            _threat = new int[_height, _width];

            StatBlock actorStat = StatResolver.EffectiveStat(actor);
            IReadOnlyList<Unit> units = state.Units;

            for (int index = 0; index < units.Count; index++)
            {
                Unit enemy = units[index];
                if (enemy.IsAlive() == false || enemy.Side == actor.Side)
                {
                    continue;
                }

                int strikeRange = enemy.EntryStat.Get(StatType.Mov) + enemy.EntryStat.Get(StatType.Rng);
                int expectedHit = ExpectedHit(enemy, actorStat);
                AddThreat(enemy.Pos, strikeRange, expectedHit);
            }
        }

        /// <summary>해당 타일에서 받을 예상 피해 합. 범위 밖은 0.</summary>
        public int ThreatAt(Coord tile)
        {
            if (GridDistance.IsInBounds(tile, _width, _height) == false)
            {
                return 0;
            }

            return _threat[tile.Y, tile.X];
        }

        private void AddThreat(Coord origin, int strikeRange, int expectedHit)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (GridDistance.Manhattan(origin, new Coord(x, y)) <= strikeRange)
                    {
                        _threat[y, x] += expectedHit;
                    }
                }
            }
        }

        private static int ExpectedHit(Unit enemy, StatBlock actorStat)
        {
            StatBlock enemyStat = StatResolver.EffectiveStat(enemy);
            // 방향은 정면(×1.0, 중립) 기준으로 산출한 뒤 "방향 평균 가중 0.5"를 곱한다 (AI-160/164)
            DamageRequest request = new DamageRequest(enemyStat, actorStat, true, Permille.BASE,
                FaceType.Front, Permille.BASE);
            int frontDamage = DamageCalculator.EstimateDamage(request);
            return frontDamage * AiConst.ThreatDirectionAvgPerMille / Permille.BASE;
        }
    }
}
