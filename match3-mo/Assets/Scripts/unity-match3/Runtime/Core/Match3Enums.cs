namespace Match3
{
    /// <summary>Port of config/EnumDefine.ts values used by match-3.</summary>
    public enum CellType
    {
        Normal = 1,
        Special = 2,
        Obstacle = 3,
        Grass = 4,
        Honey = 5,
        Bird = 6
    }

    public enum ObjectType
    {
        None = 0,
        Gem = 1,
        Leaf = 2,
        Shield = 3,
        Book = 4,
        Crown = 5,
        HMissile = 10,
        VMissile = 11,
        Propeller = 12,
        PowderKeg = 13,
        LightBall = 14,
        Ice = 20,
        Box = 21,
        Curtain = 22,
        Bird = 23,
        Corn = 24,
        Mailbox = 25,
        FixedItem = 26
    }

    public enum SpecialType
    {
        None = 0,
        HMissile,
        VMissile,
        Propeller,
        PowderKeg,
        LightBall
    }

    public enum MoveType
    {
        Down = 1,
        LeftRightDown = 2,
        Fixed = 3
    }

    public enum CleanUpType
    {
        None = 0,
        OperateCleanUp = 1,
        DropBottomCleanUp = 2
    }

    public enum GridType
    {
        NullGrid = 0,
        NormalGrid = 1
    }

    public enum GridLayer
    {
        Grass = 0,
        Main = 1,
        Honey = 2,
        Max = 3
    }

    public enum FinderCheckType
    {
        CanFind = 1,
        CanUse = 2,
        CanFindAndNormal = 3,
        CanFindAndSpecial = 4,
        CanUseAndNormal = 5
    }

    public enum NeedSameType
    {
        None = 0,
        SameType = 1,
        SimilarType = 2
    }

    public enum FinderId
    {
        SameTypeH = 1,
        SameTypeV = 2,
        AllH = 3,
        AllV = 4,
        TenWord = 5,
        TenWordNotSelf = 6,
        LShape = 7,
        TWord = 8,
        TianWord = 9,
        OneH = 10,
        OneV = 11,
        All = 12,
        RoundTwo = 13,
        RoundFour = 14
    }

    public enum CheckResultState
    {
        Yes = 0,
        No = 1
    }

    public enum PlayState
    {
        WaitOperate,
        Playing,
        CheckCells
    }

    public enum ObstacleCheckType
    {
        Normal = 0,
        IdEqual = 1,
        IdEqualNotSpecial = 2,
        ClickAction = 3,
        NotSameBatch = 4,
        OnlySelf = 5
    }

    public enum CellState
    {
        Idle,
        Born,
        Move,
        Drag,
        Wait,
        CleanUp,
        Destroy,
        Recover,
        Over
    }
}
