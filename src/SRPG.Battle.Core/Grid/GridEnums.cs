namespace SRPG.Battle.Core.Grid
{
    /// <summary>유닛이 바라보는 4방향. (N=위, E=오른쪽, S=아래, W=왼쪽, CMB-430)</summary>
    public enum Facing
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>피격 면 분류. 후면=백어택. (CMB-433/434)</summary>
    public enum FaceType
    {
        Front = 0,
        Side = 1,
        Back = 2
    }

    /// <summary>스킬 적중 범위 패턴. (CMB-440, 17_스킬_시스템 §5)</summary>
    public enum AoePattern
    {
        Single = 0,
        Cross = 1,
        Square3 = 2,
        LineN = 3,
        SelfAroundR = 4
    }

    /// <summary>이동 주체 기준 타일 점유 상태. (자기 자신은 None 으로 취급, CMB-412)</summary>
    public enum TileOccupant
    {
        None = 0,
        Ally = 1,
        Enemy = 2
    }
}
