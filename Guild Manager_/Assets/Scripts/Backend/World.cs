using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using UnityEngine;

public class World : IXmlSerializable
{
    // Day, month, year.
    float worldTime = 0;
    int[] date;
    public int height;
    public int width;

    #region Collections

    Action<Character> characterCreated;
    Action<Structure> structureChangedEvent;
    Action<Tile> tileChangedEvent;
    List<Character> characters;
    List<Character> queuedCharactersToCreate;
    List<Item> queuedItemsToCreate;
    Tile[,] tiles;
    public Action<Item> itemChangedEvent;
    public Dictionary<string, Structure> structurePrototypes { get; protected set; }
    public List<Room> rooms;
    public List<Structure> structures;
    public List<Structure> updatingStructures;
    public Path_TileGraph tileGraph;
    #endregion

    public World(int width, int height)
    {
        SetupWorld(width, height);
    }

    void SetupWorld(int width, int height, bool generateWorld = true)
    {

        // Instantiating lists
        this.characters = new List<Character>();
        this.queuedCharactersToCreate = new List<Character>();
        this.queuedItemsToCreate = new List<Item>();
        this.rooms = new List<Room>();
        this.structures = new List<Structure>();
        this.updatingStructures = new List<Structure>();

        this.height = height;
        this.width = width;

        date = new int[] { 1, 1, 1253 };
        tiles = new Tile[width, height];
        structurePrototypes = new Dictionary<string, Structure>();
        rooms.Add(new Room());

        Data.LoadData();
        GeneratePrototypes();

        // Generate tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

                tiles[x, y] = new Tile(x, y, this, GetOutSideRoom());
                tiles[x, y].RegisterTileChangedDelegate(OnTileChanged);
                tiles[x, y].structure.RegisterObjectChangedDelegate(OnStructureChanged);
            }
        }

        if (generateWorld) 
        {
            GenerateWorld(UnityEngine.Random.Range(1, 10000).GetHashCode());
            CreateCharacter(GetTile(width / 2, height / 2));
        }
    }

    public void Update(float deltaTime)
    {
        worldTime += (1 * deltaTime);

        foreach (Character c in characters)
        {
            c.Update(deltaTime);
        }

        foreach (Structure structure in updatingStructures)
        {
            structure.Update(deltaTime);
        }

        if (worldTime >= Data.DayLength)
        {
            UpdateDate();
            worldTime = 0f;
        }
    }

    public bool IsDayTime()
    {
        return worldTime <= (Data.DayLength * (1 - ((float)Data.NightRatio / 100)));
    }

    public void UpdateDate()
    {
        date[0]++;

        if (date[0] >= 30)
        {
            date[0] = 0;
            date[1]++;
        }

        if (date[1] > 6)
        {
            date[1] = 0;
            date[2]++;
        }
    }

    public Character CreateCharacter(Tile tile)
    {

        Tile t = Tile.GetSafePlaceForPlayerSpawning(tile);

        if (t == null)
        {
            t = tile;
        }

        Character c = new Character(t);
        characters.Add(c);

        if (characterCreated != null)
        {
            characterCreated(c);
        }

        // If Character cannot be visually created, add it to the queue to create once ready.
        else
        {
            queuedCharactersToCreate.Add(c);
        }

        return c;
    }

    public void GenerateWorld(int seed)
    {
        UnityEngine.Random.InitState(seed);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float height = NoiseCreator.GetNoiseAt(x, y, seed);

                // Assigns tiles
                switch (height)
                {
                    case float n when n > .85f:
                    {
                        tiles[x, y].Type = "Deep_Water";
                        break;
                    }

                    case float n when n > .78f:
                    {
                        tiles[x, y].Type = "Shallow_Water";
                        break;
                    }

                    case float n when n > .2f:
                    {
                        tiles[x, y].Type = "Grass";
                        break;
                    }

                    default:
                    {
                        tiles[x, y].Type = "Dirt";
                        break;
                    }
                }

                // Returns if the tile cannot be planted on.
                if (!Data.CheckIfTileIsFertile(tiles[x, y].Type)) continue;

                height = NoiseCreator.GetNoiseAt(x * 2, y * 2, seed);

                // Assigns plants to fertile tiles.
                switch (height)
                {
                    case float n when n > .8f:
                    {
                        if (UnityEngine.Random.value > .5f)
                        {
                            Structure prototype = structurePrototypes["Tree"];
                            tiles[x, y].structure.PlaceStructure(prototype, prototype.width, prototype.height, Facing.East);
                        }
                        break;
                    }

                    case float n when n > .45f:
                    {
                        if (UnityEngine.Random.value > .95f)
                        {
                            Structure prototype = structurePrototypes["Bush"];
                            tiles[x, y].structure.PlaceStructure(prototype, prototype.width, prototype.height, Facing.East);
                        }
                        break;
                    } 

                    default:
                    {
                        if (UnityEngine.Random.value > .95f)
                        {
                            Structure prototype = structurePrototypes["Tree"];
                            tiles[x, y].structure.PlaceStructure(prototype, prototype.width, prototype.height, Facing.East);
                        }
                        break;
                    }
                }
            }
        }

        // Generate a tilegraph with water tiles excluded for generating the path.
        this.tileGraph = new Path_TileGraph(this, false);
        int attempts = 0;

        //TODO: Think about adding bridges to world generation for paths.
        
        // Continue until the path is finished or it errors out.
        while (true)
        {
            attempts++;
            int value = UnityEngine.Random.Range(0, width);
            Tile startingTile;

            if (UnityEngine.Random.value > .5f)
            {
                startingTile = GetTile(value, 0);
            }

            else
            {
                startingTile = GetTile(0, value);
            }
            
            Tile endingTile = GetTile(width - 1, UnityEngine.Random.Range(0, width));

            if (startingTile.Type != ObjectType.Empty && startingTile.movementCost != 0 && !Data.CheckIfTileIsWater(startingTile.Type) && !startingTile.structure.canCreateRooms)
            {

                Path_AStar pathing = new Path_AStar(this, startingTile, endingTile);

                if (pathing.Length() > 70)
                {
                    startingTile.Type = "Path";
                    while (pathing.Length() > 0)
                    {
                        Tile t = pathing.Dequeue();
                        t.Type = "Path";

                        if (t.structure.Type != ObjectType.Empty)
                        {
                            Structure prototype = structurePrototypes["Empty"];
                            t.structure.PlaceStructure(prototype, prototype.width, prototype.height, Facing.East);
                        }

                        if (t.x == width - 1 && endingTile.x == t.x || t.y == height - 1 && endingTile.y == t.y)
                        {
                            break;
                        }
                    }
                    break;
                }
            }

            //TODO: How will we deal with seeds when a path cannot be created?
            if (attempts > 20)
            {
                Debug.LogError("GenerateWorld - Failed path generation " + attempts + " times.");
                break;
            }
        }

        this.tileGraph = new Path_TileGraph(this);
    }

    void GeneratePrototypes()
    {
        foreach (string type in Data.structureTypes)
        {
            StructureData data = Data.GetStructureData(type);
            Structure structure = Structure.CreatePrototype(type, Data.GetItemCategory(data.category).name, data.movementCost, data.width, data.height, data.linksToNeighbours, data.canCreateRooms);

            // Convert the string to the actual method, and load each function into the delegate
            foreach (string func in data.relatedFunctions)
            {
                if (func != null)
                {
                    structure.updateActions += (Action<Structure, float>)Delegate.CreateDelegate(typeof(Action<Structure, float>), StructureBehaviours.GetMethodInfo(func));
                }
            }

            if (data.relatedParameters.Length % 2 != 0)
            {
                Debug.LogError("There are parameters with no matching values in " + type.ToString());
            }

            string value = "";

            // Load parameters into structure.
            foreach (string item in data.relatedParameters)
            {
                if (value == "")
                {
                    value = item;
                }

                else
                {
                    structure.optionalParameters.Add(value, Data.CastToCorrectType(item));
                    value = "";
                }
            }
            structurePrototypes.Add(type, structure);
        }
    }

    public Tile GetTile(int x, int y)
    {
        if (x >= height || x < 0 || y >= width || y < 0) { return null; }
        return tiles[x, y];
    }

    void OnTileChanged(Tile tile)
    {
        if (tileChangedEvent == null) return;
        tileChangedEvent(tile);
        InvalidateTileGraph();
    }

    void OnItemChanged(Item item)
    {
        if (itemChangedEvent == null) return;
        itemChangedEvent(item);
    }

    void OnStructureChanged(Structure structure)
    {
        if (structureChangedEvent == null) return;
        structureChangedEvent(structure);
        InvalidateTileGraph();
    }

    public void RegisterTileChanged(Action<Tile> callback)
    {
        tileChangedEvent += callback;
    }

    public void UnregisterTileChanged(Action<Tile> callback)
    {
        tileChangedEvent -= callback;
    }

    public void RegisterStructureChanged(Action<Structure> callback)
    {
        structureChangedEvent += callback;
    }

    public void RegisterCharacterCreated(Action<Character> callback)
    {
        if (characterCreated == null)
        {
            characterCreated += callback;

            foreach (Character c in queuedCharactersToCreate)
            {
                characterCreated(c);
            }

            queuedCharactersToCreate = new List<Character>();
        }

        else
        {
            characterCreated += callback;
        }
    }

    public void RegisterItemChanged(Action<Item> callback)
    {
        if (itemChangedEvent == null)
        {
            itemChangedEvent += callback;

            foreach (Item i in queuedItemsToCreate)
            {
                itemChangedEvent(i);
                i.SetItemChanged(itemChangedEvent);
            }

            queuedItemsToCreate = new List<Item>();
        }

        else
        {
            itemChangedEvent += callback;
        }
    }

    public bool IsStructurePlacementValid(string objectType, Tile tile, Facing buildDirection, int width, int height)
    {
        return structurePrototypes[objectType].IsValidPosition(tile, buildDirection, width, height);
    }

    public Structure PlaceStructure(string type, Tile tile, int width, int height, Facing buildDirection, bool constructed = false, bool addOptionalParameters = true)
    {

        if (!structurePrototypes.ContainsKey(type))
        {
            Debug.LogError("Prototype array does not contain prototype for key " + type);
            return null;
        }

        bool valid = tile.structure.PlaceStructure(structurePrototypes[type], width, height, buildDirection, constructed, addOptionalParameters);
        if (!valid)
        {
            Debug.LogError("Placement was invalid " + type);
            // Failed to place object, probably because something was already there.
            return null;
        }

        structures.Add(tile.structure);

        if (structureChangedEvent != null)
        {
            structureChangedEvent(tile.structure);
        }

        return tile.structure;
    }

    #region Rooms
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }

    public Room GetOutSideRoom()
    {
        return rooms[0];
    }

    public void AddRoom(Room room)
    {
        this.rooms.Add(room);
    }

    public void DestroyRoom(Room room)
    {
        if (room == null) return;

        if (room == GetOutSideRoom())
        {
            Debug.LogError("Tried to delete outside room.");
            return;
        }

        rooms.Remove(room);
        room.UnassignAllTiles();
    }
    #endregion

    #region Saving/Loading
    
    public World(){}
    
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
		width = int.Parse( reader.GetAttribute("width"));
		height = int.Parse( reader.GetAttribute("height"));

		SetupWorld(width, height, false);

		while(reader.Read()) {
			switch(reader.Name) {
				case "Tiles":
					ReadXml_Tiles(reader);
					break;
				case "Structures":
					ReadXml_Structures(reader);
					break;
				case "Characters":
					ReadXml_Characters(reader);
					break;
                case "Jobs":
                    ReadXml_Jobs(reader);
                    break;
			}
		}

	}

	void ReadXml_Tiles(XmlReader reader) {
		// We are in the "Tiles" element, so read elements until
		// we run out of "Tile" nodes.
		while(reader.Read()) {
			if(reader.Name != "Tile")
				return;	// We are out of tiles.

			int x = int.Parse(reader.GetAttribute("x"));
			int y = int.Parse(reader.GetAttribute("y"));

            if (reader.GetAttribute("itemType") != null && reader.GetAttribute("amount") != null)
            {
                tiles[x, y].item = new Item(tiles[x, y], reader.GetAttribute("itemType"), int.Parse(reader.GetAttribute("amount")));
                queuedItemsToCreate.Add(tiles[x, y].item);
            }

			tiles[x,y].ReadXml(reader);
		}

	}

	void ReadXml_Structures(XmlReader reader) {

        Structure structure = null;

		while(reader.Read()) {
			if(reader.Name != "Structure")
				return;

			int x = int.Parse(reader.GetAttribute("x"));
			int y = int.Parse(reader.GetAttribute("y"));

			structure = PlaceStructure(reader.GetAttribute("type"), tiles[x,y], int.Parse(reader.GetAttribute("width")), int.Parse(reader.GetAttribute("height")), (Facing) Enum.Parse(typeof(Facing), reader.GetAttribute("FacingDirection")), bool.Parse(reader.GetAttribute("IsConstructed")), false);
			structure.ReadXml(reader);

            if (structure.optionalParameters.Count > 0)
            {
                this.updatingStructures.Add(structure);
            }

		}

        Room.FloodFillRoom(structure);
	}

	void ReadXml_Characters(XmlReader reader) {

		while(reader.Read()) {
			if(reader.Name != "Character")
				return;

			int x = int.Parse(reader.GetAttribute("x"));
			int y = int.Parse(reader.GetAttribute("y"));
			Character c = CreateCharacter(tiles[x,y]);

			c.ReadXml(reader);
		}
	}

    void ReadXml_Jobs(XmlReader reader) {

		while(reader.Read()) {
			if(reader.Name != "Job")
				return;

            List<buildingRequirement> items = new List<buildingRequirement>();

            int x = int.Parse(reader.GetAttribute("x"));
			int y = int.Parse(reader.GetAttribute("y"));
            Tile tile = tiles[x, y];

            // Loads saved building materials
            if (reader.GetAttribute("items") != null && reader.GetAttribute("amounts") != null)
            {
                string[] itemsString = reader.GetAttribute("items").Split('/');
                string[] amounts = reader.GetAttribute("amounts").Split('/');

                for (int i = 0; i < itemsString.Length; i++)
                {
                    if (itemsString[i] == "") continue;

                    Debug.Log(itemsString[i] + " " + int.Parse(amounts[i]));
                    items.Add(new buildingRequirement(itemsString[i], int.Parse(amounts[i])));
                }
            }
            
            // Creates new job based on category.
            switch (Enum.Parse(typeof(SaveableJob), reader.GetAttribute("jobType")))
            {
                case SaveableJob.Construction:
                    tile.room.jobQueue.Enqueue(new Job(tile, (theJob) => tile.structure.CompleteStructure(), JobType.Construction, items, float.Parse(reader.GetAttribute("jobTime"))));
                    break;
                case SaveableJob.Demolition:
                    tile.room.jobQueue.Enqueue(new Job(tile, (theJob) => tile.structure.RemoveStructure(), JobType.Demolition, null, float.Parse(reader.GetAttribute("jobTime"))));
                    break;
            }
		}
	}

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("width", width.ToString());
		writer.WriteAttributeString("height", height.ToString());

		writer.WriteStartElement("Tiles");
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				writer.WriteStartElement("Tile");
				tiles[x,y].WriteXml(writer);

                if (tiles[x, y].item != null && tiles[x, y].item.CurrentStackAmount > 0)
                {
                    tiles[x, y].item.WriteXml(writer);
                }

				writer.WriteEndElement();
			}
		}
		writer.WriteEndElement();

		writer.WriteStartElement("Structures");
		foreach(Structure structure in structures) {
			writer.WriteStartElement("Structure");
			structure.WriteXml(writer);
			writer.WriteEndElement();

		}
		writer.WriteEndElement();

		writer.WriteStartElement("Characters");
		foreach(Character c in characters) {
			writer.WriteStartElement("Character");
			c.WriteXml(writer);
            c.inventory.WriteXml(writer);
			writer.WriteEndElement();

		}
		writer.WriteEndElement();

        writer.WriteStartElement("Jobs");

        // Save each job a character is working on if its saveable.
        foreach (Character c in characters)
        {
            Job job = c.GetActiveJob();
            if (job != null && Enum.IsDefined(typeof(SaveableJob), job.jobType.ToString()))
            {
                writer.WriteStartElement("Job");
                job.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

		foreach(Room room in rooms) {

            if (room.jobQueue.Count == 0) continue;

            foreach (Job job in room.jobQueue.ToArray())
            {
                if (Enum.IsDefined(typeof(SaveableJob), job.jobType.ToString()))
                {
                    writer.WriteStartElement("Job");
                    job.WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
		}
		writer.WriteEndElement();

    }
    #endregion
}

