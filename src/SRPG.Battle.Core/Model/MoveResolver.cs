using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Grid;

namespace SRPG.Battle.Core.Model
{
    /// <summary>
    /// 강제 이동(밀치기/끌기)의 도착 타일을 결정론으로 산출한다. 시전자→대상 방향의 우세 축(동률=수평)을
    /// 카디널 스텝으로 삼아 밀치기=반대쪽·끌기=시전자쪽으로 한 칸씩 전진하며, 벽·경계·다른 유닛 점유에서 멈춘다.
    /// 끌기는 시전자 타일이 점유돼 있어 그 앞에서 자연히 멈춘다. RNG 미소비. (참조: Docs/01_전투/17_스킬_시스템.md CMB-308 Move)
    /// </summary>
    public static class MoveResolver
    {
        /// <summary>대상의 최종 도착 타일을 반환한다(이동 불가·무효면 현재 위치 그대로).</summary>
        public static Coord Resolve(BattleState state, Unit caster, Unit target, MoveKind kind, int distance)
        {
            if (state == null || caster == null || target == null || kind == MoveKind.None || distance <= 0)
            {
                return target == null ? default : target.Pos;
            }

            Coord step = CardinalStep(caster.Pos, target.Pos);
            if (kind == MoveKind.Pull)
            {
                step = new Coord(-step.X, -step.Y); // 끌기: 시전자 쪽으로
            }

            if (step.X == 0 && step.Y == 0)
            {
                return target.Pos; // 같은 타일(비정상) → 이동 없음
            }

            Coord pos = target.Pos;
            for (int stepIndex = 0; stepIndex < distance; stepIndex++)
            {
                Coord next = pos + step;
                if (state.Map.IsWall(next))
                {
                    break; // 벽·경계에서 멈춤
                }

                if (state.UnitAt(next) != null)
                {
                    break; // 다른 유닛(끌기 시 시전자 포함) 점유에서 멈춤
                }

                pos = next;
            }

            return pos;
        }

        // 시전자→대상 벡터의 우세 축을 카디널 단위 스텝으로. 동률이면 수평(결정론).
        private static Coord CardinalStep(Coord caster, Coord target)
        {
            int dx = target.X - caster.X;
            int dy = target.Y - caster.Y;
            if (Abs(dx) >= Abs(dy))
            {
                return new Coord(Sign(dx), 0);
            }

            return new Coord(0, Sign(dy));
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int Sign(int value)
        {
            if (value > 0)
            {
                return 1;
            }

            return value < 0 ? -1 : 0;
        }
    }
}
