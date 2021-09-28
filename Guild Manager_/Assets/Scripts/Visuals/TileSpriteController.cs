using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileSpriteController : MonoBehaviour
{

    public Sprite dirtSprite;
    public Sprite emptySprite;
    public Tilemap tilemap;

    World world
    {
        get { return WorldController.Instance.World; }
    }


    // Start is called before the first frame update
    void Start()
    {

        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        t.sprite = dirtSprite;

        for (int x = 0; x < world.height; x++)
        {
            for (int y = 0; y < world.width; y++)
            {
                Tile tile = world.GetTile(x, y);
                tilemap.SetTile(new Vector3Int(tile.x, tile.y, 0), t);
            }
        }
        // Registers callback.
        world.RegisterTileChanged(OnTileChanged);
    }

    void OnTileChanged(Tile tile)
    {

        // TileBase mapTile = tilemap.GetTile(new Vector3Int(tile.x, tile.y, 0);

        // Trys to get the tile object from the tile data. Continues if it can sucessfully find and assign it.
        // if (tileGameObjects.TryGetValue(tile, out GameObject tileObject)) {

        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();

        UnityEngine.Debug.Log(tile.Type);
        if (tile.Type == TileType.Dirt)
        {
            t.sprite = dirtSprite;
            tilemap.SetTile(new Vector3Int(tile.x, tile.y, 0), t);
        }

        else if (tile.Type == TileType.Empty)
        {
            t.sprite = emptySprite;
            tilemap.SetTile(new Vector3Int(tile.x, tile.y, 0), t);
        }

        else
        {
            Debug.LogError("OnTileTypeChanged - unrecognized tile type.");
        }
    }
}
