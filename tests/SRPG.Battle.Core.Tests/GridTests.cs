using SRPG.Battle.Core.Grid;
using Xunit;

namespace SRPG.Battle.Core.Tests
{
    /// <summary>그리드 거리·방향 규칙(CMB-401/430~434) 검증.</summary>
    public class GridDistanceTests
    {
        [Theory]
        [InlineData(0, 0, 0, 0, 0)]
        [InlineData(0, 0, 3, 0, 3)]
        [InlineData(0, 0, 0, 4, 4)]
        [InlineData(1, 1, 4, 5, 7)]
        [InlineData(-2, -3, 2, 3, 10)]
        public void Manhattan_SumsAbsoluteDeltas(int ax, int ay, int bx, int by, int expected)
        {
            Assert.Equal(expected, GridDistance.Manhattan(new Coord(ax, ay), new Coord(bx, by)));
        }

        [Fact]
        public void Manhattan_IsSymmetric()
        {
            var a = new Coord(2, 7);
            var b = new Coord(-5, 1);

            Assert.Equal(GridDistance.Manhattan(a, b), GridDistance.Manhattan(b, a));
        }

        /// <summary>맨해튼 기준이므로 대각은 인접이 아니다. 여기가 깨지면 사거리 전체가 틀어진다.</summary>
        [Fact]
        public void IsAdjacent_ExcludesDiagonal()
        {
            var center = new Coord(3, 3);

            Assert.True(GridDistance.IsAdjacent(center, new Coord(3, 2)));
            Assert.True(GridDistance.IsAdjacent(center, new Coord(4, 3)));
            Assert.False(GridDistance.IsAdjacent(center, new Coord(4, 4)));
            Assert.False(GridDistance.IsAdjacent(center, center));
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(7, 5, true)]
        [InlineData(8, 5, false)]
        [InlineData(7, 6, false)]
        [InlineData(-1, 0, false)]
        public void IsInBounds_ChecksHalfOpenRange(int x, int y, bool expected)
        {
            Assert.Equal(expected, GridDistance.IsInBounds(new Coord(x, y), 8, 6));
        }
    }

    public class FacingMathTests
    {
        [Theory]
        [InlineData(Facing.North, 0, -1)]
        [InlineData(Facing.East, 1, 0)]
        [InlineData(Facing.South, 0, 1)]
        [InlineData(Facing.West, -1, 0)]
        public void ToVector_MapsEachFacingToUnitVector(Facing facing, int x, int y)
        {
            Coord vector = FacingMath.ToVector(facing);

            Assert.Equal(new Coord(x, y), vector);
        }

        [Fact]
        public void RotateToward_PrefersHorizontalWhenDeltaTies()
        {
            // dx=2, dy=2 동률 -> 좌우 우선 (CMB-431)
            Facing result = FacingMath.RotateToward(new Coord(0, 0), new Coord(2, 2), Facing.North);

            Assert.Equal(Facing.East, result);
        }

        [Fact]
        public void RotateToward_UsesVerticalWhenDeltaYDominates()
        {
            Facing result = FacingMath.RotateToward(new Coord(0, 0), new Coord(1, 5), Facing.North);

            Assert.Equal(Facing.South, result);
        }

        [Fact]
        public void RotateToward_SameTile_KeepsCurrentFacing()
        {
            var tile = new Coord(4, 4);

            Assert.Equal(Facing.West, FacingMath.RotateToward(tile, tile, Facing.West));
        }

        /// <summary>후면 피격(백어택) 판정이 핵심이라 정면/후면/측면을 모두 고정한다. (CMB-433)</summary>
        [Fact]
        public void ClassifyFace_DistinguishesFrontBackAndSide()
        {
            var target = new Coord(5, 5);

            // 남쪽을 보는 대상 기준: 아래(남)에서 때리면 정면
            Assert.Equal(FaceType.Front, FacingMath.ClassifyFace(Facing.South, new Coord(5, 8), target));

            // 위(북)에서 때리면 후면
            Assert.Equal(FaceType.Back, FacingMath.ClassifyFace(Facing.South, new Coord(5, 2), target));

            // 옆에서 때리면 측면
            Assert.Equal(FaceType.Side, FacingMath.ClassifyFace(Facing.South, new Coord(9, 5), target));
        }
    }
}
