namespace SRPG.Battle.Core.Constants
{
    /// <summary>정수 공용 수학 헬퍼(결정론 코어 공유). 부동소수 없이 기기 간 동일 결과를 보장한다.</summary>
    public static class IntMath
    {
        /// <summary>value 를 [min, max] 범위로 자른다.</summary>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
