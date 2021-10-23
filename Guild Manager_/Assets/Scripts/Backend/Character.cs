using System;
using System.Threading;
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

    #region Character variables
    Inventory inventory = new Inventory();

    #endregion

    #region Pathing variables
    public Tile currTile { get; protected set; }
    Tile nextTile;
    Path_AStar pathing;
    Tile destTile;
    float movementPercent;
    float speed = 2f;

    Action<Character> characterChanged;
    Job parentJob;
    Job currentJob;
    static Thread tileGraphThread = null;
    public static bool pathingIsRefreshed = false;
    
    public static bool itemsAreRefreshed = false;

    #endregion

    public Character(Tile tile)
    {
        currTile = destTile = nextTile = tile;
    }

    void Update_DoJob(float deltaTime)
    {
        if (currentJob == null)
        {
            // Gets new job.
            if (parentJob == null)
            {
                currentJob = currTile.world.jobQueue.Dequeue();
            }

            if (currentJob != null || parentJob != null)
            {

                // if (!currentJob.tile.structure.hasJob || currentJob.tile.structure.parentStructure != null && !currentJob.tile.structure.parentStructure.hasJob)
                // {
                //     return;
                // }

                if (this.parentJob != null || currentJob != null && currentJob.requiredMaterials != null)
                {

                    // If this is a new job, assign it to parent.
                    if (this.parentJob == null)
                    {
                        this.parentJob = currentJob;
                        this.currentJob = null;
                    }

                    // If the job has all required materials, build it.
                    if (parentJob.requiredMaterials.Count == 0)
                    {
                        this.currentJob = parentJob;
                        this.parentJob = null;
                    }

                    
                    // If the character's inventory is full, or has all the required materials, haul to the construction site.
                    else if (inventory.isFull || inventory.ContainsRequiredMaterials(parentJob.requiredMaterials))
                    {
                        this.currentJob = new Job(this.parentJob.tile, (theJob) => this.HaulToConstructionComplete(), null);
                    }

                    else
                    {
                        Tile searchedTile = null;

                        // Checks each requirement and scan the world to see if the item exists.
                        foreach (BuildingRequirements requirement in this.parentJob.requiredMaterials)
                        {
                            
                            if (inventory.GetCountOfItemInInventory(requirement.material) >= requirement.amount)
                            {
                                Debug.Log("amount already in inventory.");
                                continue;
                            }

                            searchedTile = Item.SearchForItem(requirement.material, this.currTile);

                            // This means that the required material is reachable.
                            if (searchedTile != null) break;
                        }

                        // If none of the required items can be found, abandon it.
                        if (searchedTile == null)
                        {
                            AbandonJob();
                            return;
                        }

                        this.currentJob = new Job(searchedTile, (theJob) => this.HaulingOnComplete(), null);
                    }
                }

                destTile = currentJob.tile;
                currentJob.RegisterJobCancelCallback(OnJobEnded);
                currentJob.RegisterJobCompleteCallback(OnJobEnded);
            }
        }

        if ((currTile == destTile) || currTile.IsNeighbour(destTile, true))
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

        // Add unreachable job to List. Will be added back once the tilegraph is refreshed to try again.
        if (currentJob != null && parentJob == null)
        {
            currentJob.UnregisterJobCancelCallback(OnJobEnded);
            currentJob.UnregisterJobCompleteCallback(OnJobEnded);
            currTile.world.unreachableJobs.Add(currentJob);
            currentJob = null;
        }

        // If there is a parent job assigned, add the parentJob to the list and delete the current job. It will be regenerated later.
        else if (parentJob != null)
        {
            currTile.world.unreachableJobs.Add(parentJob);
            currentJob = null;
            parentJob = null;
        }
    }

    void Update_HandleMovement(float deltatime)
    {

        if ((currTile == destTile) || currTile.IsNeighbour(destTile, true))
        {
            pathing = null;
            return;
        }

        if (nextTile == null || nextTile == currTile)
        {
            // Get next tile from path finder.
            if (pathing == null || pathing.Length() == 0)
            {

                // Create a new thread to generate a new tilegraph if possible.
                if (nextTile.world.tileGraph == null)
                {
                    if (tileGraphThread == null)
                    {
                        tileGraphThread = new Thread(new ThreadStart(Thread_GenerateNewTileGraph));
                        tileGraphThread.Start();
                    }
                    return;
                }

                // Tile cannot be walked on, walked neighbors for available tiles and navigate to the best available one.
                if (destTile.structure.canCreateRooms && destTile.structure.IsConstructed)
                {
                    Tile likelyBestTile = currTile.GetClosestNeighborToGivenTile(destTile);

                    if (likelyBestTile == null)
                    {
                        AbandonJob();
                        return;
                    }

                    pathing = new Path_AStar(currTile.world, currTile, likelyBestTile);
                }

                // Generate path to destination tile.
                else
                {
                    pathing = new Path_AStar(currTile.world, currTile, destTile);
                }
                

                if (pathing.Length() == 0)
                {
                    Debug.LogWarning("Path_AStar returned no path to destination.");
                    AbandonJob();
                    return;
                }
            }

            // Get next tile from pathing.
            nextTile = pathing.Dequeue();

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
            
            // If there are no more tiles, then we have actually arrived.
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

        // If there is a new pathing mesh or items, change all non-reachable jobs to canBeReached to check if it is now reachable.
        if (pathingIsRefreshed || itemsAreRefreshed)
        {
            pathingIsRefreshed = false;
            itemsAreRefreshed = false;

            foreach (Job job in currTile.world.unreachableJobs)
            {
                currTile.world.jobQueue.Enqueue(job);
            }
            currTile.world.unreachableJobs = new List<Job>();
        }

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

    public void HaulingOnComplete()
    {
        this.inventory.AddItemFromTile(destTile.item);
    }

    public void HaulToConstructionComplete()
    {
        foreach (Item item in this.inventory.items.ToArray())
        {
            if (this.parentJob.IsRequiredType(item.Type))
            {
                item.CurrentStackAmount = this.parentJob.GiveMaterial(item.Type, item.CurrentStackAmount);
            }
        }
    }

    static void Thread_GenerateNewTileGraph()
    {
        WorldController.Instance.World.tileGraph = new Path_TileGraph(WorldController.Instance.World);
        tileGraphThread = null;
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