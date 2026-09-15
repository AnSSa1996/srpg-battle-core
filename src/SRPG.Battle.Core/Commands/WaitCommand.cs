using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Commands
{
    /// <summary>
    /// 대기. 아무 상태도 바꾸지 않는다. 턴 소비(CT 차감)는 전투 루프가 처리한다.
    /// (참조: 04 3-1 "대기 선택 시에도 게이지는 차감")
    /// </summary>
    public sealed class WaitCommand : ICommand
    {
        /// <summary>대기는 상태를 변경하지 않는다.</summary>
        public void Apply(BattleState state)
        {
        }
    }
}
