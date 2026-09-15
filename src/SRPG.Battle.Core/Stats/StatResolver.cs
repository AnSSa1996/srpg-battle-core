using System.Collections.Generic;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Stats
{
    /// <summary>
    /// 데미지·명중·CT 가 참조하는 유효 스탯(EffectiveStat)을 산출한다.
    /// EntryStat 에 상태이상 StatMod + 필드효과 StatMod 비율을 합산해 적용한다(가산 후 1회 곱).
    /// (참조: GROW-131, CMB-208, CMB-520 StatMod)
    /// </summary>
    public static class StatResolver
    {
        /// <summary>유닛의 현재 유효 스탯 블록을 반환한다.</summary>
        public static StatBlock EffectiveStat(Unit unit)
        {
            int[] percentPerMille = new int[StatBlock.STAT_COUNT];

            IReadOnlyList<StatusEffect> statuses = unit.Statuses;
            for (int index = 0; index < statuses.Count; index++)
            {
                StatusEffect status = statuses[index];
                if (status.Category != StatusCategory.StatMod)
                {
                    continue;
                }

                // 가속/공증/방감 등: 대상 스탯에 Value(‰) × 중첩 만큼 가감
                percentPerMille[(int)status.ModStat] += status.Value * status.Stacks;
            }

            // 필드효과 StatMod(전투 시작 시 설정)도 같은 ‰ 척도로 합산한다. (CMB-520 StatMod)
            StatBlock fieldPercent = unit.FieldStatPercent;
            for (int index = 0; index < StatBlock.STAT_COUNT; index++)
            {
                percentPerMille[index] += fieldPercent.Get((StatType)index);
            }

            return StatFormula.ApplyDynamicPercent(unit.EntryStat, new StatBlock(percentPerMille));
        }
    }
}
