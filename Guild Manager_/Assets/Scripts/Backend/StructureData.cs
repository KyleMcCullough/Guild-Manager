using System.Runtime.Serialization;

[System.Serializable]
public class StructureData
{
    public string name;
    public int movementCost, width, height, category;
    public bool linksToNeighbours, canCreateRooms;
    public string[] relatedFunctions;
    // public string[] parameters;
    public string[] relatedParameters;
    // public RelatedParameter[] relatedParameters;
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