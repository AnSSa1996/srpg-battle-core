using SRPG.Battle.Core.Constants;

namespace SRPG.Battle.Core.Stats
{
    /// <summary>
    /// 스탯 성장·조립 공식. 전부 정수, 나눗셈은 내림, 중간 계산은 64비트.
    /// (참조: Docs/02_성장영웅/18_밸런스_수치.md GROW-100/130/131/132)
    /// </summary>
    public static class StatFormula
    {
        private const int MIN_LEVEL = 1;
        private const int MIN_VITAL_VALUE = 1;   // HP·SPD 는 최소 1
        private const int MIN_DEFAULT_VALUE = 0; // 그 외 스탯은 최소 0
        private const int PERCENT_FLOOR_PER_MILLE = -900; // 비율 합 하한(×0.1), 예시값 GROW-132

        /// <summary>
        /// 레벨 L 에서의 스탯을 반환한다. stat(L) = floor(base × (1000 + growth‰ × (L−1)) / 1000). (GROW-100)
        /// </summary>
        public static int GrowStat(int baseValue, int growthPerMille, int level)
        {
            int effectiveLevel = level < MIN_LEVEL ? MIN_LEVEL : level;
            long scale = Permille.BASE + ((long)growthPerMille * (effectiveLevel - MIN_LEVEL));
            long scaled = (long)baseValue * scale / Permille.BASE;
            return ClampToInt(scaled);
        }

        /// <summary>
        /// 전투 진입 스탯(EntryStat)을 조립한다. 각 스탯 = floor((레벨스탯 + 장비/성급 flat) × (1000 + 비율‰) / 1000).
        /// 가산(flat) 먼저, 비율(%) 나중. (GROW-130/132)
        /// </summary>
        public static StatBlock BuildEntryStat(StatBlock leveled, StatBlock flatAdds, StatBlock percentPerMille)
        {
            int[] result = new int[StatBlock.STAT_COUNT];
            for (int index = 0; index < StatBlock.STAT_COUNT; index++)
            {
                StatType stat = (StatType)index;
                long flatSum = (long)leveled.Get(stat) + flatAdds.Get(stat);
                long scaled = flatSum * (Permille.BASE + percentPerMille.Get(stat)) / Permille.BASE;
                result[index] = ClampStat(stat, scaled);
            }

            return new StatBlock(result);
        }

        /// <summary>
        /// 전투 중 유효 스탯(EffectiveStat)을 산출한다. EntryStat × (1000 + Σ비율‰) / 1000.
        /// 비율 합은 하한(PERCENT_FLOOR)으로 clamp 한다. HP·SPD 는 최소 1, 그 외 최소 0. (GROW-131/132)
        /// </summary>
        public static StatBlock ApplyDynamicPercent(StatBlock entry, StatBlock percentPerMille)
        {
            int[] result = new int[StatBlock.STAT_COUNT];
            for (int index = 0; index < StatBlock.STAT_COUNT; index++)
            {
                StatType stat = (StatType)index;
                int percent = percentPerMille.Get(stat);
                if (percent < PERCENT_FLOOR_PER_MILLE)
                {
                    percent = PERCENT_FLOOR_PER_MILLE;
                }

                long scaled = (long)entry.Get(stat) * (Permille.BASE + percent) / Permille.BASE;
                result[index] = ClampStat(stat, scaled);
            }

            return new StatBlock(result);
        }

        private static int ClampStat(StatType stat, long value)
        {
            int minimum = IsVital(stat) ? MIN_VITAL_VALUE : MIN_DEFAULT_VALUE;
            if (value < minimum)
            {
                return minimum;
            }

            return ClampToInt(value);
        }

        private static int ClampToInt(long value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            return (int)value;
        }

        private static bool IsVital(StatType stat)
        {
            return stat == StatType.Hp || stat == StatType.Spd;
        }
    }
}
