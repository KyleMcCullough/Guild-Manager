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
    public String name;
    Dictionary<string, float> needs;
    public int id;

    #endregion

    #region Pathing variables
    Action<Character> characterChanged;
    Action<Character> characterDeleted;
    Path_AStar pathing;
    Tile destTile;
    Tile nextTile;
    bool activeNeedsJob;
    buildingRequirement haulingRequirement;
    float movementPercent;
    float speed = 2f;
    public Job currentJob;
    public Job parentJob {get; set;}
    public JobQueue prioritizedJobs = new JobQueue();
    public Quest quest;
    public State state = State.Working;
    public Tile currTile;
    public bool spawned {get; private set;}
    static Thread tileGraphThread = null;

    #endregion

    public Character(Tile tile, String name, int id, Dictionary<string, float> needs = null, bool spawned = true, bool activeNeedsJob = false)
    {
        currTile = destTile = nextTile = tile;
        this.id = id;
        this.name = name;
        this.spawned = spawned;

        if (needs == null) {
            this.needs = new Dictionary<string, float>();

            foreach (var item in Data.characterNeeds)
            {
                this.needs[item] = 100;
            }
        } 
        
        else {
            this.needs = needs;
        }

        this.activeNeedsJob = activeNeedsJob;
    }

    #region Update

    public void Update(float deltaTime)
    {

        // If a character is despawned and has no jobs, destroy them.
        if (!spawned && prioritizedJobs.Count == 0 && this.currentJob == null && this.parentJob == null || !spawned && prioritizedJobs.Count == 1 && prioritizedJobs.Peek().jobType == JobType.Drinking) {
            this.Destroy();
        }

        if (spawned) {
            Update_Needs(deltaTime);
        }

        Update_DoJob(deltaTime);

        // Only allow movement if character is spawned in map.
        if (spawned) {
            Update_HandleMovement(deltaTime);
        }

        if (characterChanged != null) characterChanged(this);
    }

    void Update_DoJob(float deltaTime)
    {
        if (currentJob == null)
        {
            // Gets new job.
            if (parentJob == null)
            {

                // Check if a need job is required and attempt to generate it. If the required need is not found, skip for now.
                if (!activeNeedsJob && spawned && prioritizedJobs != null && (prioritizedJobs.Count == 0 || prioritizedJobs.Count > 0 && prioritizedJobs.Peek().jobType != JobType.Passing)) {

                    string lowestNeedName = "";
                    float lowestNeedValue = 100f;

                    // Get lowest need value
                    foreach (var item in needs.Keys.ToArray())
                    {
                        if (needs[item] < lowestNeedValue) {
                            lowestNeedName = item;
                            lowestNeedValue = needs[item];
                        }
                    }

                    // If the lowest need value is below threshold, create a job.
                    if (lowestNeedValue < Data.needsThreshold)
                    {
                        activeNeedsJob = true;

                        switch (lowestNeedName)
                        {
                            case "thirst": {
                                Tile t = Tile.FindClosestTileCategory(currTile, "Water");

                                if (t != null) {
                                    this.prioritizedJobs.AddFirst(new Job(t, (job) => DrinkingOnComplete(), JobType.Drinking, null, 2f));
                                }
                                break;
                            }

                            case "hygiene": {
                                Tile t = Tile.FindClosestTileCategory(currTile, "Water");

                                if (t != null) {
                                    this.prioritizedJobs.AddFirst(new Job(t, (job) => HygieneOnComplete(), JobType.Hygiene, null, 3f));
                                }                                
                                break;
                            }

                            case "sleep": {
                                //FIXME: Should characters automatically look for enclosed spaces to sleep?
                                
                                Tile t = null;

                                // Checks all rooms for sleep category structures
                                foreach (var room in currTile.world.rooms)
                                {
                                    if (room.ContainsStructure("Sleep"))
                                    {
                                        t = Tile.FindClosestStructureType(currTile, "Sleep");
                                    }
                                }

                                // If there is no sleeping object, get a random tile and sleep there.
                                if (t != null) {
                                    this.prioritizedJobs.AddFirst(new Job(t, (job) => SleepingOnComplete(), JobType.Sleep, null, 10f));
                                } else {
                                    this.prioritizedJobs.AddFirst(new Job(Tile.GetRandomNearbyTile(currTile, 5), (job) => SleepingOnComplete(), JobType.Sleep, null, 10f));
                                }
                                
                                break;
                            }

                            case "hunger": {

                                Tile t = null;

                                // Checks all rooms for sleep category structures
                                foreach (var room in currTile.world.rooms)
                                {
                                    if (room.ContainsItemCategory(1))
                                    {
                                        t = Tile.FindClosestItemByCategory(currTile, 1);
                                    }
                                }

                                if (t != null) {
                                    this.prioritizedJobs.AddFirst(new Job(t, (job) => EatingOnComplete(), JobType.Eating, null, 5f));
                                }

                                break;
                            }

                            default: {
                                DebugConsole.LogError($"Character needs - {lowestNeedName} is not a valid need.");
                                break;
                            }
                        }
                    }
                }
                
                if (prioritizedJobs != null && prioritizedJobs.Count > 0)
                {
                    currentJob = prioritizedJobs.Dequeue();
                }

                else
                {
                    currentJob = Room.GetNextAvailableJob(currTile);
                    int itemsInWorld = Room.GetCountOfAllItemsInWorld(currTile.world);
                
                    // If there are no more tasks, check if items need hauling to storage.
                    if (currentJob == null && (itemsInWorld > 0 || inventory.items.Count > 0) && Room.GetCountOfAvailableStorages(currTile.world) > 0)
                    {

                        // Assign a temporary job as a placeholder, and generate using a new thread.
                        currentJob = new Job(currTile, null, JobType.Temporary, null, 5f);
                        new Thread(new ThreadStart(AssignHaulingJob)).Start();
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

        if (currentJob != null && Data.directDestinationJobs.Contains(currentJob.jobType) && currTile == destTile || currentJob != null && !Data.directDestinationJobs.Contains(currentJob.jobType) && (currTile.IsNeighbour(destTile, true) || currTile == destTile))
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

    void Update_HandleMovement(float deltatime)
    {

        if (currTile == destTile || currTile.IsNeighbour(destTile, true) && !Data.directDestinationJobs.Contains(currentJob.jobType))
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

    public void Update_Needs(float deltaTime)
    {
        foreach (string need in needs.Keys.ToArray())
        {
            if (needs[need] > 0) {
                needs[need] -= .3f * deltaTime;
            }
        }
    }
    #endregion

    #region Jobs

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

    public void HaulingOnComplete()
    {
        if (destTile.structure != null && destTile.structure.category.id == Data.GetCategoryId("Storage") && destTile.structure.inventory != null)
        {
            destTile.structure.inventory.TransferItem(this.inventory, haulingRequirement.material, haulingRequirement.amount);
        }

        else
        {
            destTile.room.RemoveItemFromRoom(destTile.item.Type, destTile.item.CurrentStackAmount);
            this.inventory.AddItemFromTile(ref destTile.item);
        }
        currTile.room.ResetUnreachableJobs();
    }

    //TODO: Merge needs compleition functions together once their increment amount can be modified elsewhere.
    public void DrinkingOnComplete()
    {
        needs["thirst"] += 30;
        activeNeedsJob = false;
    }

    public void HygieneOnComplete()
    {
        needs["hygiene"] += 50;
        activeNeedsJob = false;
    }

    public void SleepingOnComplete()
    {
        needs["sleep"] += 80;
        activeNeedsJob = false;
    }

    public void EatingOnComplete()
    {
        needs["hunger"] += 50;
        activeNeedsJob = false;
        destTile.item.CurrentStackAmount -= 1;
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

    void AssignHaulingJob()
    {
        Structure closestStorage = Room.GetClosestAvailableStorage(currTile);

        // If the character's inventory isn't full and there are items, get the closest item and create a job.
        if (closestStorage != null && !this.inventory.isFull && Room.GetCountOfAllItemsInWorld(currTile.world) > 0)
        {
            Tile searchedTile = Item.GetClosestItem(currTile);
            
            if (searchedTile != null)
            {
                prioritizedJobs.AddFirst(new Job(searchedTile, (theJob) => this.HaulingOnComplete(), JobType.Hauling, null));
            }
        }

        else if (closestStorage != null && this.inventory.items.Count > 0)
        {
            prioritizedJobs.AddFirst(new Job(closestStorage.parent, (job) => this.HaulToStorageComplete(), JobType.Hauling, null));
        }
        currentJob = null;
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

    #endregion

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

    // Visually add the character
    public void Spawn()
    {
        this.spawned = true;
    }

    // Visually remove the character.
    public void Despawn()
    {
        this.spawned = false;
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
    
    #region Saving/Loading
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		writer.WriteAttributeString("x", currTile.x.ToString());
		writer.WriteAttributeString("y", currTile.y.ToString());
        writer.WriteAttributeString("id", this.id.ToString());
        writer.WriteAttributeString("name", this.name);
        writer.WriteAttributeString("spawned", this.spawned.ToString());
        writer.WriteAttributeString("activeNeedsJob", this.activeNeedsJob.ToString());

        string _needs = "";
        string values = "";

        foreach (var need in needs.Keys)
        {
            _needs += need + "/";
            values += needs[need] + "/";
        }

        writer.WriteAttributeString("needs", _needs);
        writer.WriteAttributeString("values", values);

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