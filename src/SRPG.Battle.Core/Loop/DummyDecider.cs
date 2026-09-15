using System.Collections.Generic;
using SRPG.Battle.Core.Commands;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>
    /// M6 유틸리티 AI 전까지 쓰는 더미 의사결정기. 결정론적이다.
    /// 가장 가까운 적을 찾아 사거리 안이면 평타, 아니면 도달 타일 중 적에 가장 가까운 곳으로 이동(이동 후 닿으면 평타).
    /// (가장 가까움·동률은 슬롯/(y,x) 순서로 결정 → 무작위 없음)
    /// </summary>
    public sealed class DummyDecider : IActionDecider
    {
        /// <summary>행동자의 더미 행동을 결정한다.</summary>
        public IReadOnlyList<ICommand> Decide(BattleState state, Unit actor)
        {
            List<ICommand> commands = new List<ICommand>();

            Unit target = FindNearestEnemy(state, actor);
            if (target == null)
            {
                commands.Add(new WaitCommand());
                return commands;
            }

            int range = actor.EntryStat.Get(StatType.Rng);
            if (GridDistance.Manhattan(actor.Pos, target.Pos) <= range)
            {
                commands.Add(new AttackCommand(actor, target));
                return commands;
            }

            int mov = actor.EntryStat.Get(StatType.Mov);
            IReachQuery query = state.CreateReachQuery(actor);
            List<Coord> reach = ReachCalculator.Compute(actor.Pos, mov, query);
            Coord destination = ChooseClosestTile(reach, target.Pos, actor.Pos);

            commands.Add(new MoveCommand(actor, destination));
            if (GridDistance.Manhattan(destination, target.Pos) <= range)
            {
                commands.Add(new AttackCommand(actor, target));
            }

            return commands;
        }

        private static Unit FindNearestEnemy(BattleState state, Unit actor)
        {
            Unit nearest = null;
            int nearestDistance = 0;
            IReadOnlyList<Unit> units = state.Units;

            for (int index = 0; index < units.Count; index++)
            {
                Unit candidate = units[index];
                if (candidate.IsAlive() == false || candidate.Side == actor.Side)
                {
                    continue;
                }

                int distance = GridDistance.Manhattan(actor.Pos, candidate.Pos);
                if (nearest == null || distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static Coord ChooseClosestTile(List<Coord> reach, Coord target, Coord fallback)
        {
            Coord best = fallback;
            int bestDistance = GridDistance.Manhattan(fallback, target);

            for (int index = 0; index < reach.Count; index++)
            {
                Coord tile = reach[index];
                int distance = GridDistance.Manhattan(tile, target);
                if (distance < bestDistance)
                {
                    best = tile;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
