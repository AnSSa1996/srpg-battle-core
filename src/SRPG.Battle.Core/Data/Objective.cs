using System.Collections.Generic;
using SRPG.Battle.Core.Grid;

namespace SRPG.Battle.Core.Data
{
    /// <summary>
    /// 승리 목표(불변 입력 데이터). 타입에 따라 쓰는 필드가 다르다.
    /// KillBoss→TargetId, Capture→Tiles/Amount(holdTurns), Survive→Amount(turns), Escort→TargetId.
    /// (참조: Docs/01_전투/23_전투맵_데이터.md CMB-512)
    /// </summary>
    public sealed class Objective
    {
        /// <summary>목표 타입.</summary>
        public ObjectiveType Type { get; }

        /// <summary>대상 식별자(보스/호위 대상).</summary>
        public string TargetId { get; }

        /// <summary>거점 타일 목록(Capture 용).</summary>
        public IReadOnlyList<Coord> Tiles { get; }

        /// <summary>수치(Capture=holdTurns, Survive=turns).</summary>
        public int Amount { get; }

        /// <summary>턴 제한 도달 시 목표 미충족이면 패배(true) / HP 비교(false). 속전·DPS 던전용. (CONT-003)</summary>
        public bool FailOnTimeout { get; }

        /// <summary>아군이 한 명이라도 죽으면 즉시 패배(무손실 사수 던전). (CONT-003)</summary>
        public bool LoseOnAllyDeath { get; }

        /// <summary>승리 목표를 만든다.</summary>
        public Objective(ObjectiveType type, string targetId, IReadOnlyList<Coord> tiles, int amount, bool failOnTimeout = false, bool loseOnAllyDeath = false)
        {
            Type = type;
            TargetId = targetId;
            Tiles = tiles ?? new List<Coord>();
            Amount = amount;
            FailOnTimeout = failOnTimeout;
            LoseOnAllyDeath = loseOnAllyDeath;
        }
    }
}
