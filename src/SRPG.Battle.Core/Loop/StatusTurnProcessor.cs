using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Events;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>
    /// 대상이 턴을 시작할 때의 상태이상 처리. 고정 순서: ① DoT 적용 → ② CC 판정 → ③ 지속 감소·만료 제거.
    /// CC 로 막혔는지 여부를 반환한다(막혀도 CT 는 소비된다). DoT 피해·DoT 사망은 이벤트로 방출한다(연출용).
    /// (참조: Docs/01_전투/16_상태이상.md CMB-202/207)
    /// </summary>
    public static class StatusTurnProcessor
    {
        /// <summary>턴 시작 처리를 수행하고, CC 로 행동이 막혔으면 true 를 반환한다.</summary>
        public static bool ProcessTurnStart(BattleState state, Unit unit)
        {
            if (unit == null)
            {
                return false;
            }

            // 반복 중 제거를 위해 스냅샷(부여 순서 유지 = 결정론)
            List<StatusEffect> snapshot = new List<StatusEffect>(unit.Statuses);

            ApplyDamageOverTime(state, unit, snapshot);
            ApplyFieldTurnTick(state, unit); // 필드 TurnTick: 매 턴 고정피해 (CMB-520)
            if (unit.IsAlive() == false)
            {
                state.Sink.Emit(BattleEvent.Died(state.NextSeq(), state.ActionId, unit.Slot));
                return false; // 사망 시 CC·지속 감소는 의미 없음
            }

            bool blocked = HasControlEffect(snapshot);
            DecrementAndExpire(unit, snapshot);

            return blocked;
        }

        // 맵 TurnTick 필드효과(filter 일치): TickStatus 가 있으면 매 턴 그 상태 부여(상태형, 환경효과라 무판정),
        // 없으면 Amount 만큼 고정피해(고정피해형, DoT 처럼 StatusDamage 로 연출).
        private static void ApplyFieldTurnTick(BattleState state, Unit unit)
        {
            IReadOnlyList<FieldEffect> effects = state.Map.FieldEffects;
            int directDamage = 0;
            for (int index = 0; index < effects.Count; index++)
            {
                FieldEffect fieldEffect = effects[index];
                if (fieldEffect.ApplyType != FieldApplyType.TurnTick || FieldEffectResolver.Matches(fieldEffect, unit) == false)
                {
                    continue;
                }

                if (fieldEffect.TickStatus != null)
                {
                    StatusSystem.TryApply(state, unit, unit, fieldEffect.TickStatus, false); // 환경 효과: 시전자=대상, 무판정
                }
                else
                {
                    directDamage += fieldEffect.Amount;
                }
            }

            if (directDamage <= 0)
            {
                return;
            }

            unit.SetHp(unit.Hp - directDamage);
            state.Sink.Emit(BattleEvent.StatusDamage(state.NextSeq(), state.ActionId, unit.Slot, directDamage, unit.Hp));
        }

        private static void ApplyDamageOverTime(BattleState state, Unit unit, List<StatusEffect> statuses)
        {
            for (int index = 0; index < statuses.Count; index++)
            {
                StatusEffect status = statuses[index];
                if (status.Category != StatusCategory.DoT)
                {
                    continue;
                }

                int baseDamage = status.Value * status.Stacks; // 스냅샷된 틱 피해 × 중첩(방어·방향 무시, CMB-107)
                if (baseDamage <= 0)
                {
                    continue;
                }

                // 필드배율 적용 후 최소 1 보장(CMB-107). 필드배율은 시전자 기준(부재 시 1000=무효).
                int fieldMult = FieldMultForSource(state, status.SourceUnitSlot);
                int damage = (int)((long)baseDamage * fieldMult / Permille.BASE);
                if (damage < CombatConst.MIN_DAMAGE)
                {
                    damage = CombatConst.MIN_DAMAGE;
                }

                unit.SetHp(unit.Hp - damage);
                state.Sink.Emit(BattleEvent.StatusDamage(state.NextSeq(), state.ActionId, unit.Slot, damage, unit.Hp));
            }
        }

        // DoT 시전자의 필드 데미지 배율(‰). 시전자를 못 찾으면 무효(1000). (CMB-107)
        private static int FieldMultForSource(BattleState state, int sourceSlot)
        {
            Unit source = state.UnitBySlot(sourceSlot);
            if (source == null)
            {
                return Permille.BASE;
            }

            return FieldEffectResolver.DamageMultPerMille(state.Map, source);
        }

        private static bool HasControlEffect(List<StatusEffect> statuses)
        {
            for (int index = 0; index < statuses.Count; index++)
            {
                if (statuses[index].Category == StatusCategory.CC)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DecrementAndExpire(Unit unit, List<StatusEffect> statuses)
        {
            for (int index = 0; index < statuses.Count; index++)
            {
                StatusEffect status = statuses[index];
                status.DecrementDuration();
                if (status.IsExpired())
                {
                    unit.RemoveStatus(status);
                }
            }
        }
    }
}
