namespace SRPG.Battle.Core.Data
{
    /// <summary>
    /// 스킬 효과 한 항목. effects[] 는 이 항목들을 배열 순서대로 적용한다. 타입별로 쓰는 필드가 다르다.
    /// 팩토리(Damage/Heal/Status/Shield/GaugeMod)로 생성한다. (참조: Docs/01_전투/17_스킬_시스템.md CMB-308)
    /// ApplyStatus 는 상태 정의(StatusDef, status 테이블에서 해석)를 참조로 들고 적용한다.
    /// </summary>
    public sealed class SkillEffect
    {
        /// <summary>효과 종류.</summary>
        public EffectType Type { get; }

        /// <summary>피해/회복 배율(‰). Damage=skillPower, Heal=healPower.</summary>
        public int Power { get; }

        /// <summary>물리/마법 구분(Damage 용).</summary>
        public bool IsPhysical { get; }

        /// <summary>실드/게이지 효과량(Shield/GaugeMod 용).</summary>
        public int Value { get; }

        /// <summary>부여할 상태 정의(ApplyStatus 용, 그 외 null). 효과량·지속·중첩·대상스탯을 모두 들고 있다.</summary>
        public StatusDef AppliedStatus { get; }

        /// <summary>강제 이동 종류(Move 용, 그 외 None). 거리는 Value(타일). (CMB-308 Move)</summary>
        public MoveKind MoveKind { get; }

        private SkillEffect(EffectType type, int power, bool isPhysical, int value, StatusDef appliedStatus,
            MoveKind moveKind = MoveKind.None)
        {
            Type = type;
            Power = power;
            IsPhysical = isPhysical;
            Value = value;
            AppliedStatus = appliedStatus;
            MoveKind = moveKind;
        }

        /// <summary>피해 효과. (CMB-102, 15)</summary>
        public static SkillEffect Damage(int skillPowerPerMille, bool isPhysical)
        {
            return new SkillEffect(EffectType.Damage, skillPowerPerMille, isPhysical, 0, null);
        }

        /// <summary>회복 효과. (CMB-106)</summary>
        public static SkillEffect Heal(int healPowerPerMille)
        {
            return new SkillEffect(EffectType.Heal, healPowerPerMille, false, 0, null);
        }

        /// <summary>상태 부여 효과. status 정의를 참조한다. (CMB-308 ApplyStatus, 16)</summary>
        public static SkillEffect Status(StatusDef appliedStatus)
        {
            return new SkillEffect(EffectType.ApplyStatus, 0, false, 0, appliedStatus);
        }

        /// <summary>실드 부여 효과. (CMB-108)</summary>
        public static SkillEffect Shield(int value)
        {
            return new SkillEffect(EffectType.Shield, 0, false, value, null);
        }

        /// <summary>궁극 게이지 가감 효과.</summary>
        public static SkillEffect GaugeMod(int value)
        {
            return new SkillEffect(EffectType.GaugeMod, 0, false, value, null);
        }

        /// <summary>강제 이동 효과(밀치기/끌기). distance=이동 타일 수. (CMB-308 Move)</summary>
        public static SkillEffect Move(MoveKind moveKind, int distance)
        {
            return new SkillEffect(EffectType.Move, 0, false, distance, null, moveKind);
        }
    }
}
