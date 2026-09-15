namespace SRPG.Battle.Core.Constants
{
    /// <summary>
    /// 전투 전역 상수의 프로토타입 1차값. 실제 값은 데이터 테이블(combat_const)로 관리 예정.
    /// (참조: Docs/02_성장영웅/18_밸런스_수치.md GROW-120, 예시값)
    /// </summary>
    public static class CombatConst
    {
        /// <summary>행동권 임계값(THRESHOLD). CT 가 이 값 이상이면 행동권을 얻는다. (CMB-012)</summary>
        public const int ACTION_THRESHOLD = 10000;

        /// <summary>명중 시 최소 피해. (CMB-105)</summary>
        public const int MIN_DAMAGE = 1;

        /// <summary>방어 비율 경감 상수. 경감후 = raw × DEF_K/(방어+DEF_K). (CMB-103)</summary>
        public const int DEF_K = 800;

        /// <summary>명중확률 하한(‰)·상한(‰). 절대 명중/회피 방지. (CMB-101)</summary>
        public const int HIT_MIN_PER_MILLE = 50;
        public const int HIT_MAX_PER_MILLE = 1000;

        /// <summary>치명확률 하한(‰)·상한(‰). (CMB-104c)</summary>
        public const int CRIT_MIN_PER_MILLE = 0;
        public const int CRIT_MAX_PER_MILLE = 1000;

        /// <summary>방향 피해 배율(‰): 정면/측면/후면. (CMB-104, CMB-021)</summary>
        public const int DIR_FRONT_MULT = 1000;
        public const int DIR_SIDE_MULT = 1250;
        public const int DIR_BACK_MULT = 1500;

        /// <summary>방향 명중 보정(‰): 측면만. 정면·후면은 0(후면은 명중이 아니라 치명으로 보정). (CMB-101/434)</summary>
        public const int DIR_SIDE_HIT_BONUS = 100;

        /// <summary>방향 치명확률 보정(‰): 후면(백어택)만. 정면/측면은 0. 예시값. (CMB-021/434)</summary>
        public const int DIR_BACK_CRIT_BONUS = 150;

        /// <summary>궁극 게이지 범위·증가량. 평타 사용 +25 / 피격 +10, 100 도달 시 발동. (CMB-301~303)</summary>
        public const int ULT_GAUGE_MAX = 100;
        public const int ULT_GAIN_ON_BASIC = 25;
        public const int ULT_GAIN_ON_HIT = 10;

        /// <summary>회복 최소량. (CMB-106)</summary>
        public const int MIN_HEAL = 1;
    }
}
