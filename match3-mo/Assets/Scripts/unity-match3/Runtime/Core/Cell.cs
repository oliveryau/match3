using System.Collections.Generic;

namespace Match3
{
    /// <summary>
    /// Logic cell. Port of CellItemModel plus the flags CellView used while matching.
    /// </summary>
    public class Cell
    {
        public int Id;
        public int ColorId;
        public ObjectType ObjectType;
        public CellType CellType;
        public SpecialType Special;
        public MoveType MoveType = MoveType.Down;
        public CleanUpType CleanUpType = CleanUpType.OperateCleanUp;
        public GridPos Grid;
        public CellState State = CellState.Idle;
        public int Hp = 1;
        public int BatchId;
        public int BeCleanUpBatchId = -1;
        public bool CanOperate = true;
        public bool CanMerge = true;
        public bool CanCleanUpByRoundItem;
        public bool IsActive = true;
        public readonly List<int> SimilarTypes = new List<int>();

        public bool IsNormal => CellType == CellType.Normal;
        public bool IsSpecial => CellType == CellType.Special;
        public bool IsObstacle => CellType == CellType.Obstacle;
        public bool IsNormalOrSpecial => IsNormal || IsSpecial;
        public bool CanMoveDown => MoveType != MoveType.Fixed;
        public bool CanMoveLeftRight => MoveType == MoveType.LeftRightDown;

        public bool CanFind()
        {
            return IsActive
                   && State != CellState.Move
                   && State != CellState.Drag
                   && (State == CellState.Idle || State == CellState.Born);
        }

        public bool CanUse()
        {
            return IsActive && (State == CellState.Idle || State == CellState.Born);
        }

        public bool IsSameType(Cell other)
        {
            if (other == null) return false;
            if (ObjectType == other.ObjectType) return true;
            return SimilarTypes.Contains((int)other.ObjectType)
                   || other.SimilarTypes.Contains((int)ObjectType);
        }

        public bool MatchesColor(int colorId)
        {
            return IsNormal && ColorId == colorId;
        }

        public static Cell CreateNormal(int colorId, GridPos grid)
        {
            return new Cell
            {
                Id = colorId,
                ColorId = colorId,
                ObjectType = (ObjectType)colorId,
                CellType = CellType.Normal,
                Special = SpecialType.None,
                Grid = grid,
                CanMerge = true,
                CanOperate = true,
                MoveType = MoveType.Down,
                CleanUpType = CleanUpType.OperateCleanUp
            };
        }

        public static Cell CreateSpecial(SpecialType special, int colorId, GridPos grid)
        {
            var cell = new Cell
            {
                ColorId = colorId,
                CellType = CellType.Special,
                Special = special,
                Grid = grid,
                CanMerge = false,
                CanOperate = true,
                MoveType = MoveType.Down,
                CleanUpType = CleanUpType.OperateCleanUp
            };
            switch (special)
            {
                case SpecialType.HMissile:
                    cell.ObjectType = ObjectType.HMissile;
                    cell.Id = 100 + colorId;
                    break;
                case SpecialType.VMissile:
                    cell.ObjectType = ObjectType.VMissile;
                    cell.Id = 110 + colorId;
                    break;
                case SpecialType.Propeller:
                    cell.ObjectType = ObjectType.Propeller;
                    cell.Id = 120 + colorId;
                    break;
                case SpecialType.PowderKeg:
                    cell.ObjectType = ObjectType.PowderKeg;
                    cell.Id = 130 + colorId;
                    break;
                case SpecialType.LightBall:
                    cell.ObjectType = ObjectType.LightBall;
                    cell.Id = 140;
                    break;
            }
            return cell;
        }

        public static Cell CreateObstacle(ObjectType type, GridPos grid, int hp = 1, bool roundHit = true)
        {
            return new Cell
            {
                Id = (int)type,
                ObjectType = type,
                CellType = CellType.Obstacle,
                Grid = grid,
                Hp = hp,
                CanOperate = false,
                CanMerge = false,
                MoveType = MoveType.Fixed,
                CanCleanUpByRoundItem = roundHit,
                CleanUpType = CleanUpType.OperateCleanUp
            };
        }
    }
}
