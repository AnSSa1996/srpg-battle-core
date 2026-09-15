using System.Collections.Generic;
using SRPG.Battle.Core.Commands;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>
    /// 행동권을 얻은 유닛의 행동을 커맨드 목록으로 결정한다.
    /// M4 는 더미 구현(DummyDecider), M6 의 유틸리티 AI 가 이 인터페이스를 구현해 교체한다.
    /// </summary>
    public interface IActionDecider
    {
        /// <summary>행동자(actor)의 이번 턴 행동을 커맨드 목록으로 반환한다(적용 순서대로).</summary>
        IReadOnlyList<ICommand> Decide(BattleState state, Unit actor);
    }
}
