using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World
{
    float dayLength = 20f;
    float nightLength = 20f;

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
    public Path_TileGraph tileGraph;
    public JobQueue jobQueue;
    float worldTime = 0;

    public World(int width, int height)
    {
        this.width = width;
        this.height = height;

        date = new int[] {1, 1, 153};
        tiles = new Tile[width, height];
        jobQueue = new JobQueue();
        structurePrototypes = new Dictionary<string, Structure>();
        

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                tiles[x, y] = new Tile(x, y, this);
                tiles[x, y].RegisterTileChangedDelegate(OnTileChanged);
                tiles[x, y].structure.RegisterObjectChangedDelegate(OnStructureChanged);
            }
        }
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

        if (worldTime >= (dayLength + nightLength)) {
            UpdateDate();
            worldTime = 0f;
        }
    }

    public bool IsDayTime()
    {
        return worldTime <= dayLength;
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
            structurePrototypes.Add(type.ToString(), Structure.CreatePrototype(type, 0, 0, 0, true));
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

    public void RegisterStructureCreated(Action<Structure> callback)  {
        structureCreatedEvent += callback;
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

        if (structureCreatedEvent != null) {
            structureCreatedEvent(tile.structure);
            InvalidateTileGraph();
        }
    }

    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }
}

