using System.Runtime.Serialization;

[System.Serializable]
public class StructureData
{
    public string name;
    public int movementCost, width, height, category, placementMode;
    public bool linksToNeighbours, canCreateRooms, rotates;
    public string[] relatedFunctions;
    public string[] relatedParameters;
    public buildingRequirement[] buildingRequirement, itemsToDropOnDestroy;
}

[System.Serializable]
public class buildingRequirement
{
    public buildingRequirement(string material, int amount)
    {
        this.material = material;
        this.amount = amount;
    }

    public string material;
    public int amount;
}

[System.Serializable]
public class RelatedFunction
{
    public string name;
}

[System.Serializable]
public class RelatedParameter
{
    public string[] name;
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

[System.Serializable]
public class TileData
{
    public string name;
    public int movementCost, category, fertility;
    public bool linksToNeighbours, walkable;
}

[System.Serializable]
public class TileDataArray
{
    public TileData[] tiles;
    public Category[] categories;
}

[System.Serializable]
public class ItemData
{
    public string name;
    public int category, maxStackSize;
}

[System.Serializable]
public class ItemDataArray
{
    public ItemData[] items;
    public Category[] categories;
}