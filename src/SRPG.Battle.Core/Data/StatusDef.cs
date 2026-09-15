using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Data
{
    /// <summary>
    /// 상태 효과 정의(불변 입력 데이터, SSOT). status 테이블(TbStatus)에서 1:1 로 만들어진다.
    /// (참조: Docs/01_전투/16_상태이상.md CMB-200/206)
    /// </summary>
    public sealed class StatusDef
    {
        /// <summary>효과 식별자.</summary>
        public string Id { get; }

        /// <summary>분류(DoT/CC/StatMod/Shield/Special).</summary>
        public StatusCategory Category { get; }

        /// <summary>기본 지속(대상 턴 수).</summary>
        public int DurationTurns { get; }

        /// <summary>효과량(‰ 또는 정수, 분류별 의미). DoT=틱당 피해(ValueType 로 해석), StatMod=가감‰.</summary>
        public int Value { get; }

        /// <summary>DoT 산정 방식(고정/최대HP비율/시전자마력비율). DoT 외 분류에서는 무시. (CMB-200)</summary>
        public DotValueType ValueType { get; }

        /// <summary>이 상태 보유 시 대상이 받는 피해 배율(‰, 기본 1000=변화 없음). 취약(freeze 등). (CMB-104v)</summary>
        public int IncomingDamageMult { get; }

        /// <summary>중첩 규칙.</summary>
        public StackPolicy StackPolicy { get; }

        /// <summary>최대 중첩 수.</summary>
        public int MaxStack { get; }

        /// <summary>StatMod 일 때 대상 스탯(그 외 분류에서는 무시).</summary>
        public StatType ModStat { get; }

        /// <summary>정화(Dispel) 가능 여부.</summary>
        public bool Dispellable { get; }

        /// <summary>상태 정의를 만든다.</summary>
        public StatusDef(string id, StatusCategory category, int durationTurns, int value,
            StackPolicy stackPolicy, int maxStack, StatType modStat, bool dispellable,
            DotValueType valueType = DotValueType.FlatConst, int incomingDamageMult = 1000)
        {
            Id = id;
            Category = category;
            DurationTurns = durationTurns;
            Value = value;
            StackPolicy = stackPolicy;
            MaxStack = maxStack;
            ModStat = modStat;
            Dispellable = dispellable;
            ValueType = valueType;
            IncomingDamageMult = incomingDamageMult;
        }
    }
}
