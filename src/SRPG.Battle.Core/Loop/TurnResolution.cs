using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>다음 행동자 선정 결과. 행동할 유닛과 그때까지 전진해야 할 틱 수. (CMB-014)</summary>
    public sealed class TurnResolution
    {
        /// <summary>행동권을 얻는 유닛.</summary>
        public Unit Actor { get; }

        /// <summary>행동자가 임계값에 도달하기까지 전진할 틱 수.</summary>
        public int TicksAdvanced { get; }

        /// <summary>턴 선정 결과를 만든다.</summary>
        public TurnResolution(Unit actor, int ticksAdvanced)
        {
            Actor = actor;
            TicksAdvanced = ticksAdvanced;
        }
    }
}
