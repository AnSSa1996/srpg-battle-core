using System.Collections.Generic;

namespace SRPG.Battle.Core.Events
{
    /// <summary>이벤트를 순서대로 리스트에 모으는 싱크. 헤드리스 검증·리플레이 로그·ASCII 관전용. (VIEW-061)</summary>
    public sealed class ListEventSink : IEventSink
    {
        private readonly List<BattleEvent> _events = new List<BattleEvent>();

        /// <summary>방출 순서대로 모인 이벤트 목록.</summary>
        public IReadOnlyList<BattleEvent> Events { get { return _events; } }

        /// <summary>이벤트를 목록에 추가한다.</summary>
        public void Emit(BattleEvent battleEvent)
        {
            _events.Add(battleEvent);
        }
    }
}
