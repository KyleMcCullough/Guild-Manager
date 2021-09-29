[System.Serializable]
public class StructureData
    {
        public string name;
        public int movementCost, width, height;
        public bool linksToNeighbours;
    }

[System.Serializable]
public class StructureDataArray
    {
        public StructureData[] structures;
    }