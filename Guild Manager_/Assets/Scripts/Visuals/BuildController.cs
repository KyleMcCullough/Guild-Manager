using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Events;
using UnityEditor.EventSystems;

public class BuildController : MonoBehaviour
{



    TileType buildTile = TileType.Empty;
    public bool buildObjectsMode { get; private set; } = false;
    String buildObject = "Empty";
    public int buildType = 0;

    public void Build(Tile tile)
    {
        if (buildType == 0)
        {
            tile.Type = buildTile;
        }

        else if (buildType == 1)
        {
            // Continues to next tile if position is not valid for objects.
            if (!WorldController.Instance.World.IsStructurePlacementValid(buildObject, tile))
            {
                return;
            }

            //TODO: Only handles single tile objects

            // Creates a new job with a queued wall object temporarily reserving the tile until
            // the job is completed or cancelled.
            WorldController.Instance.World.PlaceStructure(buildObject, tile);

            Job job = new Job(tile, (theJob) => tile.structure.CompleteStructure());
            WorldController.Instance.World.jobQueue.Enqueue(job);

        }
        else if (buildType == 2)
        {
            tile.Item.CreateNewStack(100, ItemType.Wood);
        }

        else {
            if (tile.structure.Type == "Empty")
            {
                return;
            }

            WorldController.Instance.World.jobQueue.Enqueue(new Job(tile.world.GetTile(tile.x, tile.y), (theJob) => tile.structure.RemoveStructure(), 0.3f));
            // WorldController.Instance.World.jobQueue.Enqueue(new Job(tile, (theJob) => {
            //     tile.structure = new Structure(tile);
            //     tile.structure.CompleteStructure();
            // }, 0.3f));
        }
    }

    public void SetMode_BuildFloor()
    {
        buildType = 0;
        buildTile = TileType.Dirt;
    }

    public void SetMode_BuildVoid()
    {
        buildType = 0;
        buildTile = TileType.Empty;
    }

    public void SetMode_BuildWall(string objectType)
    {
        buildType = 1;
        buildObject = objectType;
    }

    public void SetMode_PutItem(string itemType)
    {
        buildType = 2;
    }

    public void SetMode_BuildTable()
    {
        buildType = 1;
        buildObject = "Table";
    }

    public void SetMode_BuildDoor()
    {
        buildType = 1;
        buildObject = "Wood_Door";
    }

    public void Deletion_Mode()
    {
        buildType = 3;
        buildObject = "Empty";
    }
}
