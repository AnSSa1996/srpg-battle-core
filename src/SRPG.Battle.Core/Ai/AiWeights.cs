using SRPG.Battle.Core.Constants;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// AI 고려요소 가중치 데이터 테이블(‰). 최종 가중치 = 클래스 base × 스탠스 배율 / 1000. (AI-130)
    /// ★예시값(프로토 1차값)★ — 실제는 ai_weights.csv / ai_stance.csv 로 이관 예정(GROW-105).
    /// 이 파일은 로직이 아닌 데이터라 매직넘버 규칙(S109)에서 예외다(editorconfig).
    /// 열 순서 = Consideration: Damage, Kill, Flank, Focus, SelfRisk, Approach, Heal, Defend.
    /// </summary>
    public static class AiWeights
    {
        // 행 = HeroClass(Warrior, Rogue, Archer, Mage, Priest), 열 = Consideration(8). ★기본값 = TbAiWeight 와 동일★
        private static int[][] _baseWeights =
        {
            new[] { 800, 1500, 300, 600, 300, 1300, 200, 400 },    // Warrior
            new[] { 1200, 2000, 1500, 1000, 800, 1200, 100, 200 }, // Rogue
            new[] { 1200, 1800, 200, 1200, 1200, 700, 150, 200 },  // Archer
            new[] { 1300, 1500, 100, 600, 1200, 700, 400, 500 },   // Mage
            new[] { 200, 0, 0, 0, 1000, 500, 2000, 1500 }          // Priest
        };

        // 행 = Stance(Attack, Defense, Hold, Protect), 열 = Consideration(8). ★기본값 = TbAiStance 와 동일★
        private static int[][] _stanceMults =
        {
            new[] { 1500, 1500, 1300, 1300, 700, 1300, 700, 700 },  // Attack
            new[] { 1000, 1000, 900, 900, 1500, 700, 1300, 1200 },  // Defense
            new[] { 1000, 1000, 800, 800, 1000, 500, 1000, 1000 },  // Hold
            new[] { 800, 800, 800, 800, 1200, 900, 1800, 1600 }     // Protect
        };

        /// <summary>
        /// 데이터(Luban TbAiWeight/TbAiStance)에서 읽은 값으로 가중치 테이블을 교체한다(App 이 전투 전 1회 주입).
        /// 헤드리스/테스트는 주입 없이 위 기본값을 쓴다(기본값 = CSV 값이라 결과 동일).
        /// </summary>
        public static void Override(int[][] baseWeights, int[][] stanceMults)
        {
            if (baseWeights != null)
            {
                _baseWeights = baseWeights;
            }

            if (stanceMults != null)
            {
                _stanceMults = stanceMults;
            }
        }

        /// <summary>클래스·스탠스·고려요소의 최종 가중치(‰)를 반환한다.</summary>
        public static int FinalWeight(HeroClass heroClass, Stance stance, Consideration consideration)
        {
            int column = (int)consideration;
            int baseWeight = _baseWeights[(int)heroClass][column];
            int stanceMult = _stanceMults[(int)stance][column];
            return baseWeight * stanceMult / Permille.BASE;
        }
    }
}
