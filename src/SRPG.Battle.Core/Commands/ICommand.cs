using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Commands
{
    /// <summary>
    /// 전투 상태의 모든 변경은 이 커맨드를 통해서만 적용한다(상태 직접 수정 금지).
    /// 실행된 커맨드를 기록하면 리플레이로 확장할 수 있다. (참조: CMB-043/047)
    /// </summary>
    public interface ICommand
    {
        /// <summary>전투 상태에 이 커맨드를 적용한다.</summary>
        void Apply(BattleState state);
    }
}
