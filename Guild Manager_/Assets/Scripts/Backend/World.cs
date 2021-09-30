using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World
{
    // Day, month, year.
    int[] date;
    List<Character> characters;
    Tile[,] tiles;
    public int width {get;}
    public int height {get;}
    public Dictionary<string, Structure> structurePrototypes {get; protected set;}
    Action<Character> characterCreated;
    Action<Tile> tileChangedEvent;
    Action<Structure> structureChangedEvent;
    Action<Structure> structureCreatedEvent;
    Action<Item> itemChangedEvent;
    public Path_TileGraph tileGraph;
    public JobQueue jobQueue;
    public List<Tile> updatingTiles;
    public List<Structure> updatingStructures;
    float worldTime = 0;

    public World(int width, int height)
    {
        this.width = width;
        this.height = height;

        date = new int[] {1, 1, 1253};
        tiles = new Tile[width, height];
        jobQueue = new JobQueue();
        structurePrototypes = new Dictionary<string, Structure>();
        updatingStructures = new List<Structure>();
        updatingTiles = new List<Tile>();
        

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                tiles[x, y] = new Tile(x, y, this);
                tiles[x, y].RegisterTileChangedDelegate(OnTileChanged);
                tiles[x, y].Item.RegisterItemChanged(OnItemChanged);
                tiles[x, y].structure.RegisterObjectChangedDelegate(OnStructureChanged);
            }
        }
        Load.LoadStructureDetails();
        GeneratePrototypes();
        characters = new List<Character>();
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
        
        if (worldTime >= Settings.DayLength) {
            UpdateDate();
            worldTime = 0f;
        }
    }

    public bool IsDayTime()
    {
        return worldTime <= (Settings.DayLength * ( 1 - ((float) Settings.NightRatio / 100)));
    }

    public void UpdateDate() {
        date[0]++;
        
        if (date[0] >= 30) {
            date[0] = 0;
            date[1]++;
        }

        if (date[1] > 6) {
            date[1] = 0;
            date[2]++;
        }
    }

    public Character CreateCharacter(Tile tile)
    {
        Character c = new Character(tile);
        characters.Add(c);

        if (characterCreated != null)
            characterCreated(c);

        return c;
    }

    void GeneratePrototypes() 
    {
        foreach (ObjectType type in Enum.GetValues(typeof(ObjectType)))
        {
            StructureData data = Settings.GetStructureData(type.ToString());
            Structure structure = Structure.CreatePrototype(type, Settings.GetCategory(data.category).name, data.movementCost, data.width, data.height, data.linksToNeighbours);

            // Convert the string to the actual method, and load each function into the delegate
            foreach (string func in data.relatedFunctions)
            {
                if (func != null)
                {
                    structure.updateActions += (Action<Structure, float>) Delegate.CreateDelegate(typeof(Action<Structure, float>), StructureBehaviours.GetMethodInfo(func));
                }
            }

            if (data.relatedParameters.Length % 2 != 0)
            {
                Debug.LogError("There are parameters with unmatching values in " + type.ToString());
            }

            string value = "";
            foreach (string item in data.relatedParameters)
            {
                if (value == "")
                {
                    value = item;
                }

                else
                {
                    Debug.Log(value + " " + Settings.CastToCorrectType(item) + Settings.CastToCorrectType(item).GetType());
                    structure.optionalParameters.Add(value, Settings.CastToCorrectType(item));
                    value = "";
                }
            }

            // for (int i = 1; i < data.relatedParameters.Length / 2; i += 2)
            // {
            //     Debug.Log(data.relatedParameters[i] + " " + Settings.CastToCorrectType(data.relatedParameters[i + 1]) + Settings.CastToCorrectType(data.relatedParameters[i + 1]).GetType());
            //     structure.optionalParameters.Add(data.relatedParameters[i], Settings.CastToCorrectType(data.relatedParameters[i + 1]));
            //     Debug.Log("new loop");
            // }

            // foreach (string item in data.relatedParameters)
            // {
            //     object x = Settings.CastToCorrectType(item);
            //     Debug.Log(x.GetType() + " " + x);
            //     structure.optionalParameters.Add()
            // }
            
            structurePrototypes.Add(type.ToString(), structure);
        }
        
    }

    public Tile GetTile(int x, int y) {
        if (x >= height || x < 0 || y >= width || y < 0) {return null;}
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

    public void RegisterTileChanged(Action<Tile> callback)  {
        tileChangedEvent += callback;
    }

    public void UnregisterTileChanged(Action<Tile> callback)  {
        tileChangedEvent -= callback;
    }

    public void RegisterStructureChanged(Action<Structure> callback)  {
        structureChangedEvent += callback;
    }

    public void RegisterCharacterCreated(Action<Character> callback)  {
        characterCreated += callback;
    }

    public void RegisterItemChanged(Action<Item> callback)  {
        itemChangedEvent += callback;
    }

    public bool IsStructurePlacementValid(string objectType, Tile tile) {
        return structurePrototypes[objectType].IsValidPosition(tile);
    }

    public void PlaceStructure(string type, Tile tile) {
        
        //TODO: This assumes 1x1 objects with no rotations.
        if (!structurePrototypes.ContainsKey(type)) {
            Debug.LogError("Prototype array does not contain prototype for key " + type);
            return;
        }

        // InstalledObject obj = InstalledObject.PlaceInstance(installedObjectPrototypes[type], tile);
        bool valid = tile.structure.PlaceStructure(structurePrototypes[type]);
        if (!valid) {
            // Failed to place object, probably because something was already there.
            return;
        }

        if (structureChangedEvent != null) {
            structureChangedEvent(tile.structure);
            InvalidateTileGraph();
        }
    }

    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }
}

