namespace SRPG.Battle.Core.Data
{
    /// <summary>스킬 타입. (CMB-300)</summary>
    public enum SkillType
    {
        Basic = 0,
        Active = 1,
        Ultimate = 2,
        Passive = 3
    }

    /// <summary>타게팅 타입. AI 가 의미 있는 대상만 뽑는 기준. (CMB-305)</summary>
    public enum TargetingType
    {
        Self = 0,
        SingleEnemy = 1,
        SingleAlly = 2,
        AoeEnemy = 3,
        AoeAlly = 4,
        Line = 5,
        Point = 6
    }

    /// <summary>스킬 효과 종류. effects[] 의 각 항목 타입. (CMB-308)</summary>
    public enum EffectType
    {
        Damage = 0,
        Heal = 1,
        ApplyStatus = 2,
        GaugeMod = 3,
        Move = 4,
        Shield = 5
    }

    /// <summary>강제 이동(Move 효과) 종류. 밀치기=시전자 반대로, 끌기=시전자 쪽으로. (CMB-308 Move)</summary>
    public enum MoveKind
    {
        None = 0,
        Knockback = 1,
        Pull = 2
    }
}
