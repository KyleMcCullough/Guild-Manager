using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Structure
{
    public Tile Parent;
    public Structure parentStructure = null;
    public List<int[]> overlappedStructureCoords = new List<int[]>();
    ObjectType type = ObjectType.Empty;

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

        this.IsConstructed = false;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.Type = prototype.Type;
        this.movementCost = prototype.movementCost;

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
        return true;
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
    }

    static public Structure CreatePrototype(ObjectType type, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false)
    {

        Structure obj = new Structure();
        obj.Type = type;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;
        obj.linksToNeighbour = linksToNeighbour;
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
}