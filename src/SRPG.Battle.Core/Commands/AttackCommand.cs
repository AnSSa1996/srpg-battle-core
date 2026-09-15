using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Events;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Commands
{
    /// <summary>
    /// 기본 공격(평타). 행동 직전 대상 방향으로 회전(CMB-431)한 뒤, 데미지 파이프라인(CMB-100~105)으로 피해를 적용한다.
    /// 평타는 물리·스킬배율 100%로 처리한다(개별 스킬은 추후 SkillCommand 로 분리).
    /// </summary>
    public sealed class AttackCommand : ICommand
    {
        private readonly Unit _attacker;
        private readonly Unit _target;

        /// <summary>공격 커맨드를 만든다.</summary>
        public AttackCommand(Unit attacker, Unit target)
        {
            _attacker = attacker;
            _target = target;
        }

        /// <summary>대상으로 회전 후 명중·치명·경감을 거친 피해를 적용한다.</summary>
        public void Apply(BattleState state)
        {
            if (_attacker == null || _target == null)
            {
                return;
            }

            if (_target.IsAlive() == false)
            {
                return;
            }

            _attacker.AddUltGauge(CombatConst.ULT_GAIN_ON_BASIC); // 평타 사용 시 궁극 게이지 충전 (CMB-302)
            _attacker.TurnTo(FacingMath.RotateToward(_attacker.Pos, _target.Pos, _attacker.Facing));

            FaceType face = FacingMath.ClassifyFace(_target.Facing, _attacker.Pos, _target.Pos);

            DamageRequest request = new DamageRequest(
                StatResolver.EffectiveStat(_attacker),
                StatResolver.EffectiveStat(_target),
                true,
                Permille.BASE,
                face,
                FieldEffectResolver.DamageMultPerMille(state.Map, _attacker), // 필드 DamageMod (CMB-520)
                ControlStatus.IncomingDamageMult(_target)); // 취약(freeze 등, CMB-104v)

            DamageResult result = DamageCalculator.Compute(request, state.Rng);
            if (result.Hit == false)
            {
                state.Sink.Emit(BattleEvent.AttackMissed(state.NextSeq(), state.ActionId, _attacker.Slot, _target.Slot));
                return;
            }

            _target.ApplyDamageWithShield(result.Damage);
            _target.AddUltGauge(CombatConst.ULT_GAIN_ON_HIT); // 피격 시 궁극 게이지 충전 (CMB-302)
            state.Sink.Emit(BattleEvent.AttackHit(state.NextSeq(), state.ActionId,
                _attacker.Slot, _target.Slot, result.Damage, result.Crit, face, _target.Hp));

            if (_target.IsAlive() == false)
            {
                state.Sink.Emit(BattleEvent.Died(state.NextSeq(), state.ActionId, _target.Slot));
            }
        }

    }
}
