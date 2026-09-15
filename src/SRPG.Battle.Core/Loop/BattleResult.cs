namespace SRPG.Battle.Core.Loop
{
    /// <summary>전투 종료 결과 종류.</summary>
    public enum BattleOutcome
    {
        Victory = 0,
        Defeat = 1
    }

    /// <summary>전투 종료 결과. 결과 종류와 종료 시점의 누적 행동 수·틱을 담는다.</summary>
    public sealed class BattleResult
    {
        /// <summary>결과 종류.</summary>
        public BattleOutcome Outcome { get; }

        /// <summary>종료 시 누적 행동 수.</summary>
        public int ActionCount { get; }

        /// <summary>종료 시 틱.</summary>
        public int Tick { get; }

        /// <summary>전투 결과를 만든다.</summary>
        public BattleResult(BattleOutcome outcome, int actionCount, int tick)
        {
            Outcome = outcome;
            ActionCount = actionCount;
            Tick = tick;
        }
    }
}
