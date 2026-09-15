namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 유틸리티 AI 점수의 입력 항목. (참조: Docs/01_전투/04_전투_시스템.md E-2, 25_AI_스코어링.md)
    /// M6a 단계의 부분집합. M6b 에서 위치가치·보호·회복 등을 추가한다.
    /// Heal/Defend 는 지원 스킬 자동화(SupportPlanner)가 쓰는 항목이다.
    /// </summary>
    public enum Consideration
    {
        Damage = 0,
        Kill = 1,
        Flank = 2,
        Focus = 3,
        SelfRisk = 4,
        Approach = 5,
        Heal = 6,
        Defend = 7
    }
}
