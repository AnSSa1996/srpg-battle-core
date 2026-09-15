namespace SRPG.Battle.Core.Data
{
    /// <summary>필드효과 적용 지점. applyType 에 따라 적용 위치가 고정된다. (CMB-520)</summary>
    public enum FieldApplyType
    {
        DamageMod = 0,
        StatMod = 1,
        MovMod = 2,
        TurnTick = 3
    }

    /// <summary>필드효과 대상 범위 종류. (CMB-521)</summary>
    public enum FieldFilterKind
    {
        All = 0,
        Class = 2,
        Side = 3
    }
}
