using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Structure
{
    #region variables
    public Tile Parent;
    public string TypeCategory;
    public Structure parentStructure = null;
    public List<int[]> overlappedStructureCoords = new List<int[]>();
    ObjectType type = ObjectType.Empty;

    public Dictionary<string, object> optionalParameters = new Dictionary<string, object>();
    public Action<Structure, float> updateActions = null;

    public ObjectType Type
    {
        get { return type; }

        set
        {
            ObjectType previous = type;
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
                structureChangedEvent(this);
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
    public Structure(Tile Parent)
    {
        this.Parent = Parent;
    }

    public Structure(Tile Parent, ObjectType Type)
    {
        this.Type = Type;
        this.Parent = Parent;
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
        if (!this.IsValidPosition(this.Parent))
        {
            return false;
        }

        this.width = prototype.width;
        this.height = prototype.height;

        if (this.width > 1 && this.overlappedStructureCoords.Count == 0 || this.height > 1 && this.overlappedStructureCoords.Count == 0)
        {
            this.ReserveRequiredTiles();
            Debug.Log(overlappedStructureCoords.Count);
        }
        
        if (prototype.updateActions != null)
        {
            this.AssignActions((Action<Structure, float>) prototype.updateActions.Clone());
        }

        this.optionalParameters = prototype.optionalParameters;
        this.IsConstructed = false;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.Type = prototype.Type;
        this.movementCost = prototype.movementCost;
        this.TypeCategory = prototype.TypeCategory;
        this.canCreateRooms = prototype.canCreateRooms;

        if (this.linksToNeighbour)
        {

            Tile tile;
            int x = Parent.x;
            int y = Parent.y;

            tile = Parent.world.GetTile(x, y + 1);
            if (tile != null && tile.structure.Type == this.Type)
            {
                tile.structure.structureChangedEvent(tile.structure);
            }

            tile = tile.world.GetTile(x + 1, y);
            if (tile != null && tile.structure.Type == this.Type)
            {
                tile.structure.structureChangedEvent(tile.structure);
            }

            tile = tile.world.GetTile(x, y - 1);
            if (tile != null && tile.structure.Type == this.Type)
            {
                tile.structure.structureChangedEvent(tile.structure);
            }

            tile = tile.world.GetTile(x - 1, y);
            if (tile != null && tile.structure.Type == this.Type)
            {
                tile.structure.structureChangedEvent(tile.structure);
            }
        }

        // if (this.Parent.Room != null && this.isConstructed && this.canCreateRooms)
        // {
        //         int x = Parent.x;
        //         int y = Parent.y;
        //         while (true)
        //         {
        //         }
        //     }
        // }
        return true;
    }

    public void AssignActions(Action<Structure, float> newActions)
    {
        this.updateActions = newActions;
        this.Parent.world.updatingStructures.Add(this);
    }

    public void RemoveActions()
    {
        this.updateActions = null;
        this.Parent.world.updatingStructures.Remove(this);
    }

    // Reserves tiles for structures with a width or height more then 1.
    void ReserveRequiredTiles()
    {
        for (int x = Parent.x; x != (Parent.x + this.width); x++)
        {
            for (int y = Parent.y; y != (Parent.y + this.height); y++)
            {
                this.overlappedStructureCoords.Add(new int[] { x, y });
                Parent.world.GetTile(x, y).structure.parentStructure = this;
            }
        }
    }

    public void CompleteStructure()
    {
        this.IsConstructed = true;

        if (this.canCreateRooms)
        {
            this.Parent.world.UpdateRooms(this);
        }
    }

    static public Structure CreatePrototype(ObjectType type, string TypeCategory, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false, bool canCreateRooms = false)
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
                    if (t.structure.Type != ObjectType.Empty || t.Type != TileType.Dirt || t.structure.parentStructure != null)
                    {
                        return false;
                    }
                }
            }
        }

        else
        {
            if (tile.structure.Type != ObjectType.Empty || tile.Type != TileType.Dirt || tile.structure.parentStructure != null) return false;
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
        return (float) this.optionalParameters["openness"] == 1;
    }

    public void OpenDoor()
    {
        if (!IsDoor() || (bool) this.optionalParameters["doorIsOpening"] == true)
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
}