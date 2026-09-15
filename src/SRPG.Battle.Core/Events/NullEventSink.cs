namespace SRPG.Battle.Core.Events
{
    /// <summary>아무것도 하지 않는 이벤트 싱크(헤드리스 기본값). 연출 없이 시뮬만 돌릴 때 사용. (VIEW-002/060)</summary>
    public sealed class NullEventSink : IEventSink
    {
        /// <summary>공유 싱글톤 인스턴스.</summary>
        public static readonly NullEventSink Instance = new NullEventSink();

        private NullEventSink()
        {
        }

        /// <summary>이벤트를 버린다.</summary>
        public void Emit(BattleEvent battleEvent)
        {
        }
    }
}
