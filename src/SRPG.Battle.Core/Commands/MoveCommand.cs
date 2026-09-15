using SRPG.Battle.Core.Events;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Commands
{
    /// <summary>
    /// 유닛을 지정 타일로 이동시킨다. 이동만 하고 끝나면 facing 은 마지막 이동 방향을 향한다. (CMB-432)
    /// 도달 가능성(Reach)은 호출 측(AI/디사이더)이 보장한다.
    /// </summary>
    public sealed class MoveCommand : ICommand
    {
        private readonly Unit _unit;
        private readonly Coord _destination;

        /// <summary>이동 커맨드를 만든다.</summary>
        public MoveCommand(Unit unit, Coord destination)
        {
            _unit = unit;
            _destination = destination;
        }

        /// <summary>유닛을 목적지로 옮기고 이동 방향으로 facing 을 맞춘다.</summary>
        public void Apply(BattleState state)
        {
            if (_unit == null)
            {
                return;
            }

            Coord from = _unit.Pos;
            _unit.MoveTo(_destination);
            _unit.TurnTo(FacingMath.RotateToward(from, _destination, _unit.Facing));

            state.Sink.Emit(BattleEvent.Moved(state.NextSeq(), state.ActionId, _unit.Slot, _destination));
        }
    }
}
