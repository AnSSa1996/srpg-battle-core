using System.Text;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Events
{
    /// <summary>
    /// 전투 상태/이벤트를 텍스트로 렌더링한다(뷰 없이 콘솔 관전·디버깅·AI 튜닝용).
    /// 순수 출력이라 시뮬에 영향 없음. (VIEW-003)
    /// </summary>
    public static class AsciiRenderer
    {
        private const char WALL_CHAR = '#';
        private const char EMPTY_CHAR = '.';
        private const char ALLY_CHAR = 'A';
        private const char ENEMY_CHAR = 'E';

        /// <summary>현재 그리드를 문자 격자로 렌더링한다(살아있는 유닛만 표시).</summary>
        public static string RenderGrid(BattleState state)
        {
            StringBuilder builder = new StringBuilder();
            for (int y = 0; y < state.Map.Height; y++)
            {
                for (int x = 0; x < state.Map.Width; x++)
                {
                    builder.Append(CellChar(state, new Coord(x, y)));
                }

                builder.Append('\n');
            }

            return builder.ToString();
        }

        /// <summary>이벤트 하나를 사람이 읽는 한 줄로 변환한다.</summary>
        public static string DescribeEvent(BattleEvent battleEvent)
        {
            switch (battleEvent.Type)
            {
                case BattleEventType.TurnStarted:
                    return "turn  unit#" + battleEvent.Slot + " (CT " + battleEvent.HpAfter + ")";
                case BattleEventType.Moved:
                    return "move  unit#" + battleEvent.Slot + " -> (" + battleEvent.ToX + "," + battleEvent.ToY + ")";
                case BattleEventType.AttackHit:
                    return "hit   unit#" + battleEvent.Slot + " -> unit#" + battleEvent.TargetSlot
                        + " dmg " + battleEvent.Amount + (battleEvent.IsCrit ? " CRIT" : "")
                        + " [" + battleEvent.Face + "] hpAfter " + battleEvent.HpAfter;
                case BattleEventType.AttackMissed:
                    return "miss  unit#" + battleEvent.Slot + " -> unit#" + battleEvent.TargetSlot;
                case BattleEventType.Died:
                    return "die   unit#" + battleEvent.Slot;
                case BattleEventType.BattleEnded:
                    return "end   " + (battleEvent.Victory ? "VICTORY" : "DEFEAT");
                default:
                    return "event " + battleEvent.Type;
            }
        }

        private static char CellChar(BattleState state, Coord cell)
        {
            if (state.Map.IsWall(cell))
            {
                return WALL_CHAR;
            }

            Unit unit = state.UnitAt(cell);
            if (unit == null)
            {
                return EMPTY_CHAR;
            }

            return unit.Side == Side.Ally ? ALLY_CHAR : ENEMY_CHAR;
        }
    }
}
