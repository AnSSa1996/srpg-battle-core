using System;

namespace SRPG.Battle.Core.Grid
{
    /// <summary>
    /// 그리드 거리 계산. 전투의 모든 거리(사거리·이동·AoE 반경)는 맨해튼 거리를 쓴다.
    /// (참조: Docs/01_전투/22_전투_그리드_규칙.md CMB-401/402)
    /// </summary>
    public static class GridDistance
    {
        private const int ADJACENT_DISTANCE = 1;

        /// <summary>두 좌표의 맨해튼 거리 |dx|+|dy| 를 반환한다. (CMB-401)</summary>
        public static int Manhattan(Coord a, Coord b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        /// <summary>두 좌표가 직교 인접(거리 1)한지 판정한다. (CMB-401)</summary>
        public static bool IsAdjacent(Coord a, Coord b)
        {
            return Manhattan(a, b) == ADJACENT_DISTANCE;
        }

        /// <summary>좌표가 [0,width)·[0,height) 범위 안에 있는지 판정한다.</summary>
        public static bool IsInBounds(Coord tile, int width, int height)
        {
            return tile.X >= 0 && tile.X < width && tile.Y >= 0 && tile.Y < height;
        }
    }
}
