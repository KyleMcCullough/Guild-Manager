using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class Structure
{
    #region variables
    public Tile parent;
    public string TypeCategory;
    public Structure parentStructure = null;
    public List<Tile> overlappedStructureTiles;
    String type = ObjectType.Empty;

    public Dictionary<string, object> optionalParameters = new Dictionary<string, object>();
    public Action<Structure, float> updateActions = null;

    public String Type
    {
        get { return type; }

        set
        {
            String previous = type;
            type = value;

            // Call callback to refresh tile visually.
            if (structureChangedEvent != null && previous != type)
            {
                structureChangedEvent(this);
            }
        }
    }

    bool isConstructed = false;
    public bool canCreateRooms = false;
    public bool IsConstructed
    {
        get { return isConstructed; }

        set
        {
            if (value)
            {
                isConstructed = true;
                if (structureChangedEvent != null)
                {
                    structureChangedEvent(this);
                }
            }

            else
            {
                isConstructed = false;
            }
        }
    }

    // Multiplyer, a value of 2 is twice as slow as 1. These can be combined with tile movementCost and other modifiers.
    // If the movementCost is 0, then it cannot be passed through.
    public float movementCost { get; protected set; }
    int width = 1;
    int height = 1;
    public bool linksToNeighbour { get; set; }

    Func<Tile, bool> positionValidation;

    public Action<Structure> structureChangedEvent { get; protected set; }

    #endregion
    public void RegisterObjectChangedDelegate(Action<Structure> callback)
    {
        structureChangedEvent += callback;
    }

    public Structure() { }
    public Structure(Tile parent)
    {
        this.parent = parent;
    }

    public Structure(Tile parent, String Type)
    {
        this.Type = Type;
        this.parent = parent;
    }

    public void Update(float deltaTime)
    {
        if (this.updateActions != null)
        {
            this.updateActions(this, deltaTime);
        }
    }

    public bool PlaceStructure(Structure prototype)
    {
        if (!this.IsValidPosition(this.parent))
        {
            return false;
        }

        this.width = prototype.width;
        this.height = prototype.height;

        if (this.width > 1 || this.height > 1)
        {
            this.ReserveRequiredTiles();
        }

        if (this.updateActions != null)
        {
            this.RemoveActions();
        }

        if (prototype.updateActions != null)
        {
            this.AssignActions((Action<Structure, float>)prototype.updateActions.Clone());
        }

        this.optionalParameters = prototype.optionalParameters;
        this.IsConstructed = false;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.movementCost = prototype.movementCost;
        this.TypeCategory = prototype.TypeCategory;
        this.canCreateRooms = prototype.canCreateRooms;
        this.Type = prototype.Type;

        if (this.linksToNeighbour)
        {
            foreach (Tile tile in this.parent.GetNeighbors())
            {
                if (tile != null && tile.structure.Type == this.Type)
                {
                    tile.structure.structureChangedEvent(tile.structure);
                }
            }
        }
        return true;
    }

    public void AssignActions(Action<Structure, float> newActions)
    {
        this.updateActions = newActions;
        this.parent.world.updatingStructures.Add(this);
    }

    public void RemoveActions()
    {
        this.updateActions = null;
        this.parent.world.updatingStructures.Remove(this);
    }

    // Reserves tiles for structures with a width or height more then 1.
    void ReserveRequiredTiles()
    {
        this.overlappedStructureTiles = new List<Tile>();
        for (int x = parent.x; x != (parent.x + this.width); x++)
        {
            for (int y = parent.y; y != (parent.y + this.height); y++)
            {
                this.overlappedStructureTiles.Add(this.parent.world.GetTile(x, y));
                parent.world.GetTile(x, y).structure.parentStructure = this;
            }
        }
    }

    void ReleaseRequiredTiles()
    {
        if (this.overlappedStructureTiles == null || this.overlappedStructureTiles.Count == 0) return;

        foreach (Tile tile in this.overlappedStructureTiles)
        {
            tile.structure.parentStructure = null;
        }
        this.overlappedStructureTiles = null;
    }

    public void CompleteStructure()
    {
        this.IsConstructed = true;

        if (this.canCreateRooms)
        {
            Thread UpdateRoomThread = new Thread(new ThreadStart(this.Thread_UpdateRooms_Creation));
            UpdateRoomThread.Start();
        }

        if (this.movementCost != 1)
        {
            parent.world.InvalidateTileGraph();
        }

        Character.pathingIsRefreshed = true;
    }

    public void RemoveStructure()
    {
        Structure prototype = this.parent.world.structurePrototypes[ObjectType.Empty];

        if (this.updateActions != null)
        {
            this.RemoveActions();
        }

        if (this.overlappedStructureTiles != null && this.overlappedStructureTiles.Count > 0)
        {
            this.ReleaseRequiredTiles();
        }

        this.optionalParameters = prototype.optionalParameters;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.Type = prototype.Type;
        this.movementCost = prototype.movementCost;
        this.TypeCategory = prototype.TypeCategory;
        this.canCreateRooms = prototype.canCreateRooms;
        this.parent.world.GetOutSideRoom().AssignTile(this.parent);
        Debug.Log(this.parent.world.rooms.IndexOf(this.parent.room));

        Thread UpdateRoomThread = new Thread(new ThreadStart(this.Thread_UpdateRooms_Deletion));
        UpdateRoomThread.Start();

        foreach (Tile tile in this.parent.GetNeighbors())
        {
            tile.structure.structureChangedEvent(tile.structure);
        }

        Character.pathingIsRefreshed = true;
    }

    static public Structure CreatePrototype(String type, string TypeCategory, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false, bool canCreateRooms = false)
    {

        Structure obj = new Structure();
        obj.TypeCategory = TypeCategory;
        obj.Type = type;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;
        obj.linksToNeighbour = linksToNeighbour;
        obj.canCreateRooms = canCreateRooms;
        obj.positionValidation = obj.IsValidPosition;

        return obj;
    }

    public bool IsValidPosition(Tile tile)
    {
        if (this.width > 1 || this.height > 1)
        {
            for (int x = tile.x; x != (tile.x + this.width); x++)
            {
                for (int y = tile.y; y != (tile.y + this.height); y++)
                {
                    Tile t = tile.world.GetTile(x, y);
                    if (!Data.CheckIfTileIsWalkable(tile.Type) || t.structure.parentStructure != null)
                    {
                        return false;
                    }
                }
            }
        }

        else
        {
            if (!Data.CheckIfTileIsWalkable(tile.Type) || tile.structure.parentStructure != null) return false;
        }
        return true;
    }

    #region Door Methods
    public bool IsDoor()
    {
        return this.TypeCategory == "Door";
    }

    public bool IsDoorOpen()
    {
        return (float)this.optionalParameters["openness"] == 1;
    }

    public void OpenDoor()
    {
        if (!IsDoor() || (bool)this.optionalParameters["doorIsOpening"] == true)
        {
            return;
        }

        this.optionalParameters["doorIsOpening"] = true;
    }

    public void CloseDoor()
    {
        if (!IsDoor())
        {
            return;
        }

        this.optionalParameters["doorIsOpening"] = false;
    }

    #endregion

    #region MultiThreading Methods

    void Thread_UpdateRooms_Creation()
    {
        Room.FloodFillRoom(this);
    }

    void Thread_UpdateRooms_Deletion()
    {
        Room.FloodFill_Remove(this);
    }

    #endregion
}