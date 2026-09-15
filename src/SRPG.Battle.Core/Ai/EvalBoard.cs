using System.Collections.Generic;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 1-ply 전방 시뮬의 보드 가치 평가식. 내 편 유효HP 합 − 상대 유효HP 합(유효HP = HP + 실드).
    /// 정수. (참조: Docs/01_전투/25_AI_스코어링.md AI-140, 04 E-3)
    /// </summary>
    public static class EvalBoard
    {
        /// <summary>지정 편 관점의 보드 가치를 산출한다.</summary>
        public static int Evaluate(BattleState state, Side side)
        {
            int mine = 0;
            int opponent = 0;
            IReadOnlyList<Unit> units = state.Units;

            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.IsAlive() == false)
                {
                    continue;
                }

                int effectiveHp = unit.Hp + unit.Shield;
                if (unit.Side == side)
                {
                    mine += effectiveHp;
                }
                else
                {
                    opponent += effectiveHp;
                }
            }

            return mine - opponent;
        }
    }
}
