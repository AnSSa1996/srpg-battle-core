namespace SRPG.Battle.Core.Constants
{
    /// <summary>
    /// 천분율(per-mille) 기준값. 배율은 1000=100% 로 표현하는 정수 표기.
    /// 결정론 정수 연산을 위해 모든 배율 계산은 이 기준으로 나눈다.
    /// (참조: 용어집 per-mille, Docs/01_전투/15_전투_수식.md)
    /// </summary>
    public static class Permille
    {
        /// <summary>100% 에 해당하는 천분율 값(1000).</summary>
        public const int BASE = 1000;
    }
}
