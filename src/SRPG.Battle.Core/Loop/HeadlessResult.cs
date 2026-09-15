using System.Collections.Generic;
using SRPG.Battle.Core.Events;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>헤드리스 실행 결과. 전투 결과 + 방출된 이벤트 로그(리플레이/관전용). (VIEW-060/061)</summary>
    public sealed class HeadlessResult
    {
        /// <summary>전투 결과(승패·행동수·틱).</summary>
        public BattleResult Result { get; }

        /// <summary>방출 순서대로의 이벤트 로그.</summary>
        public IReadOnlyList<BattleEvent> Events { get; }

        /// <summary>헤드리스 결과를 만든다.</summary>
        public HeadlessResult(BattleResult result, IReadOnlyList<BattleEvent> events)
        {
            Result = result;
            Events = events;
        }
    }
}
