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
    public Facing facingDirection = Facing.East;
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
    public int width = 1;
    public int height = 1;
    public bool linksToNeighbour { get; set; }

    // Func<Tile, bool> positionValidation;

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

    public StructureData GetTypeData(string type)
    {
        return Data.GetStructureData(type);
    }

    public bool PlaceStructure(Structure prototype, int width, int height, Facing buildDirection)
    {
        if (!this.IsValidPosition(this.parent, buildDirection, width, height))
        {
            return false;
        }

        this.width = width;
        this.height = height;
        this.facingDirection = buildDirection;

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

        int xPos = parent.x;
        int yPos = parent.y;

        switch (facingDirection)
        {

            case Facing.South:
            {
                yPos = parent.y - (this.height - 1);
                break;
            }

            case Facing.West:
            {
                xPos = parent.x - (this.width - 1);
                break;
            }
            
            default:
            {
                break;
            }
        }

        for (int x = 0; x < this.width; x++)
        {
            for (int y = 0; y < this.height; y++)
            {
                if (xPos + x == this.parent.x && yPos + y == this.parent.y)
                {
                    continue;
                }

                this.overlappedStructureTiles.Add(this.parent.world.GetTile(xPos + x, yPos + y));
                parent.world.GetTile(xPos + x, yPos + y).structure.parentStructure = this;
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

        // If this tile is part of a parent structure, call the remove structure on it and return.
        if (this.parentStructure != null)
        {
            this.parentStructure.RemoveStructure();
            return;
        }

        Structure prototype = this.parent.world.structurePrototypes[ObjectType.Empty];

        if (this.updateActions != null)
        {
            this.RemoveActions();
        }

        this.optionalParameters = prototype.optionalParameters;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.movementCost = prototype.movementCost;
        this.TypeCategory = prototype.TypeCategory;
        this.canCreateRooms = prototype.canCreateRooms;
        this.parent.world.GetOutSideRoom().AssignTile(this.parent);

        Thread UpdateRoomThread = new Thread(new ThreadStart(this.Thread_UpdateRooms_Deletion));
        UpdateRoomThread.Start();

        if (this.overlappedStructureTiles != null && this.overlappedStructureTiles.Count > 0)
        {
            this.ReleaseRequiredTiles();
        }

        
        foreach (buildingRequirement item in this.GetTypeData(this.Type).itemsToDropOnDestroy)
        {
            this.parent.item = new Item(this.parent, item.material, item.amount);
        }

        this.Type = prototype.Type;

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
        // obj.positionValidation = obj.IsValidPosition;

        return obj;
    }

    public bool IsValidPosition(Tile tile, Facing direction, int width, int height)
    {

        if (width > 1 || height > 1)
        {
            int xPos = tile.x;
            int yPos = tile.y;

            switch (direction)
            {

                case Facing.South:
                {
                    yPos = tile.y - (height - 1);
                    break;
                }

                case Facing.West:
                {
                    xPos = tile.x - (width - 1);
                    break;
                }
                
                default:
                {
                    break;
                }
            }

            for (int x = xPos; x < (xPos + width); x++)
            {
                for (int y = yPos; y < (yPos + height); y++)
                {
                    Tile t = tile.world.GetTile(x, y);
                    if (!Data.CheckIfTileIsWalkable(tile.Type) || t.structure.parentStructure != null || tile.structure.IsConstructed == true || t.structure.overlappedStructureTiles != null)
                    {
                        return false;
                    }
                }
            }
        }

        else
        {
            if (!Data.CheckIfTileIsWalkable(tile.Type) || tile.structure.parentStructure != null || tile.structure.IsConstructed == true || tile.structure.overlappedStructureTiles != null) return false;
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

    public void SwitchParentStructure(Structure structure)
    {
        if (structure.parentStructure == null) return;

        this.overlappedStructureTiles = structure.parentStructure.overlappedStructureTiles;
        this.overlappedStructureTiles.Add(structure.parent);

        structure.parentStructure = this;
        structure.overlappedStructureTiles = null;
    }
}