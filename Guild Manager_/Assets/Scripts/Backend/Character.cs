using System.Linq;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character : IXmlSerializable
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
    public Inventory inventory = new Inventory(Data.MaxInventory);
    public int id;

    #endregion

    #region Pathing variables
    public Tile currTile;
    public JobQueue prioritizedJobs = new JobQueue();
    public State state = State.Working;
    Tile nextTile;
    Path_AStar pathing;
    Tile destTile;
    float movementPercent;
    float speed = 2f;
    public bool spawned {get; private set;}
    public Quest quest;
    buildingRequirement haulingRequirement;
    Action<Character> characterChanged;
    Action<Character> characterDeleted;
    public Job parentJob {get; private set;}
    public Job currentJob;
    static Thread tileGraphThread = null;

    #endregion

    public Character(Tile tile, int id, bool spawned = true)
    {
        currTile = destTile = nextTile = tile;
        this.id = id;
        this.spawned = spawned;
    }

    void Update_DoJob(float deltaTime)
    {
        if (currentJob == null)
        {
            // Gets new job.
            if (parentJob == null)
            {
                
                if (prioritizedJobs != null && prioritizedJobs.Count > 0)
                {
                    currentJob = prioritizedJobs.Dequeue();
                }

                else
                {
                    currentJob = Room.GetNextAvailableJob(currTile);
                    int itemsInWorld = Room.GetCountOfAllItemsInWorld(currTile.world);
                
                    // If there are no more tasks, check if items need hauling to storage.
                    if (currentJob == null && Room.GetCountOfAvailableStorages(currTile.world) > 0)
                    {

                        Structure closestStorage = Room.GetClosestAvailableStorage(currTile);

                        // If the character's inventory isn't full and there are items, get the closest item and create a job.
                        if (closestStorage != null && !this.inventory.isFull && itemsInWorld > 0)
                        {
                            Tile searchedTile = Item.GetClosestItem(currTile);
                            if (searchedTile != null)
                            {
                                this.currentJob = new Job(searchedTile, (theJob) => this.HaulingOnComplete(), JobType.Hauling, null);
                            }
                        }

                        else if (closestStorage != null && this.inventory.items.Count > 0)
                        {
                            this.currentJob = new Job(closestStorage.parent, (job) => this.HaulToStorageComplete(), JobType.Hauling, null);
                        }
                    }
                }

            }

            if (currentJob != null || parentJob != null)
            {

                if (this.parentJob != null || currentJob != null && currentJob.requiredMaterials != null)
                {

                    // If this is a new job, assign it to parent.
                    if (this.parentJob == null)
                    {
                        this.parentJob = currentJob;
                        this.currentJob = null;
                    }

                    // If the job has all required materials, build it.
                    if (parentJob.HasNoRequirements())
                    {
                        this.currentJob = parentJob;
                        this.parentJob = null;
                    }

                    
                    // If the character's inventory is full, or has all the required materials, haul to the construction site.
                    else if (inventory.isFull || inventory.ContainsRequiredMaterials(parentJob.requiredMaterials))
                    {
                        this.currentJob = new Job(this.parentJob.tile, (theJob) => this.HaulToConstructionComplete(), JobType.Hauling, null);
                    }

                    else
                    {
                        Tile searchedTile = null;

                        // Checks each requirement and scan the world to see if the item exists.
                        foreach (buildingRequirement requirement in this.parentJob.requiredMaterials)
                        {
                            
                            if (inventory.GetCountOfItemInInventory(requirement.material) >= requirement.amount)
                            {
                                Debug.Log("amount already in inventory.");
                                continue;
                            }

                            if (Item.ItemExistsOnMap(currTile, requirement.material))
                            {
                                searchedTile = Item.SearchForItem(requirement.material, this.currTile);
                                haulingRequirement = requirement;
                            }

                            // This means that the required material is reachable.
                            if (searchedTile != null) break;
                        }

                        // If none of the required items can be found, abandon it.
                        if (searchedTile == null)
                        {
                            AbandonJob();
                            return;
                        }

                        this.currentJob = new Job(searchedTile, (theJob) => this.HaulingOnComplete(), JobType.Hauling, null);
                    }
                }

                destTile = currentJob.tile;
                currentJob.RegisterJobCancelCallback(OnJobEnded);
                currentJob.RegisterJobCompleteCallback(OnJobEnded);
            }
        }

        // For when character is off the map.
        if (!spawned) {
            if (currentJob != null && currentJob.jobType == JobType.Questing) {

                if (!currentJob.manuallySetJobTime) {
                    currentJob.SetJobTime(UnityEngine.Random.Range(quest.minTimeToSolve, quest.maxTimeToSolve));
                }
                currentJob.DoWork(deltaTime);
            }
            return;
        }

        // Checks if there is a nearby queue for the job tile. If there is, join it.
        if (currentJob != null && Enum.IsDefined(typeof(QueueingJob), currentJob.jobType.ToString()) && this.state != State.Queueing && (currentJob.tile.structure.UsedByCharacterID != this.id && currentJob.tile.structure.UsedByCharacterID != -1) && QueueForTileExists())
        {
            AssignWaitingJob();
        }

        if (currTile == destTile || currTile.IsNeighbour(destTile, true) && currentJob != null && (currentJob.jobType != JobType.Exiting || currentJob.jobType != JobType.Passing))
        {

            // If the job has queueing enabled, check that it isn't being used.
            if (currentJob != null && !Enum.IsDefined(typeof(QueueingJob), currentJob.jobType.ToString()) || currentJob != null && Enum.IsDefined(typeof(QueueingJob), currentJob.jobType.ToString()) && (currentJob.tile.structure.UsedByCharacterID == this.id || currentJob.tile.structure.UsedByCharacterID == -1))
            {
                this.state = State.Working;

                if (currentJob.tile.structure.UsedByCharacterID == -1)
                    currentJob.tile.structure.UsedByCharacterID = id;

                currentJob.DoWork(deltaTime);
            }

            // If a character has reserved the current tile, begin a queue.
            else if (currentJob != null && Enum.IsDefined(typeof(QueueingJob), currentJob.jobType.ToString()) && currentJob.tile.structure.UsedByCharacterID != this.id)
            {
                AssignWaitingJob();
            }
        }
    }

    bool QueueForTileExists()
    {
        if (currTile.world.characters.Count == 1) return false;

        foreach (Character c in currTile.world.characters.ToArray())
        {
            if (c == this) continue;

            if (c.currentJob != null && currentJob != null && currTile.IsNeighbour(c.currTile, true) && (c.currentJob.tile == currentJob.tile || c.parentJob != null && c.parentJob.tile == currentJob.tile) && (c.currentJob.jobType == JobType.Waiting || c.state == State.Queueing) 
            || currentJob != null && currTile.IsNeighbour(c.currTile, true) && currentJob.tile.structure.UsedByCharacterID == c.id)
            {
                return true;
            }
        }
        return false;
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
            currTile.room.unreachableJobs.Add(currentJob);
            currentJob = null;
        }

        // If there is a parent job assigned, add the parentJob to the list and delete the current job. It will be regenerated later.
        else if (parentJob != null)
        {
            if (currTile.room != null) currTile.room.unreachableJobs.Add(parentJob);
            currentJob = null;
            parentJob = null;
        }
    }

    void Update_HandleMovement(float deltatime)
    {

        if (currTile == destTile || currTile.IsNeighbour(destTile, true) && currentJob != null && (currentJob.jobType != JobType.Exiting))
        {
            pathing = null;
            return;
        }

        if (nextTile == null || nextTile == currTile)
        {

            // If the character is passing, give set path.
            if (pathing == null && currentJob.jobType == JobType.Passing) {
                if (currentJob.tile == currTile.world.npcManager.outOfMapSpawnpoints[1]) {
                    pathing = currTile.world.mainRoadPath.Copy();
                } 
                
                else if (currentJob.tile == currTile.world.npcManager.outOfMapSpawnpoints[0]) {
                    pathing = currTile.world.mainRoadPath.Copy(true);
                }

                // This is only entered on loading, as the character will always start from one of the 2 points when being created.
                if (currTile != currTile.world.npcManager.outOfMapSpawnpoints[0] && currTile != currTile.world.npcManager.outOfMapSpawnpoints[1]) {
                    while (true) {
                        Tile t = pathing.Dequeue();

                        if (t == currTile) break;
                    }
                }
            }

            // Get next tile from path finder.
            else if (pathing == null || pathing.Length() == 0)
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

        // If a character is despawned and has no jobs, destroy them.
        if (!spawned && prioritizedJobs.Count == 0 && this.currentJob == null && this.parentJob == null) {
            this.Destroy();
        }

        Update_DoJob(deltaTime);

        // Only allow movement if character is spawned in map.
        if (spawned) {
            Update_HandleMovement(deltaTime);
        }

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
        if (destTile.structure != null && destTile.structure.structureCategory == StructureCategory.Storage && destTile.structure.inventory != null)
        {
            destTile.structure.inventory.TransferItem(this.inventory, haulingRequirement.material, haulingRequirement.amount);
        }

        else
        {
            destTile.room.RemoveItemFromRoom(destTile.item.Type, destTile.item.CurrentStackAmount);
            this.inventory.AddItemFromTile(ref destTile.item);
        }
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

    public void HaulToStorageComplete()
    {
        foreach (Item item in this.inventory.items.ToArray())
        {
            item.CurrentStackAmount = this.currentJob.tile.structure.inventory.AddItem(item.type, item.CurrentStackAmount);
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

    
    public void RegisterOnDeletedCallback(Action<Character> callback)
    {
        characterDeleted += callback;
    }

    // Visually remove the character.
    public void Despawn()
    {
        this.spawned = false;
    }

    // Visually add the character
    public void EnterMap()
    {
        this.spawned = true;
    }

    // Remove all references to the character.
    public void Destroy()
    {
        if (characterDeleted != null)
        {
            currTile.world.characters.Remove(this);
            characterDeleted(this);
        }
    }

    private void AssignWaitingJob(float time = 1f)
    {
        this.state = State.Queueing;
        currentJob.UnregisterJobCancelCallback(OnJobEnded);
        currentJob.UnregisterJobCompleteCallback(OnJobEnded);
        this.parentJob = currentJob;

        this.currentJob = new Job(currTile, null, JobType.Waiting, null, time);
        this.destTile = this.nextTile = this.currTile;
        currentJob.RegisterJobCancelCallback(OnJobEnded);
        currentJob.RegisterJobCompleteCallback(OnJobEnded);
    }

    void OnJobEnded(Job job)
    {

        //FIXME: Workaround for character getting glitched into non-traversable structure. Make smoother fix
        if (currTile.structure.IsConstructed && currTile.structure.canCreateRooms)
        {
            currTile = currTile.GetClosestNeighborToGivenTile(currTile);
        }

        nextTile = destTile = currTile;
        
        if (job != currentJob)
        {
            Debug.LogError("Chracter::OnJobEnded - Character being told about job that isn't his." + $"{job.jobType} - {currentJob.jobType}");
            return;
        }

        currentJob.tile.structure.UsedByCharacterID = -1;
        state = State.Idling;
        currentJob = null;
    }
    
    #region Saving/Loading
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		writer.WriteAttributeString("x", currTile.x.ToString());
		writer.WriteAttributeString("y", currTile.y.ToString());
        writer.WriteAttributeString("id", this.id.ToString());
        writer.WriteAttributeString("spawned", this.spawned.ToString());

        if (quest != null) {
            writer.WriteAttributeString("questId", this.quest.id.ToString());
        }
	}

	public void ReadXml(XmlReader reader) 
    {
        if (reader.GetAttribute("items") != null && reader.GetAttribute("amounts") != null)
        {
            string[] items = reader.GetAttribute("items").Split('/');
            string[] amounts = reader.GetAttribute("amounts").Split('/');

            // Get all saved inventory items and add them.
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == "" || int.Parse(amounts[i]) == 0) continue;

                inventory.AddItem(items[i], int.Parse(amounts[i]));
            }
        }
	}

    #endregion
}