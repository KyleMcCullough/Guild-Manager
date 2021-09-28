using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Structure
{
    public Tile Parent;
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
    int width;
    int height;
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

        this.IsConstructed = false;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.Type = prototype.Type;
        this.movementCost = prototype.movementCost;
        this.width = prototype.width;
        this.height = prototype.height;

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

        // Makes sure the tile can be built on.
        if (tile.Type != TileType.Dirt) return false;

        // Makes sure there is no existing installedObjects on the tile.
        if (tile.structure.Type != ObjectType.Empty) return false;
        return true;
    }
}