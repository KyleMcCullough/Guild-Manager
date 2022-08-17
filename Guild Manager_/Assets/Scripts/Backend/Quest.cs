using UnityEngine;
using System.Collections.Generic;

public class Quest
{
    AdventurerRank requiredRank;
    public float minTimeToSolve, maxTimeToSolve;
    public int id;
    public string title, description;

    public Quest(string title, string description, float minTimeToSolve, float maxTimeToSolve, AdventurerRank requiredRank, int id)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.minTimeToSolve = minTimeToSolve;
        this.maxTimeToSolve = maxTimeToSolve;
        this.requiredRank = requiredRank;
    }
}