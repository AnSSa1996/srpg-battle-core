using SRPG.Battle.Core.Data;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 지원(힐/버프/실드) 행동 1건의 계획. 사용할 스킬·1차 대상 아군·평가 점수를 묶는다.
    /// 점수는 공격 후보(ScoredCandidate)와 같은 척도라 직접 비교할 수 있다. (참조: AI-120)
    /// </summary>
    public sealed class SupportPlan
    {
        /// <summary>사용할 지원 스킬.</summary>
        public SkillDef Skill { get; }

        /// <summary>1차 대상 아군(Self 스킬이면 시전자).</summary>
        public Unit Target { get; }

        /// <summary>유틸리티 점수(공격 후보와 동일 척도).</summary>
        public int Score { get; }

        /// <summary>지원 계획을 만든다.</summary>
        public SupportPlan(SkillDef skill, Unit target, int score)
        {
            Skill = skill;
            Target = target;
            Score = score;
        }
    }
}
