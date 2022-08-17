using System.Runtime.Serialization;

[System.Serializable]
public class QuestData
{
    public float minTimeToSolve, maxTimeToSolve;
    public int requiredRank, id;
    public string title, description;
}

[System.Serializable]
public class QuestDataArray
{
    public QuestData[] quests;
}