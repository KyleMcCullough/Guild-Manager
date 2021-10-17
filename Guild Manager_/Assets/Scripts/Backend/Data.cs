using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public static class Data
{
    public static int DayLength {get; private set;} = 40;
    public static int NightRatio {get; private set;} = 50;
    public static int StackLimit {get; private set;} = 100;
    public static Dictionary<string, StructureData> structureData = new Dictionary<string, StructureData>(); 
    public static List<string> ObjectTypes = new List<string>();


    static StructureDataArray structures;

    public static void LoadData()
    {
        string json = File.ReadAllText(Application.dataPath + "/Resources/images/Structures/details.json");
        Data.structures = JsonUtility.FromJson<StructureDataArray>(json);

        foreach (StructureData data in Data.structures.structures)
        {
            structureData.Add(data.name, data);
            ObjectTypes.Add(data.name);
        }
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

    public static Category GetCategory(int id)
    {
        foreach (Category item in structures.categories)
        {
            if (item.id == id)
            {
                return item;
            }
        }
        UnityEngine.Debug.LogError("No id of '" + id + "' has been found in the Category data array.");
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
}