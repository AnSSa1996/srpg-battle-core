using SRPG.Battle.Core.Grid;

namespace SRPG.Battle.Core.Model
{
    /// <summary>
    /// 특정 이동 주체(mover) 기준으로 BattleState 의 맵·점유 정보를 ReachCalculator 에 제공한다.
    /// 자기 자신이 선 칸은 None 으로 본다. (참조: CMB-412, M2 IReachQuery)
    /// </summary>
    public sealed class BattleReachQuery : IReachQuery
    {
        private readonly BattleState _state;
        private readonly Unit _mover;

        /// <summary>이동 주체 기준 도달 질의를 만든다.</summary>
        public BattleReachQuery(BattleState state, Unit mover)
        {
            _state = state;
            _mover = mover;
        }

        /// <summary>그리드 가로 크기.</summary>
        public int Width { get { return _state.Map.Width; } }

        /// <summary>그리드 세로 크기.</summary>
        public int Height { get { return _state.Map.Height; } }

        /// <summary>해당 타일이 통행 불가 지형인지 반환한다.</summary>
        public bool IsWall(Coord tile)
        {
            return _state.Map.IsWall(tile);
        }

        /// <summary>이동 주체 기준 점유 상태를 반환한다. 자기 자신/빈 칸은 None.</summary>
        public TileOccupant OccupantOf(Coord tile)
        {
            Unit occupant = _state.UnitAt(tile);
            if (occupant == null)
            {
                return TileOccupant.None;
            }

            if (occupant == _mover)
            {
                return TileOccupant.None;
            }

            return occupant.Side == _mover.Side ? TileOccupant.Ally : TileOccupant.Enemy;
        }
    }
}
