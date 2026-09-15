namespace SRPG.Battle.Core.Grid
{
    /// <summary>
    /// 도달 집합(Reach) 계산에 필요한 맵·점유 정보를 제공한다.
    /// 전투 상태(BattleState)가 이동 주체 기준으로 이 인터페이스를 구현한다(M3에서 연결).
    /// (참조: Docs/01_전투/22_전투_그리드_규칙.md CMB-410~413)
    /// </summary>
    public interface IReachQuery
    {
        /// <summary>그리드 가로 크기(타일).</summary>
        int Width { get; }

        /// <summary>그리드 세로 크기(타일).</summary>
        int Height { get; }

        /// <summary>해당 타일이 통행 불가 지형(벽 등)인지 반환한다. (CMB-410)</summary>
        bool IsWall(Coord tile);

        /// <summary>이동 주체 기준 타일 점유 상태를 반환한다. 자기 자신이 선 칸은 None 으로 본다. (CMB-412)</summary>
        TileOccupant OccupantOf(Coord tile);
    }
}
