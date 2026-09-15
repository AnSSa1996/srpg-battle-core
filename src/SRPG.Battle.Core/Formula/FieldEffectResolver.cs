using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Formula
{
    /// <summary>
    /// 맵 필드효과(CMB-520~523)를 적용 지점별로 산출한다. filter 일치 유닛에만 적용한다.
    /// DamageMod=배율 순차 곱(CMB-522), StatMod=‰ 가산, MovMod=이동력 가감. TurnTick 은 StatusTurnProcessor 가
    /// Matches 로 판정해 직접 처리(상태 부여형/고정피해형). (applyType 4종 — RangeMod 제외) (참조: 23_전투맵_데이터.md)
    /// </summary>
    public static class FieldEffectResolver
    {
        /// <summary>filter 일치 DamageMod 들을 순차 곱한 데미지 필드배율(‰). 없으면 1000. (CMB-520 DamageMod, CMB-104f)</summary>
        public static int DamageMultPerMille(BattleMap map, Unit attacker)
        {
            int mult = Permille.BASE;
            IReadOnlyList<FieldEffect> effects = map.FieldEffects;
            for (int index = 0; index < effects.Count; index++)
            {
                FieldEffect fieldEffect = effects[index];
                if (fieldEffect.ApplyType != FieldApplyType.DamageMod || Matches(fieldEffect, attacker) == false)
                {
                    continue;
                }

                mult = mult * fieldEffect.Amount / Permille.BASE;
            }

            return mult;
        }

        /// <summary>filter 일치 StatMod 들을 스탯별 ‰ 가산한 블록(전투 시작 시 1회 적용용). (CMB-520 StatMod)</summary>
        public static StatBlock StatPercent(BattleMap map, Unit unit)
        {
            int[] percentPerMille = new int[StatBlock.STAT_COUNT];
            IReadOnlyList<FieldEffect> effects = map.FieldEffects;
            for (int index = 0; index < effects.Count; index++)
            {
                FieldEffect fieldEffect = effects[index];
                if (fieldEffect.ApplyType != FieldApplyType.StatMod || Matches(fieldEffect, unit) == false)
                {
                    continue;
                }

                percentPerMille[(int)fieldEffect.TargetStat] += fieldEffect.Amount;
            }

            return new StatBlock(percentPerMille);
        }

        /// <summary>filter 일치 MovMod 들의 이동력 가감 합(타일, flat). Reach 계산 MOV_eff 에 더한다. (CMB-520 MovMod)</summary>
        public static int MovDelta(BattleMap map, Unit unit)
        {
            int delta = 0;
            IReadOnlyList<FieldEffect> effects = map.FieldEffects;
            for (int index = 0; index < effects.Count; index++)
            {
                FieldEffect fieldEffect = effects[index];
                if (fieldEffect.ApplyType == FieldApplyType.MovMod && Matches(fieldEffect, unit))
                {
                    delta += fieldEffect.Amount;
                }
            }

            return delta;
        }

        /// <summary>filter 가 해당 유닛에 적용되는지 판정한다. (CMB-521)</summary>
        public static bool Matches(FieldEffect fieldEffect, Unit unit)
        {
            switch (fieldEffect.FilterKind)
            {
                case FieldFilterKind.All:
                    return true;
                case FieldFilterKind.Class:
                    return unit.Class == fieldEffect.FilterClass;
                case FieldFilterKind.Side:
                    return unit.Side == fieldEffect.FilterSide;
                default:
                    return false;
            }
        }
    }
}
