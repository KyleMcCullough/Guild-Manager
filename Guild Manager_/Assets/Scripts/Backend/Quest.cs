using UnityEngine;
using System.Collections.Generic;

public class Quest
{
    public string title, description;
    public int id;
    float timeToSolve;
    AdventurerRank requiredRank;

    public Quest(string title, string description, float timeToSolve, AdventurerRank requiredRank, int id)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.timeToSolve = timeToSolve;
        this.requiredRank = requiredRank;
    }
}