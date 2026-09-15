using System.Collections.Generic;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Events;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Rng;

namespace SRPG.Battle.Core.Model
{
    /// <summary>
    /// 전투의 단일 상태 소스. 틱·난수·유닛·맵을 보유하며, 모든 변경은 커맨드(M4)를 통해서만 일어난다.
    /// 외부 상태에 의존하지 않는 순수 C# (결정론, CMB-040~046).
    /// (참조: Docs/01_전투/04_전투_시스템.md 3-3)
    /// </summary>
    public sealed class BattleState
    {
        private readonly DetRng _rng;
        private readonly List<Unit> _units;
        private readonly BattleMap _map;
        private readonly Queue<IReadOnlyList<Unit>> _pendingWaves;

        private int _tick = 0;
        private int _actionCount = 0;
        private int _seq = 0;
        private int _actionId = 0;
        private int _captureHold = 0;
        private int _waveIndex = 0;
        private IEventSink _sink = NullEventSink.Instance;

        /// <summary>현재 틱(유일한 시간 기준).</summary>
        public int Tick { get { return _tick; } }

        /// <summary>누적 행동 수(타임아웃 판정용).</summary>
        public int ActionCount { get { return _actionCount; } }

        /// <summary>거점을 연속으로 점령(아군 통제)한 행동 수. 통제를 잃으면 0 으로 리셋된다(Capture 목표, CMB-512).</summary>
        public int CaptureHoldTurns { get { return _captureHold; } }

        /// <summary>소환을 마친 웨이브 수(0 = 초기 배치만, 증원 1회당 +1). 웨이브 목표/연출용. (CMB-512 웨이브)</summary>
        public int WaveIndex { get { return _waveIndex; } }

        /// <summary>아직 소환되지 않은 증원 웨이브가 남아 있는지. 현재 적 전멸이라도 true 면 승리 보류·다음 웨이브 소환. (CMB-512 웨이브)</summary>
        public bool HasPendingWaves { get { return _pendingWaves.Count > 0; } }

        /// <summary>전투 전용 결정론 난수 생성기.</summary>
        public DetRng Rng { get { return _rng; } }

        /// <summary>전체 유닛(슬롯 인덱스 오름차순).</summary>
        public IReadOnlyList<Unit> Units { get { return _units; } }

        /// <summary>전투 맵.</summary>
        public BattleMap Map { get { return _map; } }

        /// <summary>이벤트 출구(기본 무동작). 헤드리스/뷰가 교체한다.</summary>
        public IEventSink Sink { get { return _sink; } }

        /// <summary>현재 행동 묶음 식별자(actionId).</summary>
        public int ActionId { get { return _actionId; } }

        /// <summary>전투 상태를 만든다. 유닛은 슬롯 인덱스 오름차순으로 정렬해 보관한다.
        /// pendingWaves 가 있으면 현재 적 전멸 시 차례로 소환할 증원 웨이브(각 웨이브 = 유닛 묶음, 순서 보존). (CMB-512 웨이브)</summary>
        public BattleState(BattleMap map, IReadOnlyList<Unit> units, DetRng rng,
            IReadOnlyList<IReadOnlyList<Unit>> pendingWaves = null)
        {
            _map = map;
            _rng = rng;
            _units = new List<Unit>(units);
            _units.Sort(CompareBySlot);

            _pendingWaves = new Queue<IReadOnlyList<Unit>>();
            if (pendingWaves != null)
            {
                for (int index = 0; index < pendingWaves.Count; index++)
                {
                    _pendingWaves.Enqueue(pendingWaves[index]);
                }
            }
        }

        /// <summary>틱을 지정 수만큼 전진한다(틱 일괄 전진, CMB-014).</summary>
        public void AdvanceTick(int ticks)
        {
            _tick += ticks;
        }

        /// <summary>누적 행동 수를 1 늘린다.</summary>
        public void IncrementActionCount()
        {
            _actionCount += 1;
        }

        /// <summary>거점 연속 점령 수를 1 늘린다(이번 행동에서 아군이 거점을 통제, Capture).</summary>
        public void IncrementCaptureHold()
        {
            _captureHold += 1;
        }

        /// <summary>거점 연속 점령 수를 0 으로 되돌린다(통제 상실 — 적이 거점에 들어왔거나 아군이 비웠을 때, Capture).</summary>
        public void ResetCaptureHold()
        {
            _captureHold = 0;
        }

        /// <summary>다음 증원 웨이브를 전장에 소환한다(유닛을 활성 목록에 추가, 슬롯 순 재정렬). WaveIndex 를 1 늘리고 등장 유닛을 반환한다.
        /// 남은 웨이브가 없으면 빈 목록을 반환한다(호출 전 HasPendingWaves 확인 권장). (CMB-512 웨이브)</summary>
        public IReadOnlyList<Unit> SpawnNextWave()
        {
            if (_pendingWaves.Count == 0)
            {
                return new List<Unit>();
            }

            IReadOnlyList<Unit> wave = _pendingWaves.Dequeue();
            for (int index = 0; index < wave.Count; index++)
            {
                _units.Add(wave[index]);
            }

            _units.Sort(CompareBySlot);
            _waveIndex += 1;
            return wave;
        }

        /// <summary>이벤트 출구를 설정한다(null 이면 무동작 싱크).</summary>
        public void SetSink(IEventSink sink)
        {
            _sink = sink ?? NullEventSink.Instance;
        }

        /// <summary>다음 이벤트 전역 순번을 발급한다.</summary>
        public int NextSeq()
        {
            int seq = _seq;
            _seq += 1;
            return seq;
        }

        /// <summary>새 행동 묶음을 시작한다(actionId 증가).</summary>
        public void BeginAction()
        {
            _actionId += 1;
        }

        /// <summary>슬롯 인덱스로 유닛을 찾는다(없으면 null). 1-ply 클론에서 대응 유닛 조회용.</summary>
        public Unit UnitBySlot(int slot)
        {
            for (int index = 0; index < _units.Count; index++)
            {
                if (_units[index].Slot == slot)
                {
                    return _units[index];
                }
            }

            return null;
        }

        /// <summary>전방 시뮬용 결정론 클론을 만든다. 유닛은 깊은 복사, 맵은 불변이라 공유. (AI-140, E-3)</summary>
        public BattleState DeterministicClone()
        {
            List<Unit> clonedUnits = new List<Unit>(_units.Count);
            for (int index = 0; index < _units.Count; index++)
            {
                clonedUnits.Add(_units[index].Clone());
            }

            BattleState clone = new BattleState(_map, clonedUnits, _rng.Clone());
            clone._tick = _tick;
            clone._actionCount = _actionCount;
            clone._captureHold = _captureHold;
            return clone;
        }

        /// <summary>해당 타일을 점유한 살아있는 유닛을 반환한다(슬롯 순서, 없으면 null).</summary>
        public Unit UnitAt(Coord tile)
        {
            for (int index = 0; index < _units.Count; index++)
            {
                Unit unit = _units[index];
                if (unit.IsAlive() && unit.Pos == tile)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>이동 주체 기준 도달 질의를 만든다(ReachCalculator 입력).</summary>
        public IReachQuery CreateReachQuery(Unit mover)
        {
            return new BattleReachQuery(this, mover);
        }

        private static int CompareBySlot(Unit a, Unit b)
        {
            return a.Slot.CompareTo(b.Slot);
        }
    }
}
