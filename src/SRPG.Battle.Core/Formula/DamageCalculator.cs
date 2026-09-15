using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Rng;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Formula
{
    /// <summary>
    /// 단일 타격의 최종 피해를 고정 순서로 계산한다(순서 변경 금지). 전부 정수, 단계마다 ‰ 나눗셈(내림).
    /// ① 명중 판정 → ② 기본 피해 raw → ③ 방어 경감 → ④ 곱연산 체인(방향→필드→치명) → ⑤ 정수화·최소값.
    /// 난수 소비 순서: 명중 → 치명. 빗나가면 치명 난수를 뽑지 않는다.
    /// (참조: Docs/01_전투/15_전투_수식.md CMB-100~110, PRNG TECH-208)
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>요청과 시드 PRNG 로 단일 타격 결과를 계산한다. RNG 를 소비하므로 호출 순서가 결과를 바꾼다.</summary>
        public static DamageResult Compute(DamageRequest request, DetRng rng)
        {
            // ① 명중 판정 (CMB-101)
            int hitChance = HitChance(request);
            if (rng.NextPerMille() >= hitChance)
            {
                return new DamageResult(false, false, 0); // 빗나감 → 치명·이후 난수 미소비
            }

            // 치명 판정 (CMB-104c) — 명중한 경우에만 소비
            int critChance = CritChance(request);
            bool crit = rng.NextPerMille() < critChance;

            int damage = ComputeDamageValue(request, crit);
            return new DamageResult(true, crit, damage);
        }

        /// <summary>난수 없이 "명중·비치명 가정"의 예상 피해를 반환한다(AI 스코어링용, RNG 미소비).</summary>
        public static int EstimateDamage(DamageRequest request)
        {
            return ComputeDamageValue(request, false);
        }

        /// <summary>명중확률(‰)을 산출한다. (CMB-101)</summary>
        public static int HitChance(DamageRequest request)
        {
            return IntMath.Clamp(
                Permille.BASE + request.Attacker.Get(StatType.Hit) - request.Target.Get(StatType.Eva) + HitBonus(request.Face),
                CombatConst.HIT_MIN_PER_MILLE,
                CombatConst.HIT_MAX_PER_MILLE);
        }

        /// <summary>치명확률(‰)을 산출한다. 후면(백어택)이면 방향 치명 보정을 더한다. (CMB-104c/021/434)</summary>
        public static int CritChance(DamageRequest request)
        {
            return IntMath.Clamp(
                request.Attacker.Get(StatType.CritRate) - request.Target.Get(StatType.CritRes) + CritBonus(request.Face),
                CombatConst.CRIT_MIN_PER_MILLE,
                CombatConst.CRIT_MAX_PER_MILLE);
        }

        /// <summary>명중·치명이 정해진 뒤의 최종 피해값을 계산한다(스킬 다중효과 실행용, RNG 미소비).</summary>
        public static int DamageValue(DamageRequest request, bool crit)
        {
            return ComputeDamageValue(request, crit);
        }

        private static int ComputeDamageValue(DamageRequest request, bool crit)
        {
            StatBlock attacker = request.Attacker;
            StatBlock target = request.Target;

            // ② 기본 피해 raw (CMB-102)
            long attackStat = request.IsPhysical ? attacker.Get(StatType.Atk) : attacker.Get(StatType.Mag);
            long damage = attackStat * request.SkillPowerPerMille / Permille.BASE;

            // ③ 방어 경감 (CMB-103)
            long defenseStat = request.IsPhysical ? target.Get(StatType.Def) : target.Get(StatType.Res);
            damage = damage * CombatConst.DEF_K / (defenseStat + CombatConst.DEF_K);

            // ④ 곱연산 체인: 방향 → 필드 → 치명 → 취약 (순서 고정, CMB-104/104v)
            damage = damage * DirectionMult(request.Face) / Permille.BASE;
            damage = damage * request.FieldMultPerMille / Permille.BASE;
            long critMult = crit ? attacker.Get(StatType.CritDmg) : Permille.BASE;
            damage = damage * critMult / Permille.BASE;
            damage = damage * request.TargetVulnerabilityPerMille / Permille.BASE; // 받는 피해 증가(취약: freeze 등, CMB-104v)

            // ⑤ 정수화·최소값 (CMB-105)
            return damage < CombatConst.MIN_DAMAGE ? CombatConst.MIN_DAMAGE : (int)damage;
        }

        private static int HitBonus(FaceType face)
        {
            if (face == FaceType.Side)
            {
                return CombatConst.DIR_SIDE_HIT_BONUS;
            }

            return 0; // 후면은 명중 보정 없음(치명으로 보정, CMB-434)
        }

        // 후면(백어택)은 치명확률을 올린다. 측면/정면은 0. (CMB-021/434)
        private static int CritBonus(FaceType face)
        {
            if (face == FaceType.Back)
            {
                return CombatConst.DIR_BACK_CRIT_BONUS;
            }

            return 0;
        }

        private static int DirectionMult(FaceType face)
        {
            if (face == FaceType.Side)
            {
                return CombatConst.DIR_SIDE_MULT;
            }

            if (face == FaceType.Back)
            {
                return CombatConst.DIR_BACK_MULT;
            }

            return CombatConst.DIR_FRONT_MULT;
        }

    }
}
