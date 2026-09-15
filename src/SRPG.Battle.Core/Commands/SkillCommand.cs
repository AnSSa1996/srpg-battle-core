using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Events;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Commands
{
    /// <summary>
    /// 액티브/궁극 스킬을 실행한다. 타게팅·AoE 로 대상을 정하고 effects[] 를 배열 순서대로 적용한다.
    /// 공격형은 대상마다 명중 1회(빗나가면 그 대상 모든 효과 무효), AoE 는 슬롯 인덱스 오름차순.
    /// 난수 소비 순서: 명중 → 치명 → 상태. 사용 후 쿨다운 설정, 궁극이면 게이지 0.
    /// (참조: Docs/01_전투/17_스킬_시스템.md CMB-300~308, 15 CMB-110~113)
    /// </summary>
    public sealed class SkillCommand : ICommand
    {
        private readonly Unit _caster;
        private readonly SkillDef _skill;
        private readonly Unit _primaryTarget;

        /// <summary>스킬 커맨드를 만든다(primaryTarget 은 Self 스킬이면 null 가능).</summary>
        public SkillCommand(Unit caster, SkillDef skill, Unit primaryTarget)
        {
            _caster = caster;
            _skill = skill;
            _primaryTarget = primaryTarget;
        }

        /// <summary>스킬을 실행한다.</summary>
        public void Apply(BattleState state)
        {
            if (_caster == null || _skill == null)
            {
                return;
            }

            state.Sink.Emit(BattleEvent.SkillCast(state.NextSeq(), state.ActionId, _caster.Slot, (int)_skill.Type));

            if (_skill.IsOffensive && _primaryTarget != null)
            {
                _caster.TurnTo(FacingMath.RotateToward(_caster.Pos, _primaryTarget.Pos, _caster.Facing));
            }

            List<Unit> targets = ResolveTargets(state);
            for (int index = 0; index < targets.Count; index++)
            {
                ApplyToTarget(state, targets[index]);
            }

            _caster.PutOnCooldown(_skill.Id, _skill.Cooldown);
            if (_skill.Type == SkillType.Ultimate)
            {
                _caster.SetUltGauge(0);
            }
        }

        private List<Unit> ResolveTargets(BattleState state)
        {
            List<Unit> targets = new List<Unit>();
            switch (_skill.Targeting)
            {
                case TargetingType.Self:
                    targets.Add(_caster);
                    return targets;
                case TargetingType.AoeEnemy:
                    return UnitsInAoe(state, true);
                case TargetingType.AoeAlly:
                    return UnitsInAoe(state, false);
                default:
                    // SingleEnemy / SingleAlly / Line / Point: 1차 대상만 (Line/Point 는 후속 확장)
                    if (_primaryTarget != null)
                    {
                        targets.Add(_primaryTarget);
                    }

                    return targets;
            }
        }

        private List<Unit> UnitsInAoe(BattleState state, bool enemiesOfCaster)
        {
            List<Unit> targets = new List<Unit>();
            if (_primaryTarget == null)
            {
                return targets;
            }

            List<Coord> tiles = AoeShape.GetTiles(_skill.Aoe, _primaryTarget.Pos, _caster.Facing, _skill.AoeSize);
            HashSet<Coord> tileSet = new HashSet<Coord>(tiles);

            IReadOnlyList<Unit> units = state.Units; // 슬롯 순서 = 결정론 (CMB-308a)
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.IsAlive() == false || tileSet.Contains(unit.Pos) == false)
                {
                    continue;
                }

                bool isEnemy = unit.Side != _caster.Side;
                if (isEnemy == enemiesOfCaster)
                {
                    targets.Add(unit);
                }
            }

            return targets;
        }

        private void ApplyToTarget(BattleState state, Unit target)
        {
            if (target.IsAlive() == false)
            {
                return;
            }

            FaceType face = FacingMath.ClassifyFace(target.Facing, _caster.Pos, target.Pos);

            if (_skill.IsOffensive)
            {
                ApplyOffensive(state, target, face);
            }
            else
            {
                ApplySupport(state, target);
            }
        }

        private void ApplyOffensive(BattleState state, Unit target, FaceType face)
        {
            DamageRequest hitRequest = new DamageRequest(
                StatResolver.EffectiveStat(_caster), StatResolver.EffectiveStat(target),
                true, Permille.BASE, face, Permille.BASE);

            if (state.Rng.NextPerMille() >= DamageCalculator.HitChance(hitRequest))
            {
                state.Sink.Emit(BattleEvent.AttackMissed(state.NextSeq(), state.ActionId, _caster.Slot, target.Slot));
                return; // 빗나가면 그 대상 모든 효과 무효 (CMB-111)
            }

            bool crit = state.Rng.NextPerMille() < DamageCalculator.CritChance(hitRequest);

            IReadOnlyList<SkillEffect> effects = _skill.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                SkillEffect effect = effects[index];
                if (effect.Type == EffectType.Damage)
                {
                    ApplyDamageEffect(state, target, effect, face, crit);
                }
                else if (effect.Type == EffectType.ApplyStatus)
                {
                    TryApplyStatus(state, target, effect, true);
                }
                else if (effect.Type == EffectType.Shield)
                {
                    GrantShield(state, target, effect.Value);
                }
                else if (effect.Type == EffectType.GaugeMod)
                {
                    target.AddUltGauge(effect.Value);
                }
                else if (effect.Type == EffectType.Move)
                {
                    ApplyMove(state, target, effect);
                }
            }
        }

        private void ApplySupport(BattleState state, Unit target)
        {
            bool isDebuff = target.Side != _caster.Side;
            IReadOnlyList<SkillEffect> effects = _skill.Effects;
            for (int index = 0; index < effects.Count; index++)
            {
                SkillEffect effect = effects[index];
                if (effect.Type == EffectType.Heal)
                {
                    ApplyHeal(state, target, effect);
                }
                else if (effect.Type == EffectType.ApplyStatus)
                {
                    TryApplyStatus(state, target, effect, isDebuff);
                }
                else if (effect.Type == EffectType.Shield)
                {
                    GrantShield(state, target, effect.Value);
                }
                else if (effect.Type == EffectType.GaugeMod)
                {
                    target.AddUltGauge(effect.Value);
                }
                else if (effect.Type == EffectType.Move)
                {
                    ApplyMove(state, target, effect);
                }
            }
        }

        // 강제 이동(밀치기/끌기): 도착 타일을 결정론으로 산출해 이동하고 Moved 이벤트를 방출한다(뷰 동기화). (CMB-308 Move)
        private void ApplyMove(BattleState state, Unit target, SkillEffect effect)
        {
            if (target == null || target.IsAlive() == false)
            {
                return;
            }

            Coord destination = MoveResolver.Resolve(state, _caster, target, effect.MoveKind, effect.Value);
            if (destination == target.Pos)
            {
                return; // 이동 없음(벽/점유로 막힘)
            }

            target.MoveTo(destination);
            state.Sink.Emit(BattleEvent.Moved(state.NextSeq(), state.ActionId, target.Slot, destination));
        }

        private void ApplyDamageEffect(BattleState state, Unit target, SkillEffect effect, FaceType face, bool crit)
        {
            DamageRequest request = new DamageRequest(
                StatResolver.EffectiveStat(_caster), StatResolver.EffectiveStat(target),
                effect.IsPhysical, effect.Power, face,
                FieldEffectResolver.DamageMultPerMille(state.Map, _caster), // 필드 DamageMod (CMB-520)
                ControlStatus.IncomingDamageMult(target)); // 취약(freeze 등, CMB-104v)

            int damage = DamageCalculator.DamageValue(request, crit);
            target.ApplyDamageWithShield(damage);
            target.AddUltGauge(CombatConst.ULT_GAIN_ON_HIT);

            state.Sink.Emit(BattleEvent.AttackHit(state.NextSeq(), state.ActionId,
                _caster.Slot, target.Slot, damage, crit, face, target.Hp));

            if (target.IsAlive() == false)
            {
                state.Sink.Emit(BattleEvent.Died(state.NextSeq(), state.ActionId, target.Slot));
            }
        }

        private void ApplyHeal(BattleState state, Unit target, SkillEffect effect)
        {
            long magic = StatResolver.EffectiveStat(_caster).Get(StatType.Mag);
            long fieldMult = FieldEffectResolver.DamageMultPerMille(state.Map, _caster); // 필드 회복 증폭 (CMB-106)
            int heal = (int)(magic * effect.Power / Permille.BASE * fieldMult / Permille.BASE);
            if (heal < CombatConst.MIN_HEAL)
            {
                heal = CombatConst.MIN_HEAL;
            }

            int maxHp = target.EntryStat.Get(StatType.Hp);
            int before = target.Hp;
            int healed = before + heal;
            target.SetHp(healed > maxHp ? maxHp : healed);

            int restored = target.Hp - before;
            if (restored > 0)
            {
                state.Sink.Emit(BattleEvent.Healed(state.NextSeq(), state.ActionId, target.Slot, restored, target.Hp));
            }
        }

        // 실드를 더하고 획득 이벤트를 방출한다(연출용). (CMB-108)
        private static void GrantShield(BattleState state, Unit target, int value)
        {
            target.SetShield(target.Shield + value);
            state.Sink.Emit(BattleEvent.ShieldGained(state.NextSeq(), state.ActionId, target.Slot, value));
        }

        private void TryApplyStatus(BattleState state, Unit target, SkillEffect effect, bool isDebuff)
        {
            bool applied = StatusSystem.TryApply(state, _caster, target, effect.AppliedStatus, isDebuff);
            if (applied == false)
            {
                return;
            }

            int seq = state.NextSeq();
            if (isDebuff)
            {
                state.Sink.Emit(BattleEvent.DebuffApplied(seq, state.ActionId, target.Slot));
            }
            else
            {
                state.Sink.Emit(BattleEvent.BuffApplied(seq, state.ActionId, target.Slot));
            }
        }

    }
}
