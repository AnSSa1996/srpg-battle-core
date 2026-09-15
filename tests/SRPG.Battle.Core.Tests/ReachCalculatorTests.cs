using System.Collections.Generic;
using SRPG.Battle.Core.Grid;
using Xunit;

namespace SRPG.Battle.Core.Tests
{
    /// <summary>
    /// 이동 도달 범위 BFS(CMB-410~414) 검증.
    /// 규칙: 코스트 균일 1 / 벽·적 = 통과 불가 / 아군 = 통과 가능하지만 정지 불가 / 제자리는 항상 포함.
    /// </summary>
    public class ReachCalculatorTests
    {
        /// <summary>테스트용 맵. '#'=벽, 'E'=적, 'A'=아군, '.'=빈 칸.</summary>
        private sealed class FakeReachQuery : IReachQuery
        {
            private readonly string[] _rows;

            public FakeReachQuery(params string[] rows)
            {
                _rows = rows;
            }

            public int Width => _rows[0].Length;

            public int Height => _rows.Length;

            public bool IsWall(Coord tile) => _rows[tile.Y][tile.X] == '#';

            public TileOccupant OccupantOf(Coord tile)
            {
                char cell = _rows[tile.Y][tile.X];
                if (cell == 'E') return TileOccupant.Enemy;
                if (cell == 'A') return TileOccupant.Ally;
                return TileOccupant.None;
            }
        }

        [Fact]
        public void ZeroMovement_ReturnsOnlyStartTile()
        {
            var query = new FakeReachQuery(
                ".....",
                ".....",
                ".....");

            List<Coord> reach = ReachCalculator.Compute(new Coord(2, 1), 0, query);

            Assert.Single(reach);
            Assert.Equal(new Coord(2, 1), reach[0]);
        }

        [Fact]
        public void OpenField_MovementOne_ReturnsStartPlusFourNeighbours()
        {
            var query = new FakeReachQuery(
                ".....",
                ".....",
                ".....");

            List<Coord> reach = ReachCalculator.Compute(new Coord(2, 1), 1, query);

            Assert.Equal(5, reach.Count);
            Assert.Contains(new Coord(2, 1), reach);
            Assert.Contains(new Coord(2, 0), reach);
            Assert.Contains(new Coord(1, 1), reach);
            Assert.Contains(new Coord(3, 1), reach);
            Assert.Contains(new Coord(2, 2), reach);
        }

        [Fact]
        public void OpenField_MovementTwo_FormsManhattanDiamond()
        {
            var query = new FakeReachQuery(
                ".....",
                ".....",
                ".....",
                ".....",
                ".....");

            var start = new Coord(2, 2);
            List<Coord> reach = ReachCalculator.Compute(start, 2, query);

            // 반경 2 다이아몬드 = 13 칸
            Assert.Equal(13, reach.Count);
            foreach (Coord tile in reach)
            {
                Assert.True(GridDistance.Manhattan(start, tile) <= 2);
            }
        }

        [Fact]
        public void Wall_BlocksMovementThrough()
        {
            var query = new FakeReachQuery(
                "..#..",
                "..#..",
                "..#..");

            List<Coord> reach = ReachCalculator.Compute(new Coord(0, 1), 4, query);

            Assert.DoesNotContain(new Coord(2, 1), reach); // 벽 자체
            Assert.DoesNotContain(new Coord(4, 1), reach); // 벽 너머
            Assert.Contains(new Coord(1, 1), reach);
        }

        [Fact]
        public void Enemy_BlocksPassage()
        {
            var query = new FakeReachQuery("..E..");

            List<Coord> reach = ReachCalculator.Compute(new Coord(0, 0), 4, query);

            Assert.DoesNotContain(new Coord(2, 0), reach); // 적 칸
            Assert.DoesNotContain(new Coord(3, 0), reach); // 적 너머
            Assert.Contains(new Coord(1, 0), reach);
        }

        /// <summary>아군은 지나갈 수 있지만 그 칸에서 멈출 수는 없다. (CMB-412)</summary>
        [Fact]
        public void Ally_IsPassableButNotStoppable()
        {
            var query = new FakeReachQuery("..A..");

            List<Coord> reach = ReachCalculator.Compute(new Coord(0, 0), 4, query);

            Assert.DoesNotContain(new Coord(2, 0), reach); // 아군 칸에는 정지 불가
            Assert.Contains(new Coord(3, 0), reach);       // 통과해서 그 너머로는 이동 가능
        }

        [Fact]
        public void Result_IsSortedByRowThenColumn()
        {
            var query = new FakeReachQuery(
                ".....",
                ".....",
                ".....",
                ".....",
                ".....");

            List<Coord> reach = ReachCalculator.Compute(new Coord(2, 2), 2, query);

            for (int i = 1; i < reach.Count; i++)
            {
                Coord previous = reach[i - 1];
                Coord current = reach[i];
                bool ordered = previous.Y < current.Y
                               || (previous.Y == current.Y && previous.X < current.X);
                Assert.True(ordered, $"정렬 위반: ({previous.X},{previous.Y}) 다음에 ({current.X},{current.Y})");
            }
        }

        [Fact]
        public void StartOutOfBounds_ReturnsEmpty()
        {
            var query = new FakeReachQuery(".....");

            Assert.Empty(ReachCalculator.Compute(new Coord(9, 0), 3, query));
        }

        [Fact]
        public void NullQuery_ReturnsEmpty()
        {
            Assert.Empty(ReachCalculator.Compute(new Coord(0, 0), 3, null));
        }
    }
}
