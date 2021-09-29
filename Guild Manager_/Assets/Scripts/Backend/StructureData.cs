[System.Serializable]
public class StructureData
{
    public string name;
    public int movementCost, width, height, category;
    public bool linksToNeighbours;
}

[System.Serializable]
public class Category
{
    public string name;
    public int id;
}


[System.Serializable]
public class StructureDataArray
{
    public StructureData[] structures;
    public Category[] categories;
}