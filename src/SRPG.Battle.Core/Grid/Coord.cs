using System;

namespace SRPG.Battle.Core.Grid
{
    /// <summary>
    /// 전투 그리드의 정수 좌표 (x→오른쪽, y→아래, 원점 좌상단).
    /// 모든 거리·이동·면 판정의 기본 단위. (참조: Docs/01_전투/22_전투_그리드_규칙.md CMB-400)
    /// </summary>
    public readonly struct Coord : IEquatable<Coord>
    {
        private readonly int _x;
        private readonly int _y;

        /// <summary>좌표를 만든다.</summary>
        public Coord(int x, int y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>x 좌표 (오른쪽 방향 증가).</summary>
        public int X { get { return _x; } }

        /// <summary>y 좌표 (아래 방향 증가).</summary>
        public int Y { get { return _y; } }

        /// <summary>두 좌표의 성분별 합.</summary>
        public static Coord operator +(Coord a, Coord b)
        {
            return new Coord(a._x + b._x, a._y + b._y);
        }

        /// <summary>두 좌표의 성분별 차.</summary>
        public static Coord operator -(Coord a, Coord b)
        {
            return new Coord(a._x - b._x, a._y - b._y);
        }

        /// <summary>두 좌표가 같은지 비교한다.</summary>
        public static bool operator ==(Coord a, Coord b)
        {
            return a._x == b._x && a._y == b._y;
        }

        /// <summary>두 좌표가 다른지 비교한다.</summary>
        public static bool operator !=(Coord a, Coord b)
        {
            return (a == b) == false;
        }

        /// <summary>같은 좌표인지 판정한다.</summary>
        public bool Equals(Coord other)
        {
            return _x == other._x && _y == other._y;
        }

        public override bool Equals(object obj)
        {
            return obj is Coord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_x * 397) ^ _y;
            }
        }

        public override string ToString()
        {
            return "(" + _x + "," + _y + ")";
        }
    }
}
