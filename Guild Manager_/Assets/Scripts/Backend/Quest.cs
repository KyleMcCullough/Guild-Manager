using UnityEngine;
using System.Collections.Generic;

public class Quest
{
    public string title, description;
    public int id;
    public float minTimeToSolve, maxTimeToSolve;
    AdventurerRank requiredRank;

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