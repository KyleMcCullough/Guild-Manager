using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character
{
    public float x
    {
        get
        {
            return Mathf.Lerp(currTile.x, nextTile.x, movementPercent);
        }
    }

    public float y
    {
        get
        {
            return Mathf.Lerp(currTile.y, nextTile.y, movementPercent);
        }
    }

    public Tile currTile { get; protected set; }
    Tile nextTile;
    Path_AStar pathing;
    Tile destTile;
    float movementPercent;
    float speed = 2f;

    Action<Character> characterChanged;
    Job currentJob;

    public Character(Tile tile)
    {
        currTile = destTile = nextTile = tile;
    }

    void Update_DoJob(float deltaTime)
    {
        if (currentJob == null)
        {
            // Gets new job.
            currentJob = currTile.world.jobQueue.Dequeue();

            if (currentJob != null)
            {

                //TODO: Check if reachable

                destTile = currentJob.tile;
                currentJob.RegisterJobCancelCallback(OnJobEnded);
                currentJob.RegisterJobCompleteCallback(OnJobEnded);
            }
        }

        if (currTile == destTile)

        // Do task adjacent to tile.
        // if (pathing != null && pathing.Length() == 1) 
        {

            if (currentJob != null)
            {
                currentJob.DoWork(deltaTime);
            }
        }
    }

    public void AbandonJob()
    {
        nextTile = destTile = currTile;
        pathing = null;
        currTile.world.jobQueue.Enqueue(currentJob);
        currentJob = null;
    }

    void Update_HandleMovement(float deltatime)
    {

        if (currTile == destTile)
        {
            pathing = null;
            return;
        }

        if (nextTile == null || nextTile == currTile)
        {
            // Get next tile from path finder.
            if (pathing == null || pathing.Length() == 0)
            {
                // Generate path to destination tile.
                pathing = new Path_AStar(currTile.world, currTile, destTile);

                if (pathing.Length() == 0)
                {
                    // Debug.LogWarning("Path_AStar returned no path to destination.");
                    AbandonJob();
                    //TODO: Cancel job if no path is found. or Reqeueue it.
                    pathing = null;
                    return;
                }
            }

            // Get next tile from pathing.
            nextTile = pathing.Dequeue();

            if (nextTile == currTile)
            {
                //Debug.LogWarning("Path_AStar returned current tile.");
            }

            if (pathing.Length() == 1) return;
        }

        if (nextTile.structure.IsDoor() && !nextTile.structure.IsDoorOpen())
        {
            nextTile.structure.OpenDoor();
            return;
        }

        // Gets total distance from destination tile.
        // Using Euclidean distance for more, but will switch to 
        // something like Manhatten or Chebyshev distance for calculations.
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.x - nextTile.x, 2) + Mathf.Pow(currTile.y - nextTile.y, 2));

        // Gets the distance we can travel this update.
        float distThisFrame = speed * deltatime;

        // Gets the percent that we have moved this update.
        float perThisFrame = distThisFrame / distToTravel;

        movementPercent += perThisFrame;

        // If movementPercent is equal to or above 1, we reached the destination.
        if (movementPercent >= 1)
        {
            //TODO: Get next tile from pathfinding system.
            //      If there are no more tiles, then we have actually arived.

            if (nextTile.structure.IsDoor())
            {
                nextTile.structure.CloseDoor();
            }

            currTile = nextTile;
            movementPercent = 0;
        }
    }

    public void Update(float deltaTime)
    {
        Update_DoJob(deltaTime);
        Update_HandleMovement(deltaTime);

        if (characterChanged != null) characterChanged(this);
    }

    public void SetDestination(Tile tile)
    {
        if (currTile.IsNeighbour(tile) == false)
        {
            Debug.Log("Characer::SetDestination - Desination is not beside current tile.");
        }

        destTile = tile;
    }

    public void RegisterOnChangedCallback(Action<Character> callback)
    {
        characterChanged += callback;
    }

    public void UnregisterOnChangedCallback(Action<Character> callback)
    {
        characterChanged -= callback;
    }

    void OnJobEnded(Job job)
    {
        // Called whether job was completed or cancelled.
        if (job != currentJob)
        {
            Debug.LogError("Chracter::OnJobEnded - Character being told about job that isn't his.");
            return;
        }

        currentJob = null;
    }
}
