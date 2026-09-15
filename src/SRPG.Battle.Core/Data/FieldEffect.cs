using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Data
{
    /// <summary>
    /// 층 필드효과 한 줄(불변 입력 데이터). applyType 에 따라 적용 지점이 고정된다.
    /// filter 로 대상 범위를 좁힌다. (참조: Docs/01_전투/23_전투맵_데이터.md CMB-520~523)
    /// </summary>
    public sealed class FieldEffect
    {
        /// <summary>효과 식별자.</summary>
        public string Id { get; }

        /// <summary>적용 지점 종류.</summary>
        public FieldApplyType ApplyType { get; }

        /// <summary>대상 범위 종류.</summary>
        public FieldFilterKind FilterKind { get; }

        /// <summary>FilterKind=Class 일 때 대상 클래스.</summary>
        public HeroClass FilterClass { get; }

        /// <summary>FilterKind=Side 일 때 대상 편.</summary>
        public Side FilterSide { get; }

        /// <summary>StatMod 일 때 대상 스탯.</summary>
        public StatType TargetStat { get; }

        /// <summary>효과량(DamageMod=배율‰, StatMod=가감‰, MovMod=delta, TurnTick=고정피해 value(TickStatus 없을 때)).</summary>
        public int Amount { get; }

        /// <summary>TurnTick 일 때 매 턴 부여할 상태 정의(있으면 상태 부여형, null 이면 Amount 만큼 고정피해형).</summary>
        public StatusDef TickStatus { get; }

        /// <summary>필드효과를 만든다.</summary>
        public FieldEffect(string id, FieldApplyType applyType, FieldFilterKind filterKind,
            HeroClass filterClass, Side filterSide,
            StatType targetStat, int amount, StatusDef tickStatus)
        {
            Id = id;
            ApplyType = applyType;
            FilterKind = filterKind;
            FilterClass = filterClass;
            FilterSide = filterSide;
            TargetStat = targetStat;
            Amount = amount;
            TickStatus = tickStatus;
        }
    }
}
