using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Model
{
    /// <summary>
    /// 상태 효과 부여를 처리한다. 디버프는 효과적중/효과저항 판정(시드 PRNG), 버프/아군 효과는 무판정 적용.
    /// 중첩은 stackPolicy(Refresh/Stack/Independent)를 따른다.
    /// (참조: Docs/01_전투/16_상태이상.md CMB-204~206)
    /// </summary>
    public static class StatusSystem
    {
        private const int FIRST_STACK = 1;

        /// <summary>
        /// 상태를 대상에 부여한다. 디버프면 적용 판정을 거치고(실패 시 false), 버프/환경 효과는 무조건 적용.
        /// 효과량·대상 스탯은 StatusDef(Value/ModStat)에서 가져온다.
        /// </summary>
        public static bool TryApply(BattleState state, Unit source, Unit target, StatusDef def, bool isDebuff)
        {
            if (def == null || target == null)
            {
                return false;
            }

            if (isDebuff && PassesResistCheck(state, source, target) == false)
            {
                return false;
            }

            ApplyByPolicy(source, target, def);
            return true;
        }

        private static bool PassesResistCheck(BattleState state, Unit source, Unit target)
        {
            int effHit = StatResolver.EffectiveStat(source).Get(StatType.EffHit);
            int effRes = StatResolver.EffectiveStat(target).Get(StatType.EffRes);
            int chance = IntMath.Clamp(
                Permille.BASE + effHit - effRes,
                CombatConst.HIT_MIN_PER_MILLE,
                CombatConst.HIT_MAX_PER_MILLE);
            return state.Rng.NextPerMille() < chance;
        }

        private static void ApplyByPolicy(Unit source, Unit target, StatusDef def)
        {
            if (def.StackPolicy == StackPolicy.Independent)
            {
                AddNew(source, target, def);
                return;
            }

            StatusEffect existing = FindById(target, def.Id);
            if (existing == null)
            {
                AddNew(source, target, def);
                return;
            }

            if (def.StackPolicy == StackPolicy.Stack)
            {
                existing.AddStack(def.MaxStack);
            }

            existing.RefreshDuration(def.DurationTurns);
        }

        private static void AddNew(Unit source, Unit target, StatusDef def)
        {
            int sourceSlot = source == null ? 0 : source.Slot;
            int value = ResolveDotValue(source, target, def);
            target.AddStatus(new StatusEffect(def.Id, def.Category, value, def.ModStat,
                sourceSlot, def.DurationTurns, FIRST_STACK, def.IncomingDamageMult));
        }

        // DoT 는 부여 시점에 틱당 피해를 실수치로 스냅샷한다(결정론·시전자 생존 무관). DoT 외 분류는 Value 그대로. (CMB-200)
        private static int ResolveDotValue(Unit source, Unit target, StatusDef def)
        {
            if (def.Category != StatusCategory.DoT || def.ValueType == DotValueType.FlatConst)
            {
                return def.Value;
            }

            if (def.ValueType == DotValueType.FlatPerMaxHp)
            {
                long maxHp = target.EntryStat.Get(StatType.Hp);
                return (int)(maxHp * def.Value / Permille.BASE);
            }

            // FlatFromCaster: 시전자 마력 × value(‰). 시전자 부재(환경효과 등)면 고정값 폴백.
            if (source == null)
            {
                return def.Value;
            }

            long magic = StatResolver.EffectiveStat(source).Get(StatType.Mag);
            return (int)(magic * def.Value / Permille.BASE);
        }

        private static StatusEffect FindById(Unit target, string id)
        {
            IReadOnlyList<StatusEffect> statuses = target.Statuses;
            for (int index = 0; index < statuses.Count; index++)
            {
                if (statuses[index].Id == id)
                {
                    return statuses[index];
                }
            }

            return null;
        }
    }
}
