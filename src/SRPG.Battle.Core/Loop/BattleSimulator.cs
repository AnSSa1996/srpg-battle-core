using System.Collections.Generic;
using SRPG.Battle.Core.Commands;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Events;
using SRPG.Battle.Core.Formula;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>
    /// 전투 루프. 다음 행동자 선정 → 틱 전진 → 행동 결정·적용 → CT 차감 → 종료 판정을 반복한다.
    /// seed + 초기 배치가 같으면 항상 같은 결과(결정론). (참조: CMB-050~057)
    /// 종료 판정 순서: ① 승리 목표 → ② 아군 전멸 → ③ 타임아웃. (CMB-053/054)
    /// </summary>
    public sealed class BattleSimulator
    {
        private readonly IActionDecider _decider;

        /// <summary>행동 결정기를 주입해 시뮬레이터를 만든다(M4 더미, 이후 M6 AI).</summary>
        public BattleSimulator(IActionDecider decider)
        {
            _decider = decider;
        }

        /// <summary>전투를 끝까지 진행해 결과를 반환한다.</summary>
        public BattleResult Run(BattleState state)
        {
            ApplyFieldStatMods(state); // 전투 시작 시 필드 StatMod 1회 적용 (CMB-520). 클론은 Run 을 거치지 않아 중복 없음.

            while (true)
            {
                // 증원 웨이브: 현재 적이 전멸했어도 남은 웨이브가 있으면 다음 웨이브를 소환하고 승리를 보류한다. (CMB-512 웨이브)
                if (AnyAlive(state, Side.Enemy) == false && state.HasPendingWaves)
                {
                    SpawnWave(state);
                    continue;
                }

                BattleResult terminal = CheckEnd(state);
                if (terminal != null)
                {
                    state.Sink.Emit(BattleEvent.BattleEnded(state.NextSeq(), state.ActionId, terminal.Outcome == BattleOutcome.Victory));
                    return terminal;
                }

                TurnResolution resolution = CtbScheduler.ResolveNext(state.Units);
                if (resolution == null)
                {
                    return new BattleResult(BattleOutcome.Defeat, state.ActionCount, state.Tick);
                }

                CtbScheduler.AdvanceAll(state, resolution.TicksAdvanced);

                Unit actor = resolution.Actor;

                state.BeginAction();
                state.Sink.Emit(BattleEvent.TurnStarted(state.NextSeq(), state.ActionId, actor.Slot, actor.Ct));

                actor.TickCooldowns(); // 자기 턴 시작 시 스킬 쿨다운 감소 (CMB-304)

                // 턴 시작 처리: DoT → CC 판정 → 지속 감소 (CMB-202). DoT 피해·사망은 내부에서 이벤트 방출.
                bool controlBlocked = StatusTurnProcessor.ProcessTurnStart(state, actor);

                // 턴 시작 DoT 로 사망하면 행동·CT 차감 없이 다음 루프(종료 판정)로
                if (actor.IsAlive() == false)
                {
                    continue;
                }

                // CC 로 막히지 않은 경우에만 행동 결정·실행
                if (controlBlocked == false)
                {
                    IReadOnlyList<ICommand> commands = _decider.Decide(state, actor);
                    for (int index = 0; index < commands.Count; index++)
                    {
                        commands[index].Apply(state);
                    }
                }

                actor.SetCt(actor.Ct - CombatConst.ACTION_THRESHOLD); // 행동 후 임계값 차감(막혀도 턴 소비, CMB-013/207)
                state.IncrementActionCount();
                UpdateCaptureHold(state); // 거점 점령 목표: 이번 행동 직후 통제 여부로 연속 점령 수 증감 (CMB-512)
            }
        }

        // 거점 점령(Capture) 목표일 때, 이번 행동 직후 아군이 거점을 통제하면 연속 점령 수를 늘리고, 잃으면 0 으로 리셋한다.
        // CheckEnd 의 ObjectiveResolver.IsMet 이 다음 루프에서 이 누적치를 holdTurns 와 비교한다. 다른 목표면 무동작.
        private static void UpdateCaptureHold(BattleState state)
        {
            Objective objective = state.Map.Objective;
            if (objective == null || objective.Type != ObjectiveType.Capture)
            {
                return;
            }

            if (ObjectiveResolver.AllyControlsCapturePoint(state, objective))
            {
                state.IncrementCaptureHold();
            }
            else
            {
                state.ResetCaptureHold();
            }

            state.Sink.Emit(BattleEvent.CaptureProgress(state.NextSeq(), state.ActionId, state.CaptureHoldTurns, objective.Amount)); // HUD 게이지용
        }

        // 증원 웨이브를 소환하고(활성 목록 추가) 새 유닛에 필드효과를 적용한 뒤 WaveSpawned 이벤트를 방출한다. (CMB-512 웨이브)
        private static void SpawnWave(BattleState state)
        {
            IReadOnlyList<Unit> spawned = state.SpawnNextWave();
            ApplyFieldStatMods(state); // 새 유닛에 StatMod 적용(SetFieldStatPercent 는 set 이라 기존 유닛엔 idempotent)
            state.Sink.Emit(BattleEvent.WaveSpawned(state.NextSeq(), state.ActionId, state.WaveIndex, spawned.Count));
        }

        // 필드효과 StatMod 를 모든 유닛에 전투 시작 시 1회 적용한다(상시·고정, CMB-520). 이후 EffectiveStat 이 합산.
        private static void ApplyFieldStatMods(BattleState state)
        {
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                unit.SetFieldStatPercent(FieldEffectResolver.StatPercent(state.Map, unit));
            }
        }

        // 종료 판정(CMB-053/054): ① 승리목표 충족(승리 우선) → ② 아군 전멸(패배) → ③ 타임아웃(HP 비교).
        private static BattleResult CheckEnd(BattleState state)
        {
            // 무손실 사수 던전(LoseOnAllyDeath): 아군이 하나라도 죽으면 즉시 패배(승리 판정보다 우선). (CONT-003)
            if (state.Map.Objective != null && state.Map.Objective.LoseOnAllyDeath && AnyAllyDead(state))
            {
                return new BattleResult(BattleOutcome.Defeat, state.ActionCount, state.Tick);
            }

            // 호위 던전(Escort): 호위 대상(지휘관=최소 슬롯 아군)이 죽으면 즉시 패배. (CONT-003)
            if (state.Map.Objective != null && state.Map.Objective.Type == ObjectiveType.Escort && EscortLeaderDead(state))
            {
                return new BattleResult(BattleOutcome.Defeat, state.ActionCount, state.Tick);
            }

            if (ObjectiveResolver.IsMet(state, state.Map.Objective))
            {
                return new BattleResult(BattleOutcome.Victory, state.ActionCount, state.Tick);
            }

            if (AnyAlive(state, Side.Ally) == false)
            {
                return new BattleResult(BattleOutcome.Defeat, state.ActionCount, state.Tick);
            }

            if (state.ActionCount >= state.Map.TurnLimit)
            {
                // 속전·DPS 던전(FailOnTimeout): 제한 턴 내 목표 미달성이면 패배. 그 외는 HP 비교(CMB-054).
                if (state.Map.Objective != null && state.Map.Objective.FailOnTimeout)
                {
                    return new BattleResult(BattleOutcome.Defeat, state.ActionCount, state.Tick);
                }

                return JudgeByHp(state);
            }

            return null;
        }

        private static BattleResult JudgeByHp(BattleState state)
        {
            int allyHp = SumHp(state, Side.Ally);
            int enemyHp = SumHp(state, Side.Enemy);

            // 동률이면 공격 측(아군) 불리 → 패배 (CMB-054)
            BattleOutcome outcome = allyHp > enemyHp ? BattleOutcome.Victory : BattleOutcome.Defeat;
            return new BattleResult(outcome, state.ActionCount, state.Tick);
        }

        private static bool AnyAlive(BattleState state, Side side)
        {
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.Side == side && unit.IsAlive())
                {
                    return true;
                }
            }

            return false;
        }

        // 아군이 한 명이라도 죽었는지(무손실 사수 판정). 전투 시작 시 전원 생존 전제. (CONT-003)
        private static bool AnyAllyDead(BattleState state)
        {
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.Side == Side.Ally && unit.IsAlive() == false)
                {
                    return true;
                }
            }

            return false;
        }

        // 호위 대상(지휘관)이 죽었는지(Escort 패배 판정). 대상 정의는 ObjectiveResolver.EscortLeader 와 공유(SSOT). (CONT-003)
        private static bool EscortLeaderDead(BattleState state)
        {
            Unit leader = ObjectiveResolver.EscortLeader(state);
            return leader != null && leader.IsAlive() == false;
        }

        private static int SumHp(BattleState state, Side side)
        {
            int total = 0;
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.Side == side && unit.IsAlive())
                {
                    total += unit.Hp;
                }
            }

            return total;
        }
    }
}
