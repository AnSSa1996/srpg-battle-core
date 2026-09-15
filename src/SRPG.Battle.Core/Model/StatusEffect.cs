using System;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Model
{
    /// <summary>
    /// 유닛에 부여된 상태 효과의 런타임 인스턴스. 지속 턴·중첩은 전투 중 변한다.
    /// (참조: Docs/01_전투/16_상태이상.md CMB-200~206)
    /// </summary>
    public sealed class StatusEffect
    {
        private const int MIN_STACK = 1;

        private int _remainingTurns;
        private int _stacks;

        /// <summary>효과 식별자.</summary>
        public string Id { get; }

        /// <summary>분류.</summary>
        public StatusCategory Category { get; }

        /// <summary>효과량(‰ 또는 정수). DoT=틱당 고정피해, StatMod=가감‰.</summary>
        public int Value { get; }

        /// <summary>StatMod 일 때 대상 스탯(그 외 분류에서는 무시).</summary>
        public StatType ModStat { get; }

        /// <summary>시전자 슬롯 인덱스(DoT 피해 산정·처리 순서용).</summary>
        public int SourceUnitSlot { get; }

        /// <summary>이 상태 보유 시 대상이 받는 피해 배율(‰, 기본 1000). 취약(freeze 등). (CMB-104v)</summary>
        public int IncomingDamageMult { get; }

        /// <summary>남은 지속(대상 턴 수).</summary>
        public int RemainingTurns { get { return _remainingTurns; } }

        /// <summary>현재 중첩 수.</summary>
        public int Stacks { get { return _stacks; } }

        /// <summary>상태 효과 인스턴스를 만든다.</summary>
        public StatusEffect(string id, StatusCategory category, int value, StatType modStat,
            int sourceUnitSlot, int durationTurns, int stacks, int incomingDamageMult = 1000)
        {
            Id = id;
            Category = category;
            Value = value;
            ModStat = modStat;
            SourceUnitSlot = sourceUnitSlot;
            IncomingDamageMult = incomingDamageMult;
            _remainingTurns = durationTurns;
            _stacks = stacks < MIN_STACK ? MIN_STACK : stacks;
        }

        /// <summary>지속을 최댓값으로 갱신한다(Refresh 정책). (CMB-206)</summary>
        public void RefreshDuration(int durationTurns)
        {
            _remainingTurns = Math.Max(_remainingTurns, durationTurns);
        }

        /// <summary>중첩을 1 늘린다(maxStack 까지). (CMB-206 Stack)</summary>
        public void AddStack(int maxStack)
        {
            if (_stacks < maxStack)
            {
                _stacks += 1;
            }
        }

        /// <summary>남은 지속을 1 줄인다(턴 시작 처리). (CMB-201/202)</summary>
        public void DecrementDuration()
        {
            _remainingTurns -= 1;
        }

        /// <summary>지속이 0 이하라 제거 대상인지 반환한다.</summary>
        public bool IsExpired()
        {
            return _remainingTurns <= 0;
        }

        /// <summary>현재 상태를 그대로 복제한다(1-ply 결정론 클론용).</summary>
        public StatusEffect Clone()
        {
            return new StatusEffect(Id, Category, Value, ModStat, SourceUnitSlot, _remainingTurns, _stacks, IncomingDamageMult);
        }
    }
}
