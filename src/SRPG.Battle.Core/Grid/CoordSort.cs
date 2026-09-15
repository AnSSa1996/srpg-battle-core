using System.Collections.Generic;

namespace SRPG.Battle.Core.Grid
{
    /// <summary>
    /// 좌표 리스트를 (y, x) 오름차순으로 정렬한다. 모든 타일 순회는 이 고정 순서를 쓴다.
    /// 컬렉션 순회 순서 의존을 없애 결정론을 보장한다. (참조: CMB-450)
    /// </summary>
    public static class CoordSort
    {
        /// <summary>(y, x) 오름차순 비교자. y 우선, 같으면 x.</summary>
        public static int CompareYThenX(Coord a, Coord b)
        {
            if (a.Y != b.Y)
            {
                return a.Y < b.Y ? -1 : 1;
            }

            if (a.X != b.X)
            {
                return a.X < b.X ? -1 : 1;
            }

            return 0;
        }

        /// <summary>리스트를 (y, x) 오름차순으로 제자리 정렬한다.</summary>
        public static void SortYThenX(List<Coord> tiles)
        {
            if (tiles == null)
            {
                return;
            }

            tiles.Sort(CompareYThenX);
        }
    }
}
