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
        if (buildObjectsMode)
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

            Job job = new Job(tile, (theJob) => sc.OnStructureComplete(tile));
            WorldController.Instance.World.jobQueue.Enqueue(job);

        }
        else
        {
            // Changes a tiles type.
            tile.Type = buildTile;
        }
    }

    public void SetMode_BuildFloor()
    {
        buildObjectsMode = false;
        buildTile = TileType.Dirt;
    }

    public void SetMode_BuildVoid()
    {
        buildObjectsMode = false;
        buildTile = TileType.Empty;
    }

    public void SetMode_BuildWall(string objectType)
    {
        buildObjectsMode = true;
        Enum.TryParse<ObjectType>(objectType, out buildObject);
    }
}
