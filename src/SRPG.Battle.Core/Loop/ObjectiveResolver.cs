using System.Collections.Generic;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Loop
{
    /// <summary>
    /// 승리 목표(Objective) 충족 판정. 매 행동 직후 BattleSimulator 가 호출한다(CMB-053 ①).
    /// 적 전멸(보드 정리)은 어떤 목표에서도 항상 승리로 본다. 그 외 타입별 조건:
    /// KillBoss=보스 처치 / Survive=지정 행동 수 생존 / Capture=거점을 holdTurns 행동 연속 점령 /
    /// Escort=호위 대상이 추출 지점(Tiles)에 도달(Tiles 미지정이면 전멸만). (23_전투맵_데이터.md CMB-512)
    /// </summary>
    public static class ObjectiveResolver
    {
        /// <summary>현재 상태가 승리 목표를 충족했는지 판정한다.</summary>
        public static bool IsMet(BattleState state, Objective objective)
        {
            if (state == null)
            {
                return false;
            }

            if (AnyEnemyAlive(state) == false)
            {
                return true; // 적 전멸 = 보드 정리, 목표 무관하게 항상 승리
            }

            if (objective == null)
            {
                return false;
            }

            switch (objective.Type)
            {
                case ObjectiveType.KillBoss:
                    return BossDefeated(state);
                case ObjectiveType.Survive:
                    return SurviveReached(state, objective.Amount);
                case ObjectiveType.Capture:
                    return CaptureHeld(state, objective.Amount);
                case ObjectiveType.Escort:
                    return EscortExtracted(state, objective);
                default:
                    return false; // Annihilate(위 전멸 검사로 처리)
            }
        }

        // 거점 점령: holdTurns 행동 이상 연속으로 아군이 거점을 통제하면 충족(연속 유지 수는 BattleSimulator 가 누적).
        // holdTurns(Amount) 가 0 이하인 데이터 오류면 점령으로는 영영 미충족(전멸로만 종료, Survive 와 동일 방어).
        private static bool CaptureHeld(BattleState state, int holdTurns)
        {
            return holdTurns > 0 && state.CaptureHoldTurns >= holdTurns;
        }

        // 호위: 추출 지점(Objective.Tiles)이 지정돼 있고 호위 대상(리더)이 살아서 그 위에 있으면 승리.
        // Tiles 미지정이면 추출 승리 없음 → 전멸로만 승리(기존 동작 보존). 리더 사망 패배는 BattleSimulator.CheckEnd 가 처리.
        private static bool EscortExtracted(BattleState state, Objective objective)
        {
            if (objective.Tiles.Count == 0)
            {
                return false;
            }

            Unit leader = EscortLeader(state);
            return leader != null && leader.IsAlive() && TilesContain(objective.Tiles, leader.Pos);
        }

        /// <summary>이번 행동 직후 아군이 거점(Objective.Tiles)을 통제하는지 — 아군이 한 명 이상 거점 위에 있고, 적은 거점에 없다.
        /// 적이 한 칸이라도 점유하면 경합(미통제)으로 본다. BattleSimulator 가 매 행동 후 카운터 증감에 사용한다.</summary>
        public static bool AllyControlsCapturePoint(BattleState state, Objective objective)
        {
            if (state == null || objective == null || objective.Type != ObjectiveType.Capture || objective.Tiles.Count == 0)
            {
                return false;
            }

            bool allyPresent = false;
            for (int index = 0; index < objective.Tiles.Count; index++)
            {
                Unit occupant = state.UnitAt(objective.Tiles[index]);
                if (occupant == null)
                {
                    continue;
                }

                if (occupant.Side == Side.Enemy)
                {
                    return false; // 적이 거점을 밟고 있으면 경합 → 미통제
                }

                if (occupant.Side == Side.Ally)
                {
                    allyPresent = true;
                }
            }

            return allyPresent;
        }

        /// <summary>호위 대상(지휘관) — 슬롯이 가장 작은 아군. 없으면 null. (Escort 승리/패배 판정 공용 SSOT)</summary>
        public static Unit EscortLeader(BattleState state)
        {
            Unit leader = null;
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.Side != Side.Ally)
                {
                    continue;
                }

                if (leader == null || unit.Slot < leader.Slot)
                {
                    leader = unit;
                }
            }

            return leader;
        }

        private static readonly IReadOnlyList<Coord> NoTiles = new List<Coord>();

        /// <summary>이 행동자가 끌려야 할 목표 타일(없으면 빈 목록). Capture=모든 아군이 거점, Escort=리더가 추출 지점.
        /// AI 의 후보 생성(CandidateGenerator)과 점수(UtilityScorer)가 공유하는 SSOT — 둘이 어긋나면 후보는 있는데 점수가 0(또는 반대)이 된다.</summary>
        public static IReadOnlyList<Coord> PullTilesFor(BattleState state, Unit actor)
        {
            Objective objective = state.Map.Objective;
            if (objective == null || objective.Tiles.Count == 0 || actor.Side != Side.Ally)
            {
                return NoTiles;
            }

            if (objective.Type == ObjectiveType.Capture)
            {
                return objective.Tiles;
            }

            if (objective.Type == ObjectiveType.Escort && actor == EscortLeader(state))
            {
                return objective.Tiles;
            }

            return NoTiles;
        }

        private static bool TilesContain(IReadOnlyList<Coord> tiles, Coord pos)
        {
            for (int index = 0; index < tiles.Count; index++)
            {
                if (tiles[index] == pos)
                {
                    return true;
                }
            }

            return false;
        }

        // 생존: 누적 행동 수가 목표치 이상이면 충족(아군 생존은 호출부 CheckEnd 가 별도 보장).
        private static bool SurviveReached(BattleState state, int requiredActions)
        {
            return requiredActions > 0 && state.ActionCount >= requiredActions;
        }

        // 보스 처치: 보스로 지정된 적이 하나 이상 있고, 그중 살아있는 보스가 없다.
        // (보스 미지정 스테이지에서 KillBoss 면 영영 미충족 → 전멸/타임아웃으로만 종료. 데이터 오류 방어)
        private static bool BossDefeated(BattleState state)
        {
            bool bossExists = false;
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.Side != Side.Enemy || unit.IsBoss == false)
                {
                    continue;
                }

                bossExists = true;
                if (unit.IsAlive())
                {
                    return false; // 살아있는 보스가 남아 있으면 미충족
                }
            }

            return bossExists;
        }

        private static bool AnyEnemyAlive(BattleState state)
        {
            IReadOnlyList<Unit> units = state.Units;
            for (int index = 0; index < units.Count; index++)
            {
                Unit unit = units[index];
                if (unit.Side == Side.Enemy && unit.IsAlive())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
