namespace SRPG.Battle.Core.Events
{
    /// <summary>
    /// 시뮬이 방출하는 BattleEvent 를 받는 단방향 출구. 뷰/로거가 구현한다.
    /// 시뮬 → (이벤트) → 뷰. 뷰는 시뮬을 호출/변경하지 않는다. (참조: VIEW-001/004)
    /// </summary>
    public interface IEventSink
    {
        /// <summary>이벤트 하나를 받는다.</summary>
        void Emit(BattleEvent battleEvent);
    }
}
