using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Tile : IXmlSerializable
{
    Action<Tile> tileChangedEvent;
    public int x, y;
    string type = ObjectType.Empty;
    public Category category {
        get {
            return Data.GetTileCategory(Data.GetTileData(type).category);
        }
    }
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
        Tile[] neighbors;

        if (!diagonals)
        {
            neighbors = new Tile[4];
        }
        else
        {
            neighbors = new Tile[8];
        }

        neighbors[0] = North();
        neighbors[1] = East();
        neighbors[2] = South();
        neighbors[3] = West();

        if (diagonals)
        {
            neighbors[4] = NorthEast();
            neighbors[5] = SouthEast();
            neighbors[6] = SouthWest();
            neighbors[7] = NorthWest();
        }

        return neighbors;
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

    public static Tile FindClosestTileCategory(Tile starting, string targetCategory)
    {
        List<Tile> checkedTiles = new List<Tile>();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        
        tilesToCheck.Enqueue(starting);

        while (tilesToCheck.Count > 0)
        {
            Tile t = null;
            while (tilesToCheck.Count > 0)
            {
                t = tilesToCheck.Dequeue();

                if (checkedTiles.Contains(t)) continue;

                break;
            }
            
            if (t == null && tilesToCheck.Count == 0) return null;

            checkedTiles.Add(t);

            if (t != null && t.category.id == Data.GetCategoryId(targetCategory)) return t;

            Tile[] ns = t.GetNeighbors();
            foreach (Tile t2 in ns)
            {
                if (checkedTiles.Contains(t2))
                {
                    continue;
                }

                if (t2 != null)
                {
                    tilesToCheck.Enqueue(t2);
                }
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
        return world.GetTile(x + 1, y + 1);
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