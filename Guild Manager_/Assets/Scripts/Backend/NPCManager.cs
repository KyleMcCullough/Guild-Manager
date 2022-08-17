using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class NPCManager
{
    World world;
    public List<Quest> quests;
    public List<Structure> jobBoards;
    public List<Tile> outOfMapSpawnpoints;

    public NPCManager(World world)
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
        c.prioritizedJobs.AddLast(new Job(jobBoard.parent, (job) => SubmitQuest(), JobType.QuestGiving, null, 2f));
        c.prioritizedJobs.AddLast(new Job(spawnpoint, (job) => c.Despawn(), JobType.Exiting, null, 0f));
    }

    public void SpawnQuestTaker()
    {
        if (jobBoards.Count == 0 || outOfMapSpawnpoints.Count == 0) return;

        // Select random spawnpoint on path and random job board.
        Tile spawnpoint = outOfMapSpawnpoints[Random.Range(0, outOfMapSpawnpoints.Count)];
        Structure jobBoard = jobBoards[Random.Range(0, jobBoards.Count)];
        
        Character c = world.CreateCharacter(spawnpoint);

        // Create jobs to accept quest, exit map and do it, then return to hand it in, and finally delete itself after exiting the map again.
        c.prioritizedJobs.AddLast(new Job(jobBoard.parent, (job) => TakeQuest(c), JobType.QuestTaking, null, 2f));
        c.prioritizedJobs.AddLast(new Job(spawnpoint, (job) => c.Despawn(), JobType.Exiting, null, 0f));
        c.prioritizedJobs.AddLast(new Job(spawnpoint, (job) => c.Spawn(), JobType.Questing, null));
        c.prioritizedJobs.AddLast(new Job(jobBoard.parent, (job) => CompleteQuest(), JobType.HandingInQuest, null, 2f));
        c.prioritizedJobs.AddLast(new Job(spawnpoint, (job) => c.Despawn(), JobType.Exiting, null, 0f));
    }

    public void SpawnPasserBy()
    {
        if (world.mainRoadPath == null || world.mainRoadPath.Length() == 0 || outOfMapSpawnpoints.Count == 0) return;


        List<Tile> spawnPoints = new List<Tile>();
        spawnPoints.AddRange(outOfMapSpawnpoints);

        Tile spawnpoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Character c = world.CreateCharacter(spawnpoint);
        spawnPoints.Remove(spawnpoint);

        // Create jobs to travel to the opposite side of map and delete itself.
        c.prioritizedJobs.AddLast(new Job(spawnPoints[Random.Range(0, spawnPoints.Count)], (job) => c.Despawn(), JobType.Passing, null, 0f));
    }

    public void SubmitQuest()
    {
        quests.Add(Data.questTemplates[Random.Range(0, Data.questTemplates.Count)]);
    }

    public void TakeQuest(Character c)
    {
        //TODO: Make logical decisions for which quests the npc can handle based on skill set.

        if (quests.Count > 0) {
            int num = Random.Range(0, quests.Count);

            c.quest = quests[num];
            quests.RemoveAt(num);
        }

        // If there are no quests to take, tell character to leave map and delete itself.
        else {
            c.prioritizedJobs.Clear();
            c.prioritizedJobs.AddLast(new Job(outOfMapSpawnpoints[Random.Range(0, outOfMapSpawnpoints.Count)], (job) => c.Despawn(), JobType.Exiting, null, 0f));
        }

    }

    public void CompleteQuest()
    {
        //TODO: Add monetary, xp and other rewards on completion.
        Debug.Log("Character handed in quest.");
    }

}