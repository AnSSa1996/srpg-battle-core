using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Loop;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 행동 후보를 결정론적으로 생성한다. 최근접 적 shortlist × 도달 타일(사거리 링) 공격 후보 + 접근 후보 + 대기 후보.
    /// 전부 고정 순서(슬롯 / (y,x))로 생성하고 하드 캡(N)을 둔다. (참조: AI-020, CMB-413)
    /// </summary>
    public static class CandidateGenerator
    {
        /// <summary>행동자의 후보를 평타 사거리(유닛 RNG)로 생성한다.</summary>
        public static List<Candidate> Generate(BattleState state, Unit actor)
        {
            return Generate(state, actor, actor.EntryStat.Get(StatType.Rng));
        }

        /// <summary>행동자의 후보를 지정 사거리(스킬 사거리)로 생성한다(생성 순서대로 인덱스 부여).</summary>
        public static List<Candidate> Generate(BattleState state, Unit actor, int range)
        {
            List<Candidate> candidates = new List<Candidate>();

            int movement = actor.EntryStat.Get(StatType.Mov) + FieldEffectResolver.MovDelta(state.Map, actor); // 필드 MovMod (CMB-520)
            if (movement < 0)
            {
                movement = 0;
            }

            IReachQuery query = state.CreateReachQuery(actor);
            List<Coord> reach = ReachCalculator.Compute(actor.Pos, movement, query);
            List<Unit> enemies = NearestEnemies(state, actor);

            int generationIndex = 0;

            // 공격 후보: 각 대상에 대해 사거리 안에 드는 도달 타일
            for (int targetIndex = 0; targetIndex < enemies.Count; targetIndex++)
            {
                Unit target = enemies[targetIndex];
                for (int tileIndex = 0; tileIndex < reach.Count; tileIndex++)
                {
                    Coord tile = reach[tileIndex];
                    if (GridDistance.Manhattan(tile, target.Pos) > range)
                    {
                        continue;
                    }

                    candidates.Add(new Candidate(tile, target, generationIndex));
                    generationIndex++;
                    if (candidates.Count >= AiConst.CandidateCap)
                    {
                        return candidates;
                    }
                }
            }

            // 접근 후보: 가장 가까운 적에 제일 근접한 도달 타일(공격 없음)
            if (enemies.Count > 0)
            {
                Coord approachTile = ClosestTile(reach, enemies[0].Pos, actor.Pos);
                candidates.Add(new Candidate(approachTile, null, generationIndex));
                generationIndex++;
            }

            // 목표 추구 후보: 거점 점령/호위 추출 타일 위에 서거나(있으면) 그쪽으로 접근하는 도달 타일. 목표 타일 없으면 무동작. (CMB-512)
            AddObjectiveCandidates(state, actor, reach, candidates, ref generationIndex);

            // 대기 후보: 제자리
            candidates.Add(new Candidate(actor.Pos, null, generationIndex));
            return candidates;
        }

        // 거점/추출 타일을 후보로 추가한다(없으면 무동작). 타일 위 도달 칸 전부 + 못 닿으면 가장 근접한 도달 칸 1개.
        private static void AddObjectiveCandidates(BattleState state, Unit actor, List<Coord> reach,
            List<Candidate> candidates, ref int generationIndex)
        {
            IReadOnlyList<Coord> tiles = ObjectiveResolver.PullTilesFor(state, actor); // 점수(UtilityScorer)와 동일 SSOT
            if (tiles.Count == 0)
            {
                return;
            }

            for (int tileIndex = 0; tileIndex < reach.Count; tileIndex++)
            {
                if (Contains(tiles, reach[tileIndex]) == false)
                {
                    continue;
                }

                candidates.Add(new Candidate(reach[tileIndex], null, generationIndex));
                generationIndex++;
                if (candidates.Count >= AiConst.CandidateCap)
                {
                    return;
                }
            }

            Coord nearestTile = NearestTo(tiles, actor.Pos);
            Coord approachTile = ClosestTile(reach, nearestTile, actor.Pos);
            candidates.Add(new Candidate(approachTile, null, generationIndex));
            generationIndex++;
        }

        private static bool Contains(IReadOnlyList<Coord> tiles, Coord pos)
        {
            for (int index = 0; index < tiles.Count; index++)
            {
                if (tiles[index] == pos)
                {
                    return true;
                }
            }

            return false;
        }

        private static Coord NearestTo(IReadOnlyList<Coord> tiles, Coord origin)
        {
            Coord best = tiles[0];
            int bestDistance = GridDistance.Manhattan(origin, tiles[0]);
            for (int index = 1; index < tiles.Count; index++)
            {
                int distance = GridDistance.Manhattan(origin, tiles[index]);
                if (distance < bestDistance)
                {
                    best = tiles[index];
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static List<Unit> NearestEnemies(BattleState state, Unit actor)
        {
            // 도발: 공격 대상을 도발자 1명으로 강제(접근 후보도 도발자 기준). (AI-170)
            Unit taunter = ControlStatus.ForcedTauntTarget(state, actor);
            if (taunter != null)
            {
                return new List<Unit> { taunter };
            }

            List<Unit> enemies = new List<Unit>();
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.IsAlive() && unit.Side != actor.Side)
                {
                    enemies.Add(unit);
                }
            }

            // 거리 오름차순, 동률은 슬롯 오름차순(안정). 단순 선택 정렬로 결정론 보장.
            SortByDistanceThenSlot(enemies, actor.Pos);

            if (enemies.Count > AiConst.EnemyShortlist)
            {
                enemies.RemoveRange(AiConst.EnemyShortlist, enemies.Count - AiConst.EnemyShortlist);
            }

            return enemies;
        }

        private static void SortByDistanceThenSlot(List<Unit> enemies, Coord origin)
        {
            enemies.Sort((a, b) =>
            {
                int distanceA = GridDistance.Manhattan(origin, a.Pos);
                int distanceB = GridDistance.Manhattan(origin, b.Pos);
                if (distanceA != distanceB)
                {
                    return distanceA < distanceB ? -1 : 1;
                }

                return a.Slot.CompareTo(b.Slot);
            });
        }

        private static Coord ClosestTile(List<Coord> reach, Coord target, Coord fallback)
        {
            Coord best = fallback;
            int bestDistance = GridDistance.Manhattan(fallback, target);
            for (int index = 0; index < reach.Count; index++)
            {
                int distance = GridDistance.Manhattan(reach[index], target);
                if (distance < bestDistance)
                {
                    best = reach[index];
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
