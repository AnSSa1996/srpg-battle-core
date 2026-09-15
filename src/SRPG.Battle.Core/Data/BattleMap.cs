using System;
using System.Collections.Generic;
using SRPG.Battle.Core.Grid;

namespace SRPG.Battle.Core.Data
{
    /// <summary>
    /// 한 전투 맵(불변 입력 데이터). 이 데이터 + seed 만으로 전투가 결정론적으로 돌아간다.
    /// (참조: Docs/01_전투/23_전투맵_데이터.md CMB-500~501)
    /// </summary>
    public sealed class BattleMap
    {
        // 타일 값: 0=통행 가능, 1=통행 불가 (CMB-500)
        private const int PASSABLE_TILE = 0;
        private const int WALL_TILE = 1;

        private readonly int[,] _tiles;

        /// <summary>맵 식별자.</summary>
        public string MapId { get; }

        /// <summary>그리드 가로 크기(타일).</summary>
        public int Width { get; }

        /// <summary>그리드 세로 크기(타일).</summary>
        public int Height { get; }

        /// <summary>승리 목표.</summary>
        public Objective Objective { get; }

        /// <summary>타임아웃 행동 수. (CMB-052/054)</summary>
        public int TurnLimit { get; }

        /// <summary>적용 필드효과 목록.</summary>
        public IReadOnlyList<FieldEffect> FieldEffects { get; }

        /// <summary>전투 맵을 만든다. tiles 는 [height, width] 배열(0=통행 가능, 1=통행 불가). 유닛 배치는 BattleSetup.Units/PendingWaves 가 소유한다.</summary>
        public BattleMap(string mapId, int[,] tiles, Objective objective, int turnLimit,
            IReadOnlyList<FieldEffect> fieldEffects)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            _tiles = (int[,])tiles.Clone();
            Height = _tiles.GetLength(0);
            Width = _tiles.GetLength(1);
            MapId = mapId;
            Objective = objective;
            TurnLimit = turnLimit;
            FieldEffects = fieldEffects ?? new List<FieldEffect>();
        }

        /// <summary>해당 타일이 통행 불가 지형인지 반환한다. 범위 밖은 통행 불가로 본다. (CMB-410)</summary>
        public bool IsWall(Coord tile)
        {
            if (GridDistance.IsInBounds(tile, Width, Height) == false)
            {
                return true;
            }

            return _tiles[tile.Y, tile.X] == WALL_TILE;
        }

        /// <summary>해당 타일이 통행 가능 지형인지 반환한다.</summary>
        public bool IsPassable(Coord tile)
        {
            if (GridDistance.IsInBounds(tile, Width, Height) == false)
            {
                return false;
            }

            return _tiles[tile.Y, tile.X] == PASSABLE_TILE;
        }
    }
}
