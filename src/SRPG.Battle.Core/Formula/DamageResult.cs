namespace SRPG.Battle.Core.Formula
{
    /// <summary>단일 타격 데미지 계산 결과. 빗나가면 Hit=false, Damage=0. (CMB-101/105)</summary>
    public sealed class DamageResult
    {
        /// <summary>명중 여부.</summary>
        public bool Hit { get; }

        /// <summary>치명 여부.</summary>
        public bool Crit { get; }

        /// <summary>최종 피해(명중 시 ≥1, 빗나가면 0).</summary>
        public int Damage { get; }

        /// <summary>데미지 결과를 만든다.</summary>
        public DamageResult(bool hit, bool crit, int damage)
        {
            Hit = hit;
            Crit = crit;
            Damage = damage;
        }
    }
}
