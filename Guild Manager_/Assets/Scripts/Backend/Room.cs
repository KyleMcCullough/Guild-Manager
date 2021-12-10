using UnityEngine;
using System.Collections.Generic;

public class Room
{
    float temperature;
    List<Tile> tiles;
    IDictionary<string, int> itemsInRoom;
    public JobQueue jobQueue;
    public List<Job> unreachableJobs;

    public Room()
    {
        this.tiles = new List<Tile>();
        this.itemsInRoom = new Dictionary<string, int>();
        this.jobQueue = new JobQueue();
        this.unreachableJobs = new List<Job>();
    }

    #region Assignment/Deletion functions

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

    public void AssignItemToRoom(string type, int amount)
    {
        if (amount == 0) return;

        if (itemsInRoom == null)
        {
            itemsInRoom = new Dictionary<string, int>();
        }
        
        if (itemsInRoom.ContainsKey(type))
        {
            itemsInRoom[type] = itemsInRoom[type] + amount;
        }

        else
        {
            itemsInRoom.Add(type, amount);
        }

        // PrintDictionary();
    }

    public void RemoveItemFromRoom(string type, int amount)
    {
        if (itemsInRoom == null)
        {
            itemsInRoom = new Dictionary<string, int>();
        }
        
        if (itemsInRoom.ContainsKey(type))
        {
            itemsInRoom[type] = itemsInRoom[type] - amount;
        
            if (itemsInRoom[type] <= 0)
            {
                itemsInRoom.Remove(type);
            }
        }

        // PrintDictionary();
    }

    public bool ContainsItem(string type)
    {
        return itemsInRoom.ContainsKey(type);
    }

    public void ResetUnreachableJobs()
    {
        foreach (Job job in unreachableJobs)
        {
            jobQueue.Enqueue(job);
        }

        unreachableJobs = new List<Job>();
    }

    void PrintDictionary()
    {
        foreach (KeyValuePair<string, int> line in itemsInRoom)
        {
            Debug.Log(line.Key + " " + line.Value);
        }
    }

    public void UnassignAllTiles()
    {
        if (this.tiles == null) return;

        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutSideRoom();
        }

        this.tiles = null;
    }

    public static void FloodFill_Remove(Structure source)
    {
        bool outside = false;
        Tile validTile = null;

        // Checks all neighbors to see if the change has exposed it to the outside.
        foreach (Tile tile in source.parent.GetNeighbors())
        {
            if (tile.room == null) continue;

            if (tile.room == tile.world.GetOutSideRoom())
            {
                outside = true;
            }

            else
            {
                validTile = tile;
            }
        }

        // Deletes room if exposed to the outside.
        if (outside)
        {
            if (validTile != null)
            {
                Room oldRoom = validTile.room;

                if (oldRoom != null && oldRoom.tiles.Count > 0)
                {
                    oldRoom.UnassignAllTiles();


                Room outsideRoom = source.parent.world.GetOutSideRoom();
                foreach (var entry in oldRoom.itemsInRoom)
                {
                    outsideRoom.AssignItemToRoom(entry.Key, entry.Value);
                }

                    source.parent.world.DestroyRoom(oldRoom);
                }
            }

            return;
        }

        // Checks amount of rooms.
        List<Room> rooms = new List<Room>();
        foreach (Tile tile in source.parent.GetNeighbors())
        {
            if (tile.room != null && !rooms.Contains(tile.room))
            {
                rooms.Add(tile.room);
            }
        }

        // If there are more then 2 rooms, merge them.
        if (rooms.Count > 0)
        {
            Room oldRoom = rooms[0];
            source.parent.room = oldRoom;

            foreach (Room room in rooms)
            {
                if (room == oldRoom) continue;
                
                foreach (Tile t in room.tiles.ToArray())
                {
                    oldRoom.AssignTile(t);
                }

                foreach (var entry in room.itemsInRoom)
                {
                    oldRoom.AssignItemToRoom(entry.Key, entry.Value);
                }

                source.parent.world.DestroyRoom(room);
            }

            oldRoom.AssignTile(source.parent);
        }

        // If there is only 1 room, add the tile to the room.
        else
        {
            foreach (Tile tile in source.parent.GetNeighbors())
            {
                if (!tile.structure.canCreateRooms && tile.room != source.parent.world.GetOutSideRoom())
                {
                    tile.room.AssignTile(source.parent);
                    return;
                }

                source.parent.room = source.parent.world.GetOutSideRoom();
            }
        }

    }

    public static void FloodFillRoom(Structure source)
    {

        World world = source.parent.world;
        Room oldRoom = source.parent.room;

        // Try building new rooms for each of our NESW directions
        foreach (Tile t in source.parent.GetNeighbors())
        {
            FloodFill(t, oldRoom);
        }

        source.parent.room = null;
        
        if (oldRoom != null && oldRoom.tiles != null)
        {
            oldRoom.tiles.Remove(source.parent);
        }

        // If this piece of furniture was added to an existing room
        // (which should always be true assuming with consider "outside" to be a big room)
        // delete that room and assign all tiles within to be "outside" for now

        if (oldRoom != world.GetOutSideRoom())
        {
            // At this point, oldRoom shouldn't have any more tiles left in it,
            // so in practice this "DeleteRoom" should mostly only need
            // to remove the room from the world's list.

            // if (oldRoom != null && oldRoom.tiles.Contains(source.parent))
            // {
            //     oldRoom.tiles.Remove(source.parent);
            // }

            if (oldRoom != null && oldRoom.itemsInRoom.Count > 0)
            {
                foreach (var entry in oldRoom.itemsInRoom)
                {
                    source.parent.room.AssignItemToRoom(entry.Key, entry.Value);
                }
            }

            if (oldRoom != null && oldRoom.tiles.Count > 0)
            {
                Debug.LogError("'oldRoom' still has tiles assigned to it. This is clearly wrong.");
            }

            world.DestroyRoom(oldRoom);
        }
    }

    static void FloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null || tile.room != oldRoom || tile.structure != null && tile.structure.canCreateRooms && tile.structure.IsConstructed)
        {
            return;
        }

        // If we get to this point, then we know that we need to create a new room.
        Room newRoom = new Room();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();

            if (t.room == oldRoom)
            {
                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbors();
                foreach (Tile t2 in ns)
                {
                    if (t2 == null || Mathf.Abs(t2.x) > Mathf.Abs(tile.x + 8) || Mathf.Abs(t2.y) > Mathf.Abs(tile.y + 8))
                    {
                        newRoom.UnassignAllTiles();
                        return;
                    }

                    // We know t2 is not null nor is it an empty tile, so just make sure it
                    // hasn't already been processed and isn't a "wall" type tile.
                    if (t2.room == oldRoom && (t2.structure == null || t2.structure.canCreateRooms == false || t2.structure.canCreateRooms && !t2.structure.IsConstructed))
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                }

            }
        }

        // Tell the world that a new room has been formed.
        tile.world.AddRoom(newRoom);
    }
    public static Job GetNextAvailableJob(Tile tile)
    {
        // There is only an outside room.
        if (tile.world.rooms.Count == 1)
        {
            return tile.world.GetOutSideRoom().jobQueue.Dequeue();
        }

        List<Tile> checkedTiles = new List<Tile>();
        List<Room> checkedRooms = new List<Room>();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        
        tilesToCheck.Enqueue(tile);

        while (checkedRooms.Count < tile.world.rooms.Count)
        {
            Tile t = null;
            while (tilesToCheck.Count > 0)
            {
                t = tilesToCheck.Dequeue();

                if (checkedTiles.Contains(t)) continue;

                break;
            }

            checkedTiles.Add(t);

            if (t.room != null && !checkedRooms.Contains(t.room))
            {
                checkedRooms.Add(t.room);
                Job job = t.room.jobQueue.Dequeue();

                if (job != null) return job;
            }

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
                if (t2.structure.Type == ObjectType.Empty || !t2.structure.canCreateRooms || t2.structure.canCreateRooms && !t2.structure.IsConstructed || t2.structure.IsDoor() && t2.structure.IsConstructed)
                {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }

        return null;
    }

    #endregion
}