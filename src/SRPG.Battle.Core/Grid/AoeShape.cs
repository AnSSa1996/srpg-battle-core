using System;
using System.Collections.Generic;

namespace SRPG.Battle.Core.Grid
{
    /// <summary>
    /// AoE 패턴의 적중 타일을 중심 기준으로 결정론적으로 산출한다(맵 경계 클립은 호출 측에서).
    /// 결과는 항상 (y, x) 오름차순으로 정렬된다. (참조: Docs/01_전투/22_전투_그리드_규칙.md CMB-440)
    /// </summary>
    public static class AoeShape
    {
        private const int SQUARE3_RADIUS = 1;

        /// <summary>
        /// 패턴별 적중 타일 집합을 반환한다.
        /// Single/Cross/Square3 는 facing·size 를 무시한다. LineN 은 facing 방향으로 size 칸,
        /// SelfAroundR 은 맨해튼 거리 ≤ size 범위를 만든다.
        /// </summary>
        public static List<Coord> GetTiles(AoePattern pattern, Coord center, Facing facing, int size)
        {
            switch (pattern)
            {
                case AoePattern.Single: return BuildSingle(center);
                case AoePattern.Cross: return BuildCross(center);
                case AoePattern.Square3: return BuildSquare3(center);
                case AoePattern.LineN: return BuildLine(center, facing, size);
                case AoePattern.SelfAroundR: return BuildSelfAround(center, size);
                default: return BuildSingle(center);
            }
        }

        private static List<Coord> BuildSingle(Coord center)
        {
            List<Coord> tiles = new List<Coord> { center };
            return tiles;
        }

        private static List<Coord> BuildCross(Coord center)
        {
            List<Coord> tiles = new List<Coord>
            {
                center,
                center + new Coord(0, -1),
                center + new Coord(0, 1),
                center + new Coord(-1, 0),
                center + new Coord(1, 0)
            };
            CoordSort.SortYThenX(tiles);
            return tiles;
        }

        private static List<Coord> BuildSquare3(Coord center)
        {
            List<Coord> tiles = new List<Coord>();
            for (int dy = -SQUARE3_RADIUS; dy <= SQUARE3_RADIUS; dy++)
            {
                for (int dx = -SQUARE3_RADIUS; dx <= SQUARE3_RADIUS; dx++)
                {
                    tiles.Add(center + new Coord(dx, dy));
                }
            }

            CoordSort.SortYThenX(tiles);
            return tiles;
        }

        private static List<Coord> BuildLine(Coord center, Facing facing, int size)
        {
            List<Coord> tiles = new List<Coord>();
            if (size <= 0)
            {
                return tiles;
            }

            Coord step = FacingMath.ToVector(facing);
            Coord cursor = center;
            for (int index = 0; index < size; index++)
            {
                cursor = cursor + step;
                tiles.Add(cursor);
            }

            CoordSort.SortYThenX(tiles);
            return tiles;
        }

        private static List<Coord> BuildSelfAround(Coord center, int radius)
        {
            List<Coord> tiles = new List<Coord>();
            if (radius < 0)
            {
                return tiles;
            }

            for (int dy = -radius; dy <= radius; dy++)
            {
                int remaining = radius - Math.Abs(dy);
                for (int dx = -remaining; dx <= remaining; dx++)
                {
                    tiles.Add(center + new Coord(dx, dy));
                }
            }

            CoordSort.SortYThenX(tiles);
            return tiles;
        }
    }
}
