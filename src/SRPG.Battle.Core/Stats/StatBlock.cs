using System;

namespace SRPG.Battle.Core.Stats
{
    /// <summary>
    /// 15종 스탯 값을 담는 불변 컨테이너. 인덱스는 StatType 순서와 일치한다.
    /// 베이스/진입(EntryStat)/유효(EffectiveStat) 스탯이 모두 이 타입을 쓴다.
    /// (참조: Docs/02_성장영웅/18_밸런스_수치.md GROW-110~131)
    /// </summary>
    public sealed class StatBlock
    {
        /// <summary>스탯 개수. StatType 정의 수와 반드시 일치한다.</summary>
        public const int STAT_COUNT = 15;

        private readonly int[] _values;

        /// <summary>STAT_COUNT 개 값으로 스탯 블록을 만든다(내부 복사본 보관).</summary>
        public StatBlock(int[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length != STAT_COUNT)
            {
                throw new ArgumentException("StatBlock 은 정확히 STAT_COUNT 개의 값이 필요하다.", nameof(values));
            }

            _values = (int[])values.Clone();
        }

        /// <summary>모든 스탯이 0인 블록을 만든다.</summary>
        public static StatBlock Zero()
        {
            return new StatBlock(new int[STAT_COUNT]);
        }

        /// <summary>지정 스탯 값을 반환한다.</summary>
        public int Get(StatType stat)
        {
            return _values[(int)stat];
        }

        /// <summary>내부 값의 복사본 배열을 반환한다(외부 변조 방지).</summary>
        public int[] ToArray()
        {
            return (int[])_values.Clone();
        }
    }
}
