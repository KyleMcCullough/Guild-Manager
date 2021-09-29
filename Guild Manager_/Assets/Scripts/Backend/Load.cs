using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class Load {
       
    public static void LoadStructureDetails()
    {
        string json = File.ReadAllText(Application.dataPath + "/Resources/images/Structures/details.json");
        Settings.structures = JsonUtility.FromJson<StructureDataArray>(json);
    }
}