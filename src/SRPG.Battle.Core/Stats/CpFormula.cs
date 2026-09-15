namespace SRPG.Battle.Core.Stats
{
    /// <summary>
    /// 전투력(CP) 산출 — 전투 스탯의 가중 합(정수, 내림). 표시·비교용 근사 지표일 뿐 승패를 직접 결정하지 않는다.
    /// (참조: Docs/02_성장영웅/18_밸런스_수치.md GROW-102/103)
    /// </summary>
    public static class CpFormula
    {
        private const int WEIGHT_HP = 100;
        private const int WEIGHT_ATK = 1000;
        private const int WEIGHT_MAG = 1000;
        private const int WEIGHT_DEF = 700;
        private const int WEIGHT_RES = 700;
        private const int WEIGHT_SPD = 1500;

        /// <summary>스탯 블록의 CP 를 반환한다(가중 합 / 1000 내림).</summary>
        public static int Compute(StatBlock stat)
        {
            long weighted =
                (long)stat.Get(StatType.Hp) * WEIGHT_HP
                + (long)stat.Get(StatType.Atk) * WEIGHT_ATK
                + (long)stat.Get(StatType.Mag) * WEIGHT_MAG
                + (long)stat.Get(StatType.Def) * WEIGHT_DEF
                + (long)stat.Get(StatType.Res) * WEIGHT_RES
                + (long)stat.Get(StatType.Spd) * WEIGHT_SPD;
            return (int)(weighted / Constants.Permille.BASE);
        }
    }
}
