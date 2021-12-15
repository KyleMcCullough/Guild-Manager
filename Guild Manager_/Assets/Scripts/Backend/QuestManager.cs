using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class QuestManager
{
    public List<Structure> jobBoards;
    public List<Tile> outOfMapSpawnpoints;
    public List<Quest> quests;
    World world;

    public QuestManager(World world)
    {
        this.jobBoards = new List<Structure>();
        this.outOfMapSpawnpoints = new List<Tile>();
        this.quests = new List<Quest>();
        this.world = world;
    }

    public void SpawnQuestGiver()
    {
        if (jobBoards.Count == 0 || outOfMapSpawnpoints.Count == 0) return;

        // Select random spawnpoint on path and random job board.
        Tile spawnpoint = outOfMapSpawnpoints[Random.Range(0, outOfMapSpawnpoints.Count)];
        Structure jobBoard = jobBoards[Random.Range(0, jobBoards.Count)];
        
        Character c = world.CreateCharacter(spawnpoint);

        // Create jobs to deliver quest, and head to the border of the map and delete itself.
        c.prioritizedJobs.Enqueue(new Job(jobBoard.parent, (job) => SubmitQuest(), JobType.QuestGiving));
        c.prioritizedJobs.Enqueue(new Job(outOfMapSpawnpoints[Random.Range(0, outOfMapSpawnpoints.Count)], (job) => c.Destroy(), JobType.Exiting, null, 0f));
    }



    public void SubmitQuest()
    {
        quests.Add(Data.questTemplates[Random.Range(0, Data.questTemplates.Count)]);
    }

}