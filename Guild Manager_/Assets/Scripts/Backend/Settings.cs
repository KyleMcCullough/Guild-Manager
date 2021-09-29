
public static class Settings
{
    public static int DayLength {get; private set;} = 40;
    public static int NightRatio {get; private set;} = 50;
    public static int StackLimit {get; private set;} = 100;

    public static StructureDataArray structures;

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

}