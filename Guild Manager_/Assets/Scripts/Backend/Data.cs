using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class Data
{
    public static int DayLength {get; private set;} = 40;
    public static int NightRatio {get; private set;} = 50;
    public static int MaxInventory {get; private set;} = 5;
    public static Dictionary<string, StructureData> structureData = new Dictionary<string, StructureData>();
    public static Dictionary<string, TileData> tileData = new Dictionary<string, TileData>();
    public static Dictionary<string, ItemData> itemData = new Dictionary<string, ItemData>();
    public static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    public static List<string> structureTypes = new List<string>();
    public static List<Quest> questTemplates = new List<Quest>();
    public static List<string> tileTypes = new List<string>();
    public static List<string> itemTypes = new List<string>();
    static List<string> firstNames = new List<string>();
    static List<string> lastNames = new List<string>();

    // Jobs that require character directly over the destination tiles
    public static List<JobType> directDestinationJobs = new List<JobType> {
        JobType.Passing,
        JobType.Exiting,
        JobType.Hygiene
    };

    public static List<string> characterNeeds = new List<string> {
        "thirst",
        "hygiene",
        "sleep",
        "hunger"
    };

    public static int needsThreshold = 80;

    static StructureDataArray structures;
    static TileDataArray tiles;
    static ItemDataArray items;
    static QuestDataArray quests;

    public static void LoadData()
    {

        if (structureData.Count != 0 || tileData.Count != 0 || itemData.Count != 0) return;

        string json = File.ReadAllText(Application.dataPath + "/Resources/images/Structures/details.json");
        Data.structures = JsonUtility.FromJson<StructureDataArray>(json);

        foreach (StructureData data in Data.structures.structures)
        {
            structureData.Add(data.name, data);
            structureTypes.Add(data.name);
        }

        json = File.ReadAllText(Application.dataPath + "/Resources/images/Tiles/details.json");
        Data.tiles = JsonUtility.FromJson<TileDataArray>(json);

        foreach (TileData data in Data.tiles.tiles)
        {
            tileData.Add(data.name, data);
            tileTypes.Add(data.name);
        }

        json = File.ReadAllText(Application.dataPath + "/Resources/images/Items/details.json");
        Data.items = JsonUtility.FromJson<ItemDataArray>(json);

        foreach (ItemData data in Data.items.items)
        {
            itemData.Add(data.name, data);
            itemTypes.Add(data.name);
        }

        json = File.ReadAllText(Application.dataPath + "/Resources/quests.json");
        Data.quests = JsonUtility.FromJson<QuestDataArray>(json);

        foreach (QuestData data in Data.quests.quests)
        {
            questTemplates.Add(new Quest(data.title, data.description, data.minTimeToSolve, data.maxTimeToSolve, (AdventurerRank) data.requiredRank, data.id));
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/");

        // Changes pixels per unit.
        foreach (Sprite sprite in sprites)
        {
            Sprite s = sprite;
            if (!sprite.name.Contains("character"))
            {
                s = Sprite.Create(sprite.texture, new Rect(0, 0, sprite.texture.width, sprite.texture.height), new Vector2(0f, 0f), 32f);
            }
            Data.sprites[sprite.name] = s;
        }

        firstNames.AddRange(File.ReadAllText(Application.dataPath + "/Resources/FirstNames.txt").Split("\n"));
        lastNames.AddRange(File.ReadAllText(Application.dataPath + "/Resources/LastNames.txt").Split("\n"));
    }

    public static StructureData GetStructureData(string name)
    {
        foreach (StructureData item in structures.structures)
        {
            if (item.name == name)
            {
                return item;
            }
        }

        UnityEngine.Debug.LogError("No '" + name + "' has been found in the structure data array.");
        return new StructureData();
    }

    public static TileData GetTileData(string name)
    {
        foreach (TileData item in tiles.tiles)
        {
            if (item.name == name)
            {
                return item;
            }
        }

        UnityEngine.Debug.LogError("No '" + name + "' has been found in the structure data array.");
        return new TileData();
    }

    public static Category GetTileCategory(int id)
    {
        foreach (Category item in tiles.categories)
        {
            if (item.id == id)
            {
                return item;
            }
        }
        UnityEngine.Debug.LogError("No category with id of '" + id + "' has been found in the Category data array.");
        return null;
    }

    public static Category GetStructureCategory(int id)
    {
        foreach (Category item in structures.categories)
        {
            if (item.id == id)
            {
                return item;
            }
        }
        UnityEngine.Debug.LogError("No category with id of '" + id + "' has been found in the Category data array.");
        return null;
    }

    public static object CastToCorrectType(string value)
    {
        if (bool.TryParse(value, out bool b))
        {
            return b;
        }
        
        if (int.TryParse(value, out int i))
        {
            return i;
        }

        if (float.TryParse(value, out float f))
        {
            return f;
        }

        return value;
    }

    public static String GenerateCharacterName()
    {
        return firstNames[UnityEngine.Random.Range(0, firstNames.Count)] + " " + lastNames[UnityEngine.Random.Range(0, lastNames.Count)];
    }

    public static int GetStackLimit(string type)
    {
        if (itemTypes.Contains(type))
        {
            return itemData[type].maxStackSize;
        }

        Debug.LogError("GetStackLimit - An invalid type was given.");
        return -1;
    }

    public static bool CheckIfTileIsWalkable(string type)
    {
        if (tileTypes.Contains(type))
        {
            return tileData[type].walkable;
        }

        Debug.LogError("CheckIfTileIsWalkable - An invalid type '" + type + "' was given.");
        return false;
    }

    public static bool CheckIfTileIsFertile(string type)
    {
        if (tileTypes.Contains(type))
        {
            return tileData[type].fertility > 0;
        }

        return false;
    }

    public static bool CheckIfTileIsWater(string type)
    {
        if (tileTypes.Contains(type))
        {
            return tileData[type].category == 1;
        }

        return false;
    }

    public static int GetCategoryId(string name)
    {
        List<Category> idList = new List<Category>();
        idList.AddRange(tiles.categories);
        idList.AddRange(structures.categories);
        idList.AddRange(items.categories);

        foreach (Category c in idList)
        {
            if (c.name.ToLower() == name.ToLower())
            {
                return c.id;
            }
        }

        return -1;
    }

    public static List<buildingRequirement> GetbuildingRequirement(string type)
    {

        if (structureTypes.Contains(type))
        {
            return new List<buildingRequirement>(structureData[type].buildingRequirement);
        }

        Debug.LogError("GetbuildingRequirement - An invalid type was given.");
        return null;
    }

    public static Quest GetQuestTemplateById(int id)
    {
        foreach (Quest q in questTemplates)
        {
            if (q.id == id)
            {
                return q;
            }
        }

        return null;
    }

    public static Sprite GetSprite(string type)
    {
        if (sprites.ContainsKey(type)) return sprites[type];
        
        Debug.LogError("GetSprite - '" + type + "' sprite does not exist.");
        return null;
    }

    public static bool ContainsSprite(string type)
    {
        return sprites.ContainsKey(type);
    }

    public static void PrintDictionary<T>(List<T> list)
    {
        foreach (var line in list)
        {
            Debug.Log(line.ToString());
        }
    }
}