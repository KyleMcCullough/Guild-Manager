
// Classes that serve no other purpose then simplifying string assignments.
public static class ObjectType
{
    public static string Empty {get; private set;} = "Empty";
}

public static class TileType
{
    public static string Empty {get; private set;} = "Empty";
}

public static class ItemType
{
    public static string Empty {get; private set;} = "Empty";
}

public enum Facing
{
    North,
    East,
    South,
    West
}