
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
    West,
    None
}

// All job types
public enum JobType
{
    Construction,
    Demolition,
    QuestGiving,
    Exiting,
    Hauling,
    Sleeping,
    Consuming
}

// Jobs that will be saved
public enum SaveableJob
{
    Construction,
    Demolition,
    QuestGiving,
    Exiting
}

public enum StructureCategory
{
    None,
    Wall,
    Furniture,
    Door,
    QuestBoard,
    Storage
}

public enum AdventurerRank
{
    F,
    E,
    D,
    C,
    B,
    A,
    S
    

}