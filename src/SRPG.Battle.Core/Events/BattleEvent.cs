using SRPG.Battle.Core.Grid;

namespace SRPG.Battle.Core.Events
{
    /// <summary>이벤트 종류. (참조: Docs/01_전투/26_전투_연출_뷰.md VIEW-010, 표준 이벤트 목록의 부분집합)</summary>
    public enum BattleEventType
    {
        TurnStarted = 0,
        Moved = 1,
        AttackHit = 2,
        AttackMissed = 3,
        Died = 4,
        BattleEnded = 5,
        SkillCast = 6,
        Healed = 7,
        ShieldGained = 8,
        BuffApplied = 9,
        DebuffApplied = 10,
        StatusDamage = 11,
        WaveSpawned = 12,
        CaptureProgress = 13
    }

    /// <summary>
    /// 시뮬이 방출하는 불변 사실 이벤트. 결정론적 순서(seq)와 행동 묶음(actionId)을 가진다.
    /// 뷰는 이 값 객체만 소비하며, 결과값(amount, hpAfter)을 재계산하지 않는다.
    /// (참조: VIEW-010~012)
    /// </summary>
    public sealed class BattleEvent
    {
        private const int NO_SLOT = -1;

        /// <summary>전역 순번(시뮬 처리 순서).</summary>
        public int Seq { get; }

        /// <summary>행동 묶음 식별자(한 유닛 행동의 이동+공격+사망 등).</summary>
        public int ActionId { get; }

        /// <summary>이벤트 종류.</summary>
        public BattleEventType Type { get; }

        /// <summary>주체 유닛 슬롯.</summary>
        public int Slot { get; }

        /// <summary>대상 유닛 슬롯(없으면 -1).</summary>
        public int TargetSlot { get; }

        /// <summary>이동 목적지 x.</summary>
        public int ToX { get; }

        /// <summary>이동 목적지 y.</summary>
        public int ToY { get; }

        /// <summary>피해/회복/CT 등 수치값.</summary>
        public int Amount { get; }

        /// <summary>치명 여부.</summary>
        public bool IsCrit { get; }

        /// <summary>피격 면.</summary>
        public FaceType Face { get; }

        /// <summary>대상 처리 후 HP.</summary>
        public int HpAfter { get; }

        /// <summary>BattleEnded 일 때 승리 여부.</summary>
        public bool Victory { get; }

        private BattleEvent(int seq, int actionId, BattleEventType type, int slot, int targetSlot,
            int toX, int toY, int amount, bool isCrit, FaceType face, int hpAfter, bool victory)
        {
            Seq = seq;
            ActionId = actionId;
            Type = type;
            Slot = slot;
            TargetSlot = targetSlot;
            ToX = toX;
            ToY = toY;
            Amount = amount;
            IsCrit = isCrit;
            Face = face;
            HpAfter = hpAfter;
            Victory = victory;
        }

        /// <summary>턴 시작 이벤트.</summary>
        public static BattleEvent TurnStarted(int seq, int actionId, int slot, int ct)
        {
            return new BattleEvent(seq, actionId, BattleEventType.TurnStarted, slot, NO_SLOT, 0, 0, ct, false, FaceType.Front, 0, false);
        }

        /// <summary>이동 이벤트.</summary>
        public static BattleEvent Moved(int seq, int actionId, int slot, Coord to)
        {
            return new BattleEvent(seq, actionId, BattleEventType.Moved, slot, NO_SLOT, to.X, to.Y, 0, false, FaceType.Front, 0, false);
        }

        /// <summary>명중 이벤트(피해 적용 결과 포함).</summary>
        public static BattleEvent AttackHit(int seq, int actionId, int slot, int targetSlot, int amount, bool isCrit, FaceType face, int hpAfter)
        {
            return new BattleEvent(seq, actionId, BattleEventType.AttackHit, slot, targetSlot, 0, 0, amount, isCrit, face, hpAfter, false);
        }

        /// <summary>빗나감 이벤트.</summary>
        public static BattleEvent AttackMissed(int seq, int actionId, int slot, int targetSlot)
        {
            return new BattleEvent(seq, actionId, BattleEventType.AttackMissed, slot, targetSlot, 0, 0, 0, false, FaceType.Front, 0, false);
        }

        /// <summary>사망 이벤트.</summary>
        public static BattleEvent Died(int seq, int actionId, int slot)
        {
            return new BattleEvent(seq, actionId, BattleEventType.Died, slot, NO_SLOT, 0, 0, 0, false, FaceType.Front, 0, false);
        }

        /// <summary>전투 종료 이벤트.</summary>
        public static BattleEvent BattleEnded(int seq, int actionId, bool victory)
        {
            return new BattleEvent(seq, actionId, BattleEventType.BattleEnded, NO_SLOT, NO_SLOT, 0, 0, 0, false, FaceType.Front, 0, victory);
        }

        /// <summary>스킬 시전 이벤트(액티브/궁극 사용 알림). slot = 시전자, amount = 스킬 타입((int)SkillType).</summary>
        public static BattleEvent SkillCast(int seq, int actionId, int slot, int skillKind)
        {
            return new BattleEvent(seq, actionId, BattleEventType.SkillCast, slot, NO_SLOT, 0, 0, skillKind, false, FaceType.Front, 0, false);
        }

        /// <summary>회복 이벤트. slot = 대상, amount = 실제 회복량, hpAfter = 회복 후 HP.</summary>
        public static BattleEvent Healed(int seq, int actionId, int slot, int amount, int hpAfter)
        {
            return new BattleEvent(seq, actionId, BattleEventType.Healed, slot, NO_SLOT, 0, 0, amount, false, FaceType.Front, hpAfter, false);
        }

        /// <summary>보호막 획득 이벤트. slot = 대상, amount = 추가된 실드량.</summary>
        public static BattleEvent ShieldGained(int seq, int actionId, int slot, int amount)
        {
            return new BattleEvent(seq, actionId, BattleEventType.ShieldGained, slot, NO_SLOT, 0, 0, amount, false, FaceType.Front, 0, false);
        }

        /// <summary>이로운 상태(버프) 부여 이벤트. slot = 대상.</summary>
        public static BattleEvent BuffApplied(int seq, int actionId, int slot)
        {
            return new BattleEvent(seq, actionId, BattleEventType.BuffApplied, slot, NO_SLOT, 0, 0, 0, false, FaceType.Front, 0, false);
        }

        /// <summary>해로운 상태(디버프) 부여 이벤트. slot = 대상.</summary>
        public static BattleEvent DebuffApplied(int seq, int actionId, int slot)
        {
            return new BattleEvent(seq, actionId, BattleEventType.DebuffApplied, slot, NO_SLOT, 0, 0, 0, false, FaceType.Front, 0, false);
        }

        /// <summary>지속피해(DoT 등) 틱 이벤트. slot = 대상, amount = 피해, hpAfter = 피해 후 HP.</summary>
        public static BattleEvent StatusDamage(int seq, int actionId, int slot, int amount, int hpAfter)
        {
            return new BattleEvent(seq, actionId, BattleEventType.StatusDamage, slot, NO_SLOT, 0, 0, amount, false, FaceType.Front, hpAfter, false);
        }

        /// <summary>증원 웨이브 소환 이벤트. toX = 웨이브 번호(1부터), amount = 이번에 등장한 유닛 수. 뷰가 새 유닛 비주얼 생성에 사용. (CMB-512 웨이브)</summary>
        public static BattleEvent WaveSpawned(int seq, int actionId, int waveIndex, int unitCount)
        {
            return new BattleEvent(seq, actionId, BattleEventType.WaveSpawned, NO_SLOT, NO_SLOT, waveIndex, 0, unitCount, false, FaceType.Front, 0, false);
        }

        /// <summary>거점 점령 진행 이벤트. toX = 현재 연속 점령 수, amount = 목표 holdTurns. 뷰 HUD 게이지 갱신용. (CMB-512 거점 점령)</summary>
        public static BattleEvent CaptureProgress(int seq, int actionId, int held, int required)
        {
            return new BattleEvent(seq, actionId, BattleEventType.CaptureProgress, NO_SLOT, NO_SLOT, held, 0, required, false, FaceType.Front, 0, false);
        }
    }
}
