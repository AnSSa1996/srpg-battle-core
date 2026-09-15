using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Formula
{
    /// <summary>
    /// 단일 타격 데미지 계산 입력. 스탯은 유효 스탯(EffectiveStat), 배율은 ‰ 정수.
    /// (참조: Docs/01_전투/15_전투_수식.md CMB-100~104)
    /// </summary>
    public sealed class DamageRequest
    {
        /// <summary>공격자 유효 스탯.</summary>
        public StatBlock Attacker { get; }

        /// <summary>대상 유효 스탯.</summary>
        public StatBlock Target { get; }

        /// <summary>물리(ATK·DEF)면 true, 마법(마력·저항)이면 false.</summary>
        public bool IsPhysical { get; }

        /// <summary>스킬 배율(‰). 평타=1000(100%).</summary>
        public int SkillPowerPerMille { get; }

        /// <summary>피격 면(정면/측면/후면). 명중 보정·방향 배율에 쓰인다.</summary>
        public FaceType Face { get; }

        /// <summary>층 필드효과 누적 배율(‰). 없으면 1000.</summary>
        public int FieldMultPerMille { get; }

        /// <summary>대상 받는 피해 배율(‰, 취약 상태 누적). 없으면 1000. (CMB-104v)</summary>
        public int TargetVulnerabilityPerMille { get; }

        /// <summary>데미지 계산 입력을 만든다.</summary>
        public DamageRequest(StatBlock attacker, StatBlock target, bool isPhysical, int skillPowerPerMille,
            FaceType face, int fieldMultPerMille, int targetVulnerabilityPerMille = 1000)
        {
            Attacker = attacker;
            Target = target;
            IsPhysical = isPhysical;
            SkillPowerPerMille = skillPowerPerMille;
            Face = face;
            FieldMultPerMille = fieldMultPerMille;
            TargetVulnerabilityPerMille = targetVulnerabilityPerMille;
        }
    }
}
