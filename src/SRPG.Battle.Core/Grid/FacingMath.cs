using System;

namespace SRPG.Battle.Core.Grid
{
    /// <summary>
    /// 방향(facing) 회전과 피격 면 판정. 전부 정수 기하라 같은 입력이면 항상 같은 결과.
    /// (참조: Docs/01_전투/22_전투_그리드_규칙.md CMB-430~434)
    /// </summary>
    public static class FacingMath
    {
        /// <summary>facing 을 단위 벡터로 변환한다. N=(0,-1), E=(1,0), S=(0,1), W=(-1,0).</summary>
        public static Coord ToVector(Facing facing)
        {
            switch (facing)
            {
                case Facing.North: return new Coord(0, -1);
                case Facing.East: return new Coord(1, 0);
                case Facing.South: return new Coord(0, 1);
                case Facing.West: return new Coord(-1, 0);
                default: return new Coord(0, -1);
            }
        }

        /// <summary>
        /// self 가 target 을 향하도록 회전한 facing 을 반환한다.
        /// |dx|≥|dy| 면 좌우(E/W), 아니면 상하(N/S). 동률이면 좌우 우선. 같은 칸이면 현재 facing 유지. (CMB-431)
        /// </summary>
        public static Facing RotateToward(Coord self, Coord target, Facing current)
        {
            int dx = target.X - self.X;
            int dy = target.Y - self.Y;

            if (dx == 0 && dy == 0)
            {
                return current;
            }

            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                return dx > 0 ? Facing.East : Facing.West;
            }

            return dy > 0 ? Facing.South : Facing.North;
        }

        /// <summary>
        /// 피격 면을 판정한다. 피격자 facing 과 (공격자−피격자) 벡터의 정면축/측면축 우세로 분류한다.
        /// 정면축이 우세하고 같은 방향이면 정면, 반대면 후면, 그 외(측면 우세/동률)는 측면. (CMB-433)
        /// </summary>
        public static FaceType ClassifyFace(Facing targetFacing, Coord attackerPos, Coord targetPos)
        {
            Coord facingVector = ToVector(targetFacing);
            int dx = attackerPos.X - targetPos.X;
            int dy = attackerPos.Y - targetPos.Y;

            int along = (dx * facingVector.X) + (dy * facingVector.Y);
            int perpendicular = Math.Abs((dx * facingVector.Y) - (dy * facingVector.X));
            int alongMagnitude = Math.Abs(along);

            if (alongMagnitude > perpendicular)
            {
                return along > 0 ? FaceType.Front : FaceType.Back;
            }

            return FaceType.Side;
        }
    }
}
