using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Tile
{
    public int x, y;
    Action<Tile> tileChangedEvent;
    string type = "";
    public Structure structure;
    public Room room = null;
    public World world;
    Item item;
    public Item Item 
    {
        get 
        {
            if (item == null) {
                item = new Item(this);
            }
            return item;
        }
    }

    public float movementCost
    {
        get
        {

            // Unwalkable.
            if (!Data.tileData[this.type].walkable)
            {
                return 0;
            }

            if (structure == null || !structure.IsConstructed)
            {
                return 1;
            }

            return 1 * structure.movementCost;
        }
    }

    public string Type
    {
        get { return type; }

        set
        {
            string previous = type;
            type = value;

            // Call callback to refresh tile visually.
            if (tileChangedEvent != null && previous != type)
            {
                tileChangedEvent(this);
            }
        }
    }

    public Tile(int x, int y, World world)
    {
        this.x = x;
        this.y = y;
        this.structure = new Structure(this);
        this.world = world;
    }

    public void RegisterTileChangedDelegate(Action<Tile> callback)
    {
        tileChangedEvent += callback;
    }

    public bool IsNeighbour(Tile tile, bool diagonals = false)
    {

		return 
			Mathf.Abs( this.x - tile.x ) + Mathf.Abs( this.y - tile.y ) == 1 ||  // Check hori/vert adjacency
			( diagonals && ( Mathf.Abs( this.x - tile.x ) == 1 && Mathf.Abs( this.y - tile.y ) == 1 ) ) // Check diag adjacency
			;
    }

    #region Neighbor Methods
    public Tile[] GetNeighbors(bool diagonals = false)
    {
        Tile[] neighbours;

        if (!diagonals)
        {
            neighbours = new Tile[4];
        }
        else
        {
            neighbours = new Tile[8];
        }

        Tile n;

        n = world.GetTile(x, y + 1);
        neighbours[0] = n;

        n = world.GetTile(x + 1, y);
        neighbours[1] = n;

        n = world.GetTile(x, y - 1);
        neighbours[2] = n;

        n = world.GetTile(x - 1, y);
        neighbours[3] = n;

        if (diagonals)
        {
            n = world.GetTile(x + 1, y + 1);
            neighbours[4] = n;
            n = world.GetTile(x + 1, y - 1);
            neighbours[5] = n;
            n = world.GetTile(x - 1, y - 1);
            neighbours[6] = n;
            n = world.GetTile(x - 1, y + 1);
            neighbours[7] = n;
        }

        return neighbours;
    }

    // Gets closest available neighbor.
    public Tile GetClosestNeighborToGivenTile(Tile dest)
    {
        Tile t = null;

        if (this.x > dest.x)
        {
            t = dest.East();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        if (this.x <= dest.x)
        {
            t = dest.West();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        if (this.y > dest.y)
        {
            t = dest.North();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        if (this.y < dest.y)
        {
            t = dest.South();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        if (this.x > dest.x && this.y > dest.y)
        {
            t = dest.SouthWest();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        if (this.x <= dest.x && this.y <= dest.y)
        {
            t = dest.NorthWest();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        if (this.x > dest.x && this.y < dest.y)
        {
            t = dest.SouthEast();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        if (this.x <= dest.x && this.y >= dest.y)
        {
            t = dest.NorthEast();
            if (!t.structure.canCreateRooms || t.structure.canCreateRooms && !t.structure.IsConstructed)
            {
                return t;
            }
        }

        return null;

    }

    public Tile North()
    {
        return world.GetTile(x, y + 1);
    }

    public Tile South()
    {
        return world.GetTile(x, y - 1);
    }

    public Tile East()
    {
        return world.GetTile(x + 1, y);
    }

    public Tile West()
    {
        return world.GetTile(x - 1, y);
    }

    public Tile NorthEast()
    {
        return world.GetTile(x - 1, y + 1);
    }

    public Tile NorthWest()
    {
        return world.GetTile(x - 1, y + 1);
    }

    public Tile SouthEast()
    {
        return world.GetTile(x + 1, y - 1);
    }

    public Tile SouthWest()
    {
        return world.GetTile(x - 1, y - 1);
    }

    #endregion
}