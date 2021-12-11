
// Classes that serve no other purpose then simplifying string assignments.
public static class ObjectType
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

// All job types
public enum JobType
{
    Construction,
    Demolition,
    Hauling,
    Sleeping,
    Consuming
}

// Jobs that will be saved
public enum SaveableJob
{
    Construction,
    Demolition
}