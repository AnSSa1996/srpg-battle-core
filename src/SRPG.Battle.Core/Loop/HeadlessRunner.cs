using SRPG.Battle.Core.Events;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>
    /// 뷰 없이 시뮬만 실행하고 이벤트 로그를 모은다. 서버 검증·자동 테스트·AI 튜닝(ASCII 관전)에 사용.
    /// 동일 seed + 배치면 결과·이벤트 스트림이 항상 동일하다. (VIEW-060/061)
    /// </summary>
    public static class HeadlessRunner
    {
        /// <summary>주어진 상태에 수집 싱크를 달고 끝까지 실행해 결과+이벤트를 반환한다.</summary>
        public static HeadlessResult Run(BattleState state, IActionDecider decider)
        {
            ListEventSink sink = new ListEventSink();
            state.SetSink(sink);

            BattleSimulator simulator = new BattleSimulator(decider);
            BattleResult result = simulator.Run(state);

            return new HeadlessResult(result, sink.Events);
        }
    }
}
