namespace SRPG.Battle.Core.Data
{
    /// <summary>승리 목표 타입. 충족 시 즉시 승리 판정. (CMB-512)</summary>
    public enum ObjectiveType
    {
        Annihilate = 0,
        KillBoss = 1,
        Capture = 2,
        Survive = 3,
        Escort = 4
    }
}
