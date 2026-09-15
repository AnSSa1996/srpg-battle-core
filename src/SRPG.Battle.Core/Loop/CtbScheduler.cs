using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>
    /// SPD 기반 CTB(Charge Turn Battle) 스케줄러. 틱 일괄 전진으로 다음 행동자를 결정론적으로 선정한다.
    /// 도달틱 = ceil((THRESHOLD − CT) / SPD), 최소 유닛이 다음 행동자.
    /// 동시 도달 시 ① SPD 높은 유닛 → ② 슬롯 인덱스 낮은 유닛. 무작위 없음.
    /// (참조: Docs/01_전투/04_전투_시스템.md CMB-010~016)
    /// </summary>
    public static class CtbScheduler
    {
        private const int MIN_SPD = 1; // SPD 는 최소 1 (0 나눗셈 방지, GROW 유효스탯 하한과 일치)

        /// <summary>다음에 행동할 유닛과 전진 틱 수를 선정한다(살아있는 유닛만). 없으면 null.</summary>
        public static TurnResolution ResolveNext(IReadOnlyList<Unit> units)
        {
            if (units == null)
            {
                return null;
            }

            Unit bestUnit = null;
            int bestTicks = 0;
            int bestSpd = 0;

            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.IsAlive() == false)
                {
                    continue;
                }

                int spd = SpeedOf(unit);
                int ticks = TicksToThreshold(unit.Ct, spd);

                bool isBetter = bestUnit == null
                    || ticks < bestTicks
                    || (ticks == bestTicks && spd > bestSpd);

                if (isBetter)
                {
                    bestUnit = unit;
                    bestTicks = ticks;
                    bestSpd = spd;
                }
            }

            if (bestUnit == null)
            {
                return null;
            }

            return new TurnResolution(bestUnit, bestTicks);
        }

        /// <summary>모든 살아있는 유닛의 CT 를 ticks 만큼 일괄 전진하고 틱 카운터를 올린다. (CMB-014)</summary>
        public static void AdvanceAll(BattleState state, int ticks)
        {
            if (state == null)
            {
                return;
            }

            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.IsAlive())
                {
                    unit.AddCt(SpeedOf(unit) * ticks);
                }
            }

            state.AdvanceTick(ticks);
        }

        private static int TicksToThreshold(int ct, int spd)
        {
            int remaining = CombatConst.ACTION_THRESHOLD - ct;
            if (remaining <= 0)
            {
                return 0;
            }

            return (remaining + spd - 1) / spd; // 정수 올림(ceil)
        }

        private static int SpeedOf(Unit unit)
        {
            // 유효 SPD: 가속/둔화 등 StatMod 가 CT 충전에 반영된다 (CMB-208)
            int spd = StatResolver.EffectiveStat(unit).Get(StatType.Spd);
            return spd < MIN_SPD ? MIN_SPD : spd;
        }
    }
}
