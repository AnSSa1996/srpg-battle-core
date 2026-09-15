namespace SRPG.Battle.Core.Constants
{
    /// <summary>
    /// 유틸리티 AI 성능·정규화 수치. 기본값(프로토 1차값)은 아래 private const, 실제 값은 데이터(TbAiConst)에서
    /// App 이 전투 전 1회 Override 로 주입한다. 헤드리스/테스트는 주입 없이 기본값을 쓴다(기본값 = CSV 값).
    /// (참조: Docs/01_전투/25_AI_스코어링.md AI-150, ai_const)
    /// </summary>
    public static class AiConst
    {
        private const int DEFAULT_CANDIDATE_CAP = 40;
        private const int DEFAULT_ENEMY_SHORTLIST = 3;
        private const int DEFAULT_FLANK_BACK_NORM = 1000;
        private const int DEFAULT_FLANK_SIDE_NORM = 600;
        private const int DEFAULT_FLANK_FRONT_NORM = 0;
        private const int DEFAULT_LOOKAHEAD_K = 8;
        private const int DEFAULT_LOOKAHEAD_WEIGHT = 1000;
        private const int DEFAULT_THREAT_DIRECTION_AVG_PER_MILLE = 500;
        private const int DEFAULT_SUPPORT_BUFF_NORM = 600;
        private const int DEFAULT_OBJECTIVE_PULL_WEIGHT = 3000;

        /// <summary>후보 하드 캡(N). (AI-150)</summary>
        public static int CandidateCap { get; private set; } = DEFAULT_CANDIDATE_CAP;

        /// <summary>대상 후보로 추릴 최근접 적 수. (AI-020)</summary>
        public static int EnemyShortlist { get; private set; } = DEFAULT_ENEMY_SHORTLIST;

        /// <summary>면(Flank) 정규화: 후면. (AI-110)</summary>
        public static int FlankBackNorm { get; private set; } = DEFAULT_FLANK_BACK_NORM;

        /// <summary>면(Flank) 정규화: 측면. (AI-110)</summary>
        public static int FlankSideNorm { get; private set; } = DEFAULT_FLANK_SIDE_NORM;

        /// <summary>면(Flank) 정규화: 정면. (AI-110)</summary>
        public static int FlankFrontNorm { get; private set; } = DEFAULT_FLANK_FRONT_NORM;

        /// <summary>1-ply 전방 시뮬을 적용할 상위 후보 수(K). (AI-150)</summary>
        public static int LookaheadK { get; private set; } = DEFAULT_LOOKAHEAD_K;

        /// <summary>1-ply 보드 평가 가중치(‰). (AI-140)</summary>
        public static int LookaheadWeight { get; private set; } = DEFAULT_LOOKAHEAD_WEIGHT;

        /// <summary>위협맵 ExpectedHit 의 방향 평균 가중(‰). 기획서 0.5 = 500. (AI-160/164)</summary>
        public static int ThreatDirectionAvgPerMille { get; private set; } = DEFAULT_THREAT_DIRECTION_AVG_PER_MILLE;

        /// <summary>지원 AI: 아군 버프(ApplyStatus) 1건의 고정 가치 정규화(‰). (AI-120)</summary>
        public static int SupportBuffNorm { get; private set; } = DEFAULT_SUPPORT_BUFF_NORM;

        /// <summary>목표 추구(거점 점령·호위 추출) 타일 근접도 가중치(‰). 거점 위가 인접 칸보다 우세하도록 강하게. (CMB-512)</summary>
        public static int ObjectivePullWeight { get; private set; } = DEFAULT_OBJECTIVE_PULL_WEIGHT;

        /// <summary>데이터(TbAiConst)에서 읽은 값으로 AI 수치를 교체한다(App 이 전투 전 1회 주입).</summary>
        public static void Override(int candidateCap, int enemyShortlist, int flankBackNorm, int flankSideNorm,
            int flankFrontNorm, int lookaheadK, int lookaheadWeight, int threatDirectionAvgPerMille, int supportBuffNorm,
            int objectivePullWeight)
        {
            CandidateCap = candidateCap;
            EnemyShortlist = enemyShortlist;
            FlankBackNorm = flankBackNorm;
            FlankSideNorm = flankSideNorm;
            FlankFrontNorm = flankFrontNorm;
            LookaheadK = lookaheadK;
            LookaheadWeight = lookaheadWeight;
            ThreatDirectionAvgPerMille = threatDirectionAvgPerMille;
            SupportBuffNorm = supportBuffNorm;
            ObjectivePullWeight = objectivePullWeight;
        }
    }
}
