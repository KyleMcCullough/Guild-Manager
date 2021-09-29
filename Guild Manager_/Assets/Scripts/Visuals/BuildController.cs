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
    ObjectType buildObject = ObjectType.Empty;
    public int buildType = 0;


    // Start is called before the first frame updates
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Build(Tile tile)
    {
        if (buildType == 0)
        {
            tile.Type = buildTile;
        }

        if (buildType == 1)
        {
            // Continues to next tile if position is not valid for objects.
            if (!WorldController.Instance.World.IsStructurePlacementValid(buildObject.ToString(), tile))
            {
                return;
            }

            //TODO: Only handles single tile objects

            // Creates a new job with a queued wall object temporarily reserving the tile until
            // the job is completed or cancelled.

            StructureSpriteController sc = FindObjectOfType<StructureSpriteController>();

            WorldController.Instance.World.PlaceStructure(buildObject.ToString(), tile);

            Job job = new Job(tile, (theJob) => tile.structure.CompleteStructure());
            WorldController.Instance.World.jobQueue.Enqueue(job);

        }
        else
        {
            tile.Item.CreateNewStack(100, ItemType.Wood);
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
        Enum.TryParse<ObjectType>(objectType, out buildObject);
    }

    public void SetMode_PutItem(string itemType)
    {
        buildType = 2;
    }

    public void SetMode_BuildTable()
    {
        buildType = 1;
        buildObject = ObjectType.Table;
    }
}
