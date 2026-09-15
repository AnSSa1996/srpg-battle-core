using System.Collections.Generic;

namespace SRPG.Battle.Core.Grid
{
    /// <summary>
    /// 한 턴에 이동·정지 가능한 타일 집합(Reach)을 균일 코스트 BFS 로 산출한다.
    /// 이동 코스트는 모든 통행 가능 타일에서 1. 아군 점유 칸은 통과 가능하나 정지 불가,
    /// 적 점유·벽은 통과 불가. 제자리(이동 0)도 항상 후보. 결과는 (y,x) 오름차순.
    /// (참조: Docs/01_전투/22_전투_그리드_규칙.md CMB-410~414)
    /// </summary>
    public static class ReachCalculator
    {
        // 모든 통행 가능 타일의 이동 코스트는 1로 균일 (CMB-410)
        private const int MOVE_COST = 1;

        private static readonly Coord[] _neighborSteps =
        {
            new Coord(0, -1),
            new Coord(-1, 0),
            new Coord(1, 0),
            new Coord(0, 1)
        };

        /// <summary>
        /// start 에서 movEff 이동력으로 도달해 정지할 수 있는 빈 타일 집합을 반환한다.
        /// start(제자리)는 항상 포함한다. (CMB-413)
        /// </summary>
        public static List<Coord> Compute(Coord start, int movEff, IReachQuery query)
        {
            List<Coord> result = new List<Coord>();
            if (query == null)
            {
                return result;
            }

            if (GridDistance.IsInBounds(start, query.Width, query.Height) == false)
            {
                return result;
            }

            int effectiveMov = movEff < 0 ? 0 : movEff;
            bool[,] visited = new bool[query.Height, query.Width];
            int[,] distance = new int[query.Height, query.Width];

            Queue<Coord> frontier = new Queue<Coord>();
            visited[start.Y, start.X] = true;
            distance[start.Y, start.X] = 0;
            frontier.Enqueue(start);
            result.Add(start); // 제자리도 항상 후보 (자기 자신은 None 취급)

            while (frontier.Count > 0)
            {
                Coord current = frontier.Dequeue();
                int currentDistance = distance[current.Y, current.X];
                if (currentDistance >= effectiveMov)
                {
                    continue;
                }

                for (int index = 0; index < _neighborSteps.Length; index++)
                {
                    Coord next = current + _neighborSteps[index];
                    if (GridDistance.IsInBounds(next, query.Width, query.Height) == false)
                    {
                        continue;
                    }

                    if (visited[next.Y, next.X])
                    {
                        continue;
                    }

                    if (query.IsWall(next))
                    {
                        visited[next.Y, next.X] = true; // 재검사 방지
                        continue;
                    }

                    TileOccupant occupant = query.OccupantOf(next);
                    if (occupant == TileOccupant.Enemy)
                    {
                        visited[next.Y, next.X] = true; // 통과 불가
                        continue;
                    }

                    visited[next.Y, next.X] = true;
                    distance[next.Y, next.X] = currentDistance + MOVE_COST;
                    frontier.Enqueue(next);

                    if (occupant == TileOccupant.None)
                    {
                        result.Add(next); // 빈 타일만 정지 후보 (아군 칸은 통과만)
                    }
                }
            }

            CoordSort.SortYThenX(result);
            return result;
        }
    }
}
