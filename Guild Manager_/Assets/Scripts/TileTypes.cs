
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

public enum State
{
    Working,
    Queueing,
    Idling
}

// All job types
public enum JobType
{
    Construction,
    Demolition,
    Drinking,
    Hygiene,
    Sleep,
    Eating,
    Exiting,
    HandingInQuest,
    Hauling,
    Passing,
    QuestGiving,
    QuestTaking,
    Questing,
    Waiting,
    Temporary
}

// Jobs that will be saved
public enum SaveableJob
{
    Construction,
    Demolition,
    Drinking,
    Hygiene,
    Eating,
    Sleep,
    Exiting,
    HandingInQuest,
    Passing,
    QuestGiving,
    QuestTaking,
    Questing,
}

public enum QueueingJob
{
    HandingInQuest,
    QuestGiving,
    QuestTaking
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