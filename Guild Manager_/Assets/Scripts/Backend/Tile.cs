using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Tile : IXmlSerializable
{
    public int x, y;
    Action<Tile> tileChangedEvent;
    string type = ObjectType.Empty;
    public Structure structure;
    public Room room = null;
    public World world;
    public Item item = null;

    public float movementCost
    {
        get
        {

            // Unwalkable.
            if (!Data.CheckIfTileIsWalkable(this.Type))
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

    public Tile(int x, int y, World world, Room outside)
    {
        this.x = x;
        this.y = y;
        this.structure = new Structure(this);
        this.world = world;
        this.room = outside;
    }

    public void RegisterTileChangedDelegate(Action<Tile> callback)
    {
        tileChangedEvent += callback;
    }

    public Tile FindNearestItem(string item)
    {
        int radius = 1;

        if (this.item != null && this.item.Type == item)
        {
            return this;
        }

        for (int x = this.x - radius; x < this.x + radius; x++)
        {
            for (int y = this.y - radius; y < this.x + radius; y++)
            {
                Tile t = world.GetTile(x, y);
                if (!t.structure.canCreateRooms && item != null && t.item.Type == item)
                {
                    // Tries to create a path to see if it can be reached.
                    if (new Path_AStar(world, this, t).Length() != 0) return t;
                }
            }

            radius ++;
        }

        // This means the required item is not on the map.
        Debug.Log("FindNearestItem - " + item + " is not on the map or cannot be found.");
        return null;
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

    public static Tile GetSafePlaceForPlayerSpawning(Tile starting)
    {
        List<Tile> checkedTiles = new List<Tile>();
        Queue<Tile> tilesToCheck = new Queue<Tile>();

        tilesToCheck.Enqueue(starting);

        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();
            checkedTiles.Add(t);

            if (Data.CheckIfTileIsWalkable(t.Type)) return t;

            Tile[] ns = t.GetNeighbors();
            foreach (Tile t2 in ns)
            {
                if (checkedTiles.Contains(t2))
                {
                    continue;
                }

                if (t2 == null)
                {
                    return null;
                }

                // We know t2 is not null nor is it an empty tile, so just make sure it
                // hasn't already been processed and isn't a "wall" type tile.
                if (!checkedTiles.Contains(t2))
                {
                    tilesToCheck.Enqueue(t2);
                }
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


    #region Saving/Loading
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        Type = reader.GetAttribute("type");
    }

    public void WriteXml(XmlWriter writer)
    {
		writer.WriteAttributeString( "x", x.ToString());
		writer.WriteAttributeString( "y", y.ToString());
		writer.WriteAttributeString("type", Type);
    }
    #endregion

}