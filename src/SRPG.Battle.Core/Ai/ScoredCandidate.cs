namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// 점수가 매겨진 공격/이동 후보. 지원 행동(SupportPlan)과 같은 척도로 비교하기 위해
    /// 채택 후보와 그 최종 점수(1-ply 가산 포함)를 함께 들고 다닌다. (참조: AI-120/140)
    /// </summary>
    public sealed class ScoredCandidate
    {
        /// <summary>채택된 후보.</summary>
        public Candidate Candidate { get; }

        /// <summary>후보의 최종 유틸리티 점수.</summary>
        public int Score { get; }

        /// <summary>점수 매긴 후보를 만든다.</summary>
        public ScoredCandidate(Candidate candidate, int score)
        {
            Candidate = candidate;
            Score = score;
        }
    }
}
