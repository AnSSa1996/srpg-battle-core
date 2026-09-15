namespace SRPG.Battle.Core.Stats
{
    /// <summary>
    /// 전투가 사용하는 모든 스탯의 단일 정의(SSOT). 값은 0부터 연속이며 StatBlock 의 배열 인덱스로 쓰인다.
    /// (참조: Docs/02_성장영웅/18_밸런스_수치.md GROW-110, 스탯 마스터 15종)
    /// </summary>
    public enum StatType
    {
        Hp = 0,
        Atk = 1,
        Mag = 2,
        Def = 3,
        Res = 4,
        Spd = 5,
        Mov = 6,
        Rng = 7,
        Hit = 8,
        Eva = 9,
        CritRate = 10,
        CritDmg = 11,
        CritRes = 12,
        EffHit = 13,
        EffRes = 14
    }
}
