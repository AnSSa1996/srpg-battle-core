using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 후보를 결정론 클론에 "예상 적용"(난수 없이 EstimateDamage)한 뒤 보드 가치 변화량을 반환한다.
    /// 결정 단계는 무작위 금지(AI-032)라 명중/치명은 기대값으로 본다. (참조: AI-140, 04 E-3)
    /// </summary>
    public static class LookaheadEvaluator
    {
        /// <summary>후보 적용 전후의 EvalBoard 변화량(actor 편 관점)을 반환한다.</summary>
        public static int BoardDelta(BattleState state, Unit actor, Candidate candidate)
        {
            int before = EvalBoard.Evaluate(state, actor.Side);

            BattleState clone = state.DeterministicClone();
            Unit clonedActor = clone.UnitBySlot(actor.Slot);
            if (clonedActor == null)
            {
                return 0;
            }

            clonedActor.MoveTo(candidate.Destination);

            if (candidate.Target != null)
            {
                ApplyExpectedAttack(clone, clonedActor, candidate);
            }

            int after = EvalBoard.Evaluate(clone, actor.Side);
            return after - before;
        }

        private static void ApplyExpectedAttack(BattleState clone, Unit clonedActor, Candidate candidate)
        {
            Unit clonedTarget = clone.UnitBySlot(candidate.Target.Slot);
            if (clonedTarget == null || clonedTarget.IsAlive() == false)
            {
                return;
            }

            FaceType face = FacingMath.ClassifyFace(clonedTarget.Facing, clonedActor.Pos, clonedTarget.Pos);
            DamageRequest request = new DamageRequest(
                StatResolver.EffectiveStat(clonedActor),
                StatResolver.EffectiveStat(clonedTarget),
                true,
                Permille.BASE,
                face,
                Permille.BASE);

            int damage = DamageCalculator.EstimateDamage(request);
            clonedTarget.ApplyDamageWithShield(damage);
        }
    }
}
