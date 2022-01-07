using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Structure : IXmlSerializable
{
    #region variables
    public Tile parent;
    public StructureCategory structureCategory
    {
        get
        {
            return (StructureCategory) Data.structureData[Type].category;
        }
    }
    public Structure parentStructure = null;
    public Facing facingDirection = Facing.East;
    public List<Tile> overlappedStructureTiles;
    String type = ObjectType.Empty;

    public Dictionary<string, object> optionalParameters = new Dictionary<string, object>();
    public Action<Structure, float> updateActions = null;
    public Inventory inventory = null;

    public String Type
    {
        get { return type; }

        set
        {
            String previous = type;
            type = value;

            // Call callback to refresh tile visually.
            if (structureChangedEvent != null && previous != type)
            {
                structureChangedEvent(this);
            }
        }
    }

    bool isConstructed = false;
    public bool canCreateRooms = false;
    public bool IsConstructed
    {
        get { return isConstructed; }

        set
        {
            if (value)
            {
                isConstructed = true;
                if (structureChangedEvent != null)
                {
                    structureChangedEvent(this);
                }
            }

            else
            {
                isConstructed = false;
            }
        }
    }

    // Multiplyer, a value of 2 is twice as slow as 1. These can be combined with tile movementCost and other modifiers.
    // If the movementCost is 0, then it cannot be passed through.
    public float movementCost { get; protected set; }
    public int width = 1;
    public int height = 1;
    public bool linksToNeighbour { get; set; }

    // Func<Tile, bool> positionValidation;

    public Action<Structure> structureChangedEvent { get; protected set; }

    #endregion
    public void RegisterObjectChangedDelegate(Action<Structure> callback)
    {
        structureChangedEvent += callback;
    }

    public Structure() { }
    public Structure(Tile parent)
    {
        this.parent = parent;
    }

    public Structure(Tile parent, String Type)
    {
        this.Type = Type;
        this.parent = parent;
    }

    public void Update(float deltaTime)
    {
        if (this.updateActions != null)
        {
            this.updateActions(this, deltaTime);
        }
    }

    public StructureData GetTypeData(string type)
    {
        return Data.GetStructureData(type);
    }

    public bool PlaceStructure(Structure prototype, int width, int height, Facing buildDirection, bool constructed = true, bool addOptionalParameters = true)
    {

        // Checks if position is valid and the structure is not constructed. Constructed structures are only given when loaded and don't need validation.
        if (!this.IsValidPosition(this.parent, buildDirection, width, height) && !constructed)
        {
            return false;
        }

        this.width = width;
        this.height = height;
        this.facingDirection = buildDirection;

        if (this.width > 1 || this.height > 1)
        {
            this.ReserveRequiredTiles();
        }

        if (this.updateActions != null)
        {
            this.RemoveActions();
        }

        if (prototype.updateActions != null)
        {
            this.AssignActions((Action<Structure, float>)prototype.updateActions.Clone());
        }

        if (addOptionalParameters)
        {
            this.optionalParameters = prototype.optionalParameters;
        }

        if (Data.structureData[prototype.type].storageAmount > 0)
        {
            this.inventory = new Inventory(Data.structureData[prototype.type].storageAmount);
        }

        this.IsConstructed = constructed;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.movementCost = prototype.movementCost;
        this.canCreateRooms = prototype.canCreateRooms;
        this.Type = prototype.Type;

        if (this.linksToNeighbour)
        {
            foreach (Tile tile in this.parent.GetNeighbors())
            {
                if (tile != null && tile.structure.Type == this.Type)
                {
                    tile.structure.structureChangedEvent(tile.structure);
                }
            }
        }

        if (!this.parent.world.structures.Contains(this))
        {
            this.parent.world.structures.Add(this);
        }

        return true;
    }

    public void AssignActions(Action<Structure, float> newActions)
    {
        this.updateActions = newActions;
        this.parent.world.updatingStructures.Add(this);
    }

    public void RemoveActions()
    {
        this.updateActions = null;
        this.parent.world.updatingStructures.Remove(this);
    }

    // Reserves tiles for structures with a width or height more then 1.
    void ReserveRequiredTiles()
    {
        this.overlappedStructureTiles = new List<Tile>();

        int xPos = parent.x;
        int yPos = parent.y;

        switch (facingDirection)
        {

            case Facing.South:
            {
                yPos = parent.y - (this.height - 1);
                break;
            }

            case Facing.West:
            {
                xPos = parent.x - (this.width - 1);
                break;
            }
            
            default:
            {
                break;
            }
        }

        for (int x = 0; x < this.width; x++)
        {
            for (int y = 0; y < this.height; y++)
            {
                if (xPos + x == this.parent.x && yPos + y == this.parent.y)
                {
                    continue;
                }

                this.overlappedStructureTiles.Add(this.parent.world.GetTile(xPos + x, yPos + y));
                parent.world.GetTile(xPos + x, yPos + y).structure.parentStructure = this;
            }
        }
    }

    void ReleaseRequiredTiles()
    {
        if (this.overlappedStructureTiles == null || this.overlappedStructureTiles.Count == 0) return;

        foreach (Tile tile in this.overlappedStructureTiles)
        {
            tile.structure.parentStructure = null;
        }
        this.overlappedStructureTiles = null;
    }

    public void CompleteStructure(bool loading = false)
    {
        this.IsConstructed = true;

        if (this.canCreateRooms && !loading)
        {
            Thread UpdateRoomThread = new Thread(new ThreadStart(this.Thread_UpdateRooms_Creation));
            UpdateRoomThread.Start();
        }

        if (this.structureCategory == StructureCategory.QuestBoard)
        {
            Debug.Log("Added");
            this.parent.world.questManager.jobBoards.Add(this);
        }

        if (this.inventory != null)
        {
            this.parent.world.storageStructures.Add(this);
        }

        if (this.movementCost != 1)
        {
            parent.world.InvalidateTileGraph();
        }


        if (this.parent.room != null) this.parent.room.ResetUnreachableJobs();
    }

    public void RemoveStructure()
    {

        // If this tile is part of a parent structure, call the remove structure on it and return.
        if (this.parentStructure != null)
        {
            this.parentStructure.RemoveStructure();
            return;
        }

        this.parent.world.structures.Remove(this);
        Structure prototype = this.parent.world.structurePrototypes[ObjectType.Empty];

        if (this.updateActions != null)
        {
            this.RemoveActions();
        }

        if (this.structureCategory == StructureCategory.QuestBoard)
        {
            this.parent.world.questManager.jobBoards.Remove(this);
        }

        this.optionalParameters = prototype.optionalParameters;
        this.linksToNeighbour = prototype.linksToNeighbour;
        this.movementCost = prototype.movementCost;
        this.canCreateRooms = prototype.canCreateRooms;
        this.parent.world.GetOutSideRoom().AssignTile(this.parent);

        Thread UpdateRoomThread = new Thread(new ThreadStart(this.Thread_UpdateRooms_Deletion));
        UpdateRoomThread.Start();

        if (this.overlappedStructureTiles != null && this.overlappedStructureTiles.Count > 0)
        {
            this.ReleaseRequiredTiles();
        }

        // Drop items from the destruction if built
        if (this.IsConstructed)
        {
            foreach (buildingRequirement item in this.GetTypeData(this.Type).itemsToDropOnDestroy)
            {
                Debug.Log(item.material + item.amount);
                Item.CreateNewItem(this.parent, item.material, item.amount);
            }
        }

        this.Type = prototype.Type;

        foreach (Tile tile in this.parent.GetNeighbors())
        {
            tile.structure.structureChangedEvent(tile.structure);
        }

        if (this.parent.world.structures.Contains(this))
        {
            this.parent.world.structures.Remove(this);
        }

        if (this.inventory != null)
        {
            foreach (Item item in this.inventory.items)
            {
                Item.CreateNewItem(this.parent, item.type, item.CurrentStackAmount);
            }

            this.parent.world.storageStructures.Remove(this);
        }

        if (this.parent.room != null) this.parent.room.ResetUnreachableJobs();
    }

    static public Structure CreatePrototype(String type, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false, bool canCreateRooms = false)
    {

        Structure obj = new Structure();
        obj.Type = type;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;
        obj.linksToNeighbour = linksToNeighbour;
        obj.canCreateRooms = canCreateRooms;
        // obj.positionValidation = obj.IsValidPosition;

        return obj;
    }

    public bool IsValidPosition(Tile tile, Facing direction, int width, int height)
    {

        if (width > 1 || height > 1)
        {
            int xPos = tile.x;
            int yPos = tile.y;

            switch (direction)
            {

                case Facing.West:
                {
                    yPos = tile.y - (height - 1);
                    break;
                }

                case Facing.South:
                {
                    xPos = tile.x - (width - 1);
                    break;
                }
            }

            for (int x = xPos; x < (xPos + width); x++)
            {
                for (int y = yPos; y < (yPos + height); y++)
                {
                    Tile t = tile.world.GetTile(x, y);
                    if (!Data.CheckIfTileIsWalkable(tile.Type) || t.structure.type != ObjectType.Empty || t.structure.parentStructure != null)
                    {
                        return false;
                    }
                }
            }
        }

        else
        {
            if (!Data.CheckIfTileIsWalkable(tile.Type) || tile.structure.type != ObjectType.Empty || tile.structure.parentStructure != null) return false;
        }
        return true;
    }

    #region Door Methods
    public bool IsDoor()
    {
        return this.structureCategory == StructureCategory.Door;
    }

    public bool IsDoorOpen()
    {
        return (float)this.optionalParameters["openness"] == 1;
    }

    public void OpenDoor()
    {
        if (!IsDoor() || (bool)this.optionalParameters["doorIsOpening"] == true)
        {
            return;
        }

        this.optionalParameters["doorIsOpening"] = true;
    }

    public void CloseDoor()
    {
        if (!IsDoor())
        {
            return;
        }

        this.optionalParameters["doorIsOpening"] = false;
    }

    #endregion

    #region MultiThreading Methods

    void Thread_UpdateRooms_Creation()
    {
        Room.FloodFillRoom(this);
    }

    void Thread_UpdateRooms_Deletion()
    {
        Room.FloodFill_Remove(this);
        this.parent.world.CreateNewStructureAtTile(this.parent);
    }

    #endregion

    public void SwitchParentStructure(Structure structure)
    {
        if (structure.parentStructure == null) return;

        this.overlappedStructureTiles = structure.parentStructure.overlappedStructureTiles;
        this.overlappedStructureTiles.Add(structure.parent);

        structure.parentStructure = this;
        structure.overlappedStructureTiles = null;
    }

    #region Saving/Loading
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        type = reader.GetAttribute("type");
        width = int.Parse(reader.GetAttribute("width"));
        height = int.Parse(reader.GetAttribute("height"));

        if (reader.GetAttribute("keys") != null && reader.GetAttribute("values") != null)
        {

            optionalParameters = new Dictionary<string, object>();
            string[] keys = reader.GetAttribute("keys").Split('/');
            string[] values = reader.GetAttribute("values").Split('/');

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == "" || values[i] == "") continue;

                optionalParameters.Add(keys[i], Data.CastToCorrectType(values[i]));
            }
        }

        // Load structure parent
        if (reader.GetAttribute("xParent") != null && reader.GetAttribute("yParent") != null)
        {
            this.parentStructure = this.parent.world.GetTile(int.Parse(reader.GetAttribute("xParent")), int.Parse(reader.GetAttribute("yParent"))).structure;
        }

        // Load reserved structure tiles
        if (reader.GetAttribute("xOverlappedCoords") != null && reader.GetAttribute("yOverlappedCoords") != null)
        {
            string[] xTileCoords = reader.GetAttribute("xOverlappedCoords").Split('/');
            string[] yTileCoords = reader.GetAttribute("yOverlappedCoords").Split('/');

                for (int i = 0; i < xTileCoords.Length; i++)
                {
                    if (xTileCoords[i] == "" || xTileCoords[i] == "") continue;

                    overlappedStructureTiles.Add(this.parent.world.GetTile(int.Parse(xTileCoords[i]), int.Parse(yTileCoords[i])));
                }
        }

        if (reader.GetAttribute("items") != null && reader.GetAttribute("amounts") != null)
        {
            string[] itemsString = reader.GetAttribute("items").Split('/');
            string[] amounts = reader.GetAttribute("amounts").Split('/');

            for (int i = 0; i < itemsString.Length; i++)
            {
                if (itemsString[i] == "") continue;

                this.inventory.AddItem(itemsString[i], int.Parse(amounts[i]));
            }
        }

        if (this.IsConstructed) this.CompleteStructure(true);
    }

	public void WriteXml(XmlWriter writer) {
		writer.WriteAttributeString("x", parent.x.ToString());
		writer.WriteAttributeString("y", parent.y.ToString());
        writer.WriteAttributeString("width", width.ToString());
		writer.WriteAttributeString("height", height.ToString());
		writer.WriteAttributeString("type", Type);
        writer.WriteAttributeString("IsConstructed", IsConstructed.ToString());
        writer.WriteAttributeString("FacingDirection", facingDirection.ToString());

        // Save parent structure
        if (this.parentStructure != null)
        {
            writer.WriteAttributeString("xParent", parentStructure.parent.x.ToString());
		    writer.WriteAttributeString("yParent", parentStructure.parent.y.ToString());
        }

        // Save overlapped structures
        if (this.overlappedStructureTiles != null)
        {
            string xTileCoords = "";
            string yTileCoords = "";

            foreach (Tile t in this.overlappedStructureTiles)
            {
                xTileCoords += t.x + "/";
                yTileCoords += t.y + "/";
            }

            writer.WriteAttributeString("xOverlappedCoords", xTileCoords);
            writer.WriteAttributeString("yOverlappedCoords", yTileCoords);
        }

        // Save optional parameters
        if (this.optionalParameters != null)
        {
            string keys = "";
            string values = "";

            foreach (string item in optionalParameters.Keys)
            {
                keys += item + "/";
                values += optionalParameters[item] + "/";
            }

            writer.WriteAttributeString("keys", keys);
		    writer.WriteAttributeString("values", values);

        }

        if (this.inventory != null)
        {
            this.inventory.WriteXml(writer);
        }
	}
    #endregion
}