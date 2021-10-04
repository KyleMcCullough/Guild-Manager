using UnityEngine;
using System.Collections.Generic;

public class Room
{
    float temperature;
    List<Tile> tiles;

    public Room()
    {
        this.tiles = new List<Tile>();
    }

    public void AssignTile(Tile t)
    {
        if (this.tiles == null)
        {
            this.tiles = new List<Tile>();
        }

        if (t.room != null)
        {
            t.room.tiles.Remove(t);
        }

        if (tiles.Contains(t))
        {
            return;
        }
        t.room = this;
        tiles.Add(t);
    }

    public void UnassignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutSideRoom();
        }

        this.tiles = null;
    }

    public static void FloodFillRoom(Structure source)
    {

        World world = source.Parent.world;
        Room oldRoom = source.Parent.room;

        foreach (Tile t in source.Parent.GetNeighbours())
        {
            FloodFill(t, oldRoom);
        }

        source.Parent.room = null;
        oldRoom.tiles.Remove(source.Parent);
        
        if (oldRoom != world.GetOutSideRoom())
        {
            if (oldRoom.tiles.Count > 0)
            {
                Debug.LogError("FloodFillRoom - oldRoom still has tiles assigned to it.");
            }
            world.DestroyRoom(oldRoom);
        }
    }

    static void FloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            return;
        }

        if (tile.room != oldRoom)
        {
            // This tile has been assigned to another new room.
            return;
        }

        if (tile.structure != null && tile.structure.canCreateRooms)
        {
            // This tile has wall/door.
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            return;
        }

        Room newRoom = new Room();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();

            if (t.Type == TileType.Empty)
            {
                break;
            }

            if (t.room == oldRoom)
            {
                newRoom.AssignTile(t);

                foreach (Tile t2 in t.GetNeighbours())
                {

                    if (t2 == null || t2.Type == TileType.Empty || Mathf.Abs(t2.x) > Mathf.Abs(tile.x + 8) || Mathf.Abs(t2.y) > Mathf.Abs(tile.y + 8))
                    {
                        newRoom.UnassignAllTiles();
                        return;
                    }

                    if (t2.room == oldRoom && t2.structure == null || t2.structure.canCreateRooms == false || t2.structure.canCreateRooms && !t2.structure.IsConstructed)
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                }
            }
        }
        tile.world.AddRoom(newRoom);
    }
}