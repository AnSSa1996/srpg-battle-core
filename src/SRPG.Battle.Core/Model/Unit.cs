using System.Collections.Generic;
using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Stats;

namespace SRPG.Battle.Core.Model
{
    /// <summary>
    /// 전투 그리드 위에서 행동하는 개체(아군 영웅/적)의 런타임 상태.
    /// 모든 변경은 커맨드(M4)·시스템(M5)을 통해 이 메서드들로만 이뤄진다(단일 상태 소스 원칙, CMB-042/043).
    /// (참조: Docs/01_전투/04_전투_시스템.md)
    /// </summary>
    public sealed class Unit
    {
        private const int MIN_ALIVE_HP = 0;

        private readonly List<StatusEffect> _statuses = new List<StatusEffect>();
        private readonly Dictionary<string, int> _cooldowns = new Dictionary<string, int>();

        private SkillDef _activeSkill;
        private SkillDef _ultSkill;

        private Coord _pos;
        private Facing _facing;
        private int _ct = 0;
        private int _hp;
        private int _shield = 0;
        private int _ultGauge = 0;
        private StatBlock _fieldStatPercent = StatBlock.Zero();

        /// <summary>슬롯 인덱스. 결정론 정렬·타이브레이크의 기준 키. (CMB-015)</summary>
        public int Slot { get; }

        /// <summary>소속 편.</summary>
        public Side Side { get; }

        /// <summary>클래스(MOV/RNG·필드필터·AI 성향 기준).</summary>
        public HeroClass Class { get; }

        /// <summary>영웅 ID(뷰 스프라이트·애니메이터 선택용. 0이면 클래스 폴백).</summary>
        public int HeroId { get; }

        /// <summary>배치 시 지정한 AI 스탠스(고려요소 가중치 프로파일).</summary>
        public Stance Stance { get; }

        /// <summary>보스 유닛 여부(KillBoss 승리목표 판정 기준, CMB-512). 기본 false.</summary>
        public bool IsBoss { get; }

        /// <summary>전투 진입 스탯(전투 시작 시 1회 산출, 기준값).</summary>
        public StatBlock EntryStat { get; }

        /// <summary>필드효과 StatMod 누적치(‰, 전투 시작 시 1회 설정). EffectiveStat 이 상태이상 StatMod 와 함께 합산. (CMB-520)</summary>
        public StatBlock FieldStatPercent { get { return _fieldStatPercent; } }

        /// <summary>현재 좌표.</summary>
        public Coord Pos { get { return _pos; } }

        /// <summary>바라보는 방향.</summary>
        public Facing Facing { get { return _facing; } }

        /// <summary>행동 게이지(CT).</summary>
        public int Ct { get { return _ct; } }

        /// <summary>현재 HP.</summary>
        public int Hp { get { return _hp; } }

        /// <summary>현재 보호막(실드).</summary>
        public int Shield { get { return _shield; } }

        /// <summary>궁극 게이지(0~100).</summary>
        public int UltGauge { get { return _ultGauge; } }

        /// <summary>걸려 있는 상태 효과 목록(부여 순서 보존).</summary>
        public IReadOnlyList<StatusEffect> Statuses { get { return _statuses; } }

        /// <summary>액티브 스킬(없으면 null).</summary>
        public SkillDef ActiveSkill { get { return _activeSkill; } }

        /// <summary>궁극 스킬(없으면 null).</summary>
        public SkillDef UltSkill { get { return _ultSkill; } }

        /// <summary>유닛을 만든다. 시작 HP 는 EntryStat 의 HP 로 별도 지정한다. 스탠스 기본값은 공격, 보스 아님.</summary>
        public Unit(int slot, Side side, HeroClass heroClass, StatBlock entryStat,
            Coord pos, Facing facing, int startHp, Stance stance = Stance.Attack, bool isBoss = false, int heroId = 0)
        {
            Slot = slot;
            Side = side;
            Class = heroClass;
            EntryStat = entryStat;
            _pos = pos;
            _facing = facing;
            _hp = startHp;
            Stance = stance;
            IsBoss = isBoss;
            HeroId = heroId;
        }

        /// <summary>HP 가 0 보다 크면 살아있다.</summary>
        public bool IsAlive()
        {
            return _hp > MIN_ALIVE_HP;
        }

        /// <summary>좌표를 옮긴다.</summary>
        public void MoveTo(Coord pos)
        {
            _pos = pos;
        }

        /// <summary>방향을 돌린다.</summary>
        public void TurnTo(Facing facing)
        {
            _facing = facing;
        }

        /// <summary>행동 게이지를 더한다(틱 충전).</summary>
        public void AddCt(int amount)
        {
            _ct += amount;
        }

        /// <summary>행동 게이지를 설정한다(이월·스킬 조작용).</summary>
        public void SetCt(int value)
        {
            _ct = value;
        }

        /// <summary>HP 를 설정한다(피해/회복 결과 반영). 0 미만은 0 으로 바닥 처리(오버킬 음수 방지).</summary>
        public void SetHp(int value)
        {
            _hp = value < 0 ? 0 : value;
        }

        /// <summary>보호막을 설정한다.</summary>
        public void SetShield(int value)
        {
            _shield = value;
        }

        /// <summary>피해를 적용한다: 보호막을 먼저 차감한 뒤 남은 피해를 HP 에 적용한다. (CMB-108)</summary>
        public void ApplyDamageWithShield(int damage)
        {
            int remaining = damage;
            if (_shield > 0)
            {
                int absorbed = _shield < remaining ? _shield : remaining;
                _shield -= absorbed;
                remaining -= absorbed;
            }

            if (remaining > 0)
            {
                _hp -= remaining;
                if (_hp < 0)
                {
                    _hp = 0; // 오버킬 음수 방지(IsAlive 는 >0 이라 사망 판정 동일)
                }
            }
        }

        /// <summary>궁극 게이지를 설정한다.</summary>
        public void SetUltGauge(int value)
        {
            _ultGauge = value;
        }

        /// <summary>필드효과 StatMod 누적치를 설정한다(전투 시작 시 1회, BattleSimulator 전용). (CMB-520)</summary>
        public void SetFieldStatPercent(StatBlock percentPerMille)
        {
            _fieldStatPercent = percentPerMille ?? StatBlock.Zero();
        }

        /// <summary>궁극 게이지를 가감한다(0~최대치로 clamp). (CMB-301/302)</summary>
        public void AddUltGauge(int amount)
        {
            int next = _ultGauge + amount;
            if (next < 0)
            {
                next = 0;
            }

            if (next > CombatConst.ULT_GAUGE_MAX)
            {
                next = CombatConst.ULT_GAUGE_MAX;
            }

            _ultGauge = next;
        }

        /// <summary>궁극이 발동 가능한지(게이지 최대치 도달 + 궁극 스킬 보유). (CMB-303)</summary>
        public bool IsUltReady()
        {
            return _ultGauge >= CombatConst.ULT_GAUGE_MAX && _ultSkill != null;
        }

        /// <summary>액티브·궁극 스킬을 설정한다(평타는 AttackCommand 가 담당).</summary>
        public void SetSkills(SkillDef activeSkill, SkillDef ultSkill)
        {
            _activeSkill = activeSkill;
            _ultSkill = ultSkill;
        }

        /// <summary>스킬 남은 쿨다운(자기 턴 수). 없으면 0. (CMB-304)</summary>
        public int GetCooldown(string skillId)
        {
            if (skillId == null)
            {
                return 0;
            }

            return _cooldowns.TryGetValue(skillId, out int remaining) ? remaining : 0;
        }

        /// <summary>스킬을 쿨다운에 올린다(사용 직후).</summary>
        public void PutOnCooldown(string skillId, int cooldownTurns)
        {
            if (skillId == null || cooldownTurns <= 0)
            {
                return;
            }

            _cooldowns[skillId] = cooldownTurns;
        }

        /// <summary>모든 쿨다운을 1 줄인다(자기 턴 시작). 감산은 순서 무관이라 결정론적. (CMB-304)</summary>
        public void TickCooldowns()
        {
            if (_cooldowns.Count == 0)
            {
                return;
            }

            List<string> keys = new List<string>(_cooldowns.Keys);
            for (int index = 0; index < keys.Count; index++)
            {
                string key = keys[index];
                int next = _cooldowns[key] - 1;
                if (next <= 0)
                {
                    _cooldowns.Remove(key);
                }
                else
                {
                    _cooldowns[key] = next;
                }
            }
        }

        /// <summary>상태 효과를 추가한다.</summary>
        public void AddStatus(StatusEffect status)
        {
            if (status == null)
            {
                return;
            }

            _statuses.Add(status);
        }

        /// <summary>지정 상태 효과를 제거한다.</summary>
        public void RemoveStatus(StatusEffect status)
        {
            _statuses.Remove(status);
        }

        /// <summary>현재 상태를 그대로 복제한다(1-ply 결정론 클론용). EntryStat 은 불변이라 공유한다.</summary>
        public Unit Clone()
        {
            Unit copy = new Unit(Slot, Side, Class, EntryStat, _pos, _facing, _hp, Stance, IsBoss);
            copy._ct = _ct;
            copy._shield = _shield;
            copy._ultGauge = _ultGauge;
            copy._activeSkill = _activeSkill; // SkillDef 는 불변 → 공유
            copy._ultSkill = _ultSkill;
            copy._fieldStatPercent = _fieldStatPercent; // StatBlock 불변 → 공유
            for (int index = 0; index < _statuses.Count; index++)
            {
                copy._statuses.Add(_statuses[index].Clone());
            }

            foreach (KeyValuePair<string, int> entry in _cooldowns)
            {
                copy._cooldowns[entry.Key] = entry.Value;
            }

            return copy;
        }
    }
}
