using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Events;
using UnityEditor.EventSystems;

public class BuildController : MonoBehaviour
{



    string buildTile = "Dirt";
    public bool buildObjectsMode { get; private set; } = false;
    public string buildObject = ObjectType.Empty;
    public int buildType = 0;
    public int buildWidth = 1;
    public int buildHeight = 1;
    public Facing buildDirection = Facing.East;

    private void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotateStructure(0);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            RotateStructure(1);
        }
    }

    public void Build(Tile tile)
    {
        if (buildType == 0)
        {
            tile.Type = buildTile;
        }

        else if (buildType == 1)
        {
            // Continues to next tile if position is not valid for objects.
            if (!WorldController.Instance.World.IsStructurePlacementValid(buildObject, tile, buildDirection, buildWidth, buildHeight))
            {
                return;
            }

            // Creates a new job with a queued wall object temporarily reserving the tile until
            // the job is completed or cancelled.
            WorldController.Instance.World.PlaceStructure(buildObject, tile, this.buildWidth, this.buildHeight, this.buildDirection);
            WorldController.Instance.World.jobQueue.Enqueue(new Job(tile, (theJob) => tile.structure.CompleteStructure(), Data.GetBuildingRequirements(buildObject)));

        }
        else if (buildType == 2)
        {
            tile.item = new Item(tile, "Wood", 50);
        }

        else {
            if (tile.structure.Type == ObjectType.Empty && tile.structure.parentStructure == null)
            {
                return;
            }

            // if (tile.structure.parentStructure == null)
            // {
            WorldController.Instance.World.jobQueue.Enqueue(new Job(tile.world.GetTile(tile.x, tile.y), (theJob) => tile.structure.RemoveStructure(), null, 0.3f));
            // }

            // If the structure being deleted is part of a parent structure, set a delete for the parent.
            // else 
            // {
            //     Debug.Log("Going to parent structure to destroy.");
            //     Debug.Log(tile.structure.parentStructure.parent);
            //     Debug.Log(tile.structure.parentStructure);
            //     Debug.Log(WorldController.Instance.World.jobQueue);

            //     WorldController.Instance.World.jobQueue.Enqueue(new Job(tile.structure.parentStructure.parent, (theJob) => tile.structure.parentStructure.RemoveStructure(), null, 0.3f));
            // }
        }
    }

    // 0 is left, 1 is right.
    public void RotateStructure(int direction)
    {

        // if (direction != 1 || direction != 2) return;

        if (direction == 1)
        {
            switch (this.buildDirection)
            {
                case Facing.North:
                {
                    this.buildDirection = Facing.East;
                    break;
                }

                case Facing.East:
                {
                    this.buildDirection = Facing.South;
                    break;
                }

                case Facing.South:
                {
                    this.buildDirection = Facing.West;
                    break;
                }

                case Facing.West:
                {
                    this.buildDirection = Facing.North;
                    break;
                }
            }
        }

        if (direction == 0)
        {
            switch (this.buildDirection)
            {
                case Facing.North:
                {
                    this.buildDirection = Facing.West;
                    break;
                }

                case Facing.East:
                {
                    this.buildDirection = Facing.North;
                    break;
                }

                case Facing.South:
                {
                    this.buildDirection = Facing.East;
                    break;
                }

                case Facing.West:
                {
                    this.buildDirection = Facing.South;
                    break;
                }
            }
        }

        int temp = this.buildWidth;
        this.buildWidth = this.buildHeight;
        this.buildHeight = temp;
    }

    public void SetMode_BuildFloor()
    {
        buildType = 0;
        buildTile = "Grass";
    }

    public void SetMode_BuildVoid()
    {
        buildType = 0;
        buildTile = "Dirt";
    }

    public void SetMode_BuildWall(string objectType)
    {
        buildType = 1;
        buildObject = objectType;
        this.buildWidth = Data.structureData[objectType].width;
        this.buildHeight = Data.structureData[objectType].height;
    }

    public void SetMode_PutItem(string itemType)
    {
        buildType = 2;
    }

    public void SetMode_BuildTable()
    {
        buildType = 1;
        buildObject = "Table";
        this.buildWidth = Data.structureData["Table"].width;
        this.buildHeight = Data.structureData["Table"].height;
    }

    public void SetMode_BuildDoor()
    {
        buildType = 1;
        buildObject = "Wood_Door";
        this.buildWidth = Data.structureData["Wood_Door"].width;
        this.buildHeight = Data.structureData["Wood_Door"].height;
    }

    public void Deletion_Mode()
    {
        buildType = 3;
        buildObject = ObjectType.Empty;
    }
}
