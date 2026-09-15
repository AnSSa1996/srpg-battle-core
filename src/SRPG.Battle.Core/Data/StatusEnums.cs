namespace SRPG.Battle.Core.Data
{
    /// <summary>상태 효과 분류. (CMB-200)</summary>
    public enum StatusCategory
    {
        DoT = 0,
        CC = 1,
        StatMod = 2,
        Shield = 3,
        Special = 4,
        /// <summary>침묵: 턴은 행동하되 액티브·궁극 사용 불가(평타만). CC 와 달리 턴을 막지 않는다. (CMB-200)</summary>
        Silence = 5,
        /// <summary>도발: AI 타깃을 시전자로 강제. 턴을 막지 않는다. (AI-170)</summary>
        Taunt = 6
    }

    /// <summary>상태 중첩 규칙. (CMB-206)</summary>
    public enum StackPolicy
    {
        Refresh = 0,
        Stack = 1,
        Independent = 2
    }

    /// <summary>
    /// DoT 틱당 피해 산정 방식. 부여 시점에 Value 를 실수치로 스냅샷한다(결정론·시전자 생존 무관). (CMB-200)
    /// </summary>
    public enum DotValueType
    {
        /// <summary>value 를 고정 피해로 그대로 사용.</summary>
        FlatConst = 0,
        /// <summary>대상 최대HP × value(‰) 를 틱 피해로. (예: 독)</summary>
        FlatPerMaxHp = 1,
        /// <summary>시전자 마력 × value(‰) 를 틱 피해로. (예: 화상)</summary>
        FlatFromCaster = 2
    }
}
