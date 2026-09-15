using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;

namespace SRPG.Battle.Core.Model
{
    /// <summary>
    /// 제어형 상태(침묵·도발)의 판정 SSOT. AI 의사결정(UtilityDecider·CandidateGenerator)이 참조한다.
    /// CC(스턴/빙결)와 달리 이들은 턴을 막지 않고 "무엇을 할 수 있는지"만 제한한다. 전부 결정론.
    /// (참조: Docs/01_전투/16_상태이상.md CMB-200, Docs/01_전투/25_AI_스코어링.md AI-170)
    /// </summary>
    public static class ControlStatus
    {
        /// <summary>침묵 상태면 true(액티브·궁극 사용 불가, 평타만). (CMB-200)</summary>
        public static bool IsSilenced(Unit unit)
        {
            if (unit == null)
            {
                return false;
            }

            IReadOnlyList<StatusEffect> statuses = unit.Statuses;
            for (int index = 0; index < statuses.Count; index++)
            {
                if (statuses[index].Category == StatusCategory.Silence)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 도발로 강제된 공격 대상을 반환한다(없으면 null). 유닛에 걸린 Taunt 중
        /// 시전자가 살아있는 적인 것만 유효하며, 여럿이 겹치면 가장 최근 부여된 것이 우선한다(부여 순서=상태 목록 순서, 결정론). (AI-170/171)
        /// </summary>
        public static Unit ForcedTauntTarget(BattleState state, Unit unit)
        {
            if (state == null || unit == null)
            {
                return null;
            }

            Unit forced = null;
            IReadOnlyList<StatusEffect> statuses = unit.Statuses;
            for (int index = 0; index < statuses.Count; index++)
            {
                StatusEffect status = statuses[index];
                if (status.Category != StatusCategory.Taunt)
                {
                    continue;
                }

                Unit source = state.UnitBySlot(status.SourceUnitSlot);
                if (source == null || source.IsAlive() == false || source.Side == unit.Side)
                {
                    continue; // 시전자 사망·아군 도발(비정상)은 무시
                }

                forced = source; // 뒤에 오는(더 최근 부여) 유효 도발이 우선 (AI-171)
            }

            return forced;
        }

        /// <summary>
        /// 대상이 받는 피해 배율(‰)을 반환한다. 취약 상태(freeze 등, IncomingDamageMult≠1000)를 누적 곱한다.
        /// 취약이 없으면 1000(변화 없음). 데미지 파이프라인 마지막 배율(CMB-104v). (CMB-104v)
        /// </summary>
        public static int IncomingDamageMult(Unit target)
        {
            if (target == null)
            {
                return Permille.BASE;
            }

            long mult = Permille.BASE;
            IReadOnlyList<StatusEffect> statuses = target.Statuses;
            for (int index = 0; index < statuses.Count; index++)
            {
                mult = mult * statuses[index].IncomingDamageMult / Permille.BASE;
            }

            return (int)mult;
        }
    }
}
