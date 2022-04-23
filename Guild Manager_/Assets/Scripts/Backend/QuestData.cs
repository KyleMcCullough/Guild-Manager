using System.Runtime.Serialization;

[System.Serializable]
public class QuestData
{
    public string title, description;
    public float minTimeToSolve, maxTimeToSolve;
    public int requiredRank, id;
}

[System.Serializable]
public class QuestDataArray
{
    public QuestData[] quests;
}