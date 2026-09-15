namespace SRPG.Battle.Core.Model
{
    /// <summary>유닛의 소속 진영(전투 편). (Docs/01_전투/04_전투_시스템.md)</summary>
    public enum Side
    {
        Ally = 0,
        Enemy = 1
    }

    /// <summary>배치 시 지정하는 AI 성향. 고려요소 가중치 프로파일을 바꾼다. (CMB 4-3, 스탠스)</summary>
    public enum Stance
    {
        Attack = 0,
        Defense = 1,
        Hold = 2,
        Protect = 3
    }

    /// <summary>영웅 클래스 5종 고정. 이동력(MOV)·사거리(RNG)를 가른다. (HERO-001)</summary>
    public enum HeroClass
    {
        Warrior = 0,
        Rogue = 1,
        Archer = 2,
        Mage = 3,
        Priest = 4
    }
}
