using System.Collections.Generic;
using SRPG.Battle.Core.Grid;

namespace SRPG.Battle.Core.Data
{
    /// <summary>
    /// 스킬 정의(불변 입력 데이터). AI 후보 생성·데미지·상태이상이 이 스키마를 참조한다.
    /// (참조: Docs/01_전투/17_스킬_시스템.md CMB-300~308)
    /// </summary>
    public sealed class SkillDef
    {
        /// <summary>스킬 식별자.</summary>
        public string Id { get; }

        /// <summary>스킬 타입(평타/액티브/궁극/패시브).</summary>
        public SkillType Type { get; }

        /// <summary>타게팅 타입.</summary>
        public TargetingType Targeting { get; }

        /// <summary>사거리(타일, 맨해튼).</summary>
        public int Range { get; }

        /// <summary>적중 범위 패턴.</summary>
        public AoePattern Aoe { get; }

        /// <summary>AoE 크기(LineN 칸수 / SelfAroundR 반경). 그 외 패턴은 무시.</summary>
        public int AoeSize { get; }

        /// <summary>공격형(피해 동반=명중 판정) 여부. false 면 비공격형. (CMB-111/112)</summary>
        public bool IsOffensive { get; }

        /// <summary>AoE 가 아군에도 적중(오사)하는지. (CMB-441)</summary>
        public bool HitsAlly { get; }

        /// <summary>쿨다운(자기 턴 수). 0 이면 매 턴 사용 가능.</summary>
        public int Cooldown { get; }

        /// <summary>효과 배열(순서대로 적용).</summary>
        public IReadOnlyList<SkillEffect> Effects { get; }

        /// <summary>스킬 정의를 만든다.</summary>
        public SkillDef(string id, SkillType type, TargetingType targeting, int range, AoePattern aoe,
            int aoeSize, bool isOffensive, bool hitsAlly, int cooldown, IReadOnlyList<SkillEffect> effects)
        {
            Id = id;
            Type = type;
            Targeting = targeting;
            Range = range;
            Aoe = aoe;
            AoeSize = aoeSize;
            IsOffensive = isOffensive;
            HitsAlly = hitsAlly;
            Cooldown = cooldown;
            Effects = effects ?? new List<SkillEffect>();
        }
    }
}
