using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileSpriteController : MonoBehaviour
{
    public Tilemap tilemap;
    public Dictionary<string, Sprite> tileSprites;

    World world
    {
        get { return WorldController.Instance.World; }
    }


    // Start is called before the first frame update
    void Start()
    {
        tileSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Tiles");

        foreach (Sprite sprite in sprites)
        {
            Sprite s = Sprite.Create(sprite.texture, new Rect(0, 0, sprite.texture.width, sprite.texture.height), new Vector2(0f, 0f), 32f);
            tileSprites[sprite.name] = s;
        }

        // Registers callback.
        world.RegisterTileChanged(OnTileChanged);
        RefreshAllTiles();
    }

    void OnTileChanged(Tile tile)
    {

        // TileBase mapTile = tilemap.GetTile(new Vector3Int(tile.x, tile.y, 0);

        // Trys to get the tile object from the tile data. Continues if it can sucessfully find and assign it.
        // if (tileGameObjects.TryGetValue(tile, out GameObject tileObject)) {

        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        AssignSprite(tile);
    }
    public void AssignSprite(Tile obj)
    {
        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();

        if (!Data.tileData[obj.Type].linksToNeighbours)
        {
            t.sprite = tileSprites[obj.Type + "_"];
            tilemap.SetTile(new Vector3Int(obj.x, obj.y, 0), t);
            return;
        }

        string spriteName = obj.Type;

        // Check for neighbours North, East, South, West.
        Tile tile;

        int x = obj.x;
        int y = obj.y;

        tile = world.GetTile(x, y + 1);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type)
        {
            spriteName += "N";
        }

        tile = world.GetTile(x + 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type)
        {
            spriteName += "E";
        }

        tile = world.GetTile(x, y - 1);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type)
        {
            spriteName += "S";
        }

        tile = world.GetTile(x - 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type)
        {
            spriteName += "W";
        }

        if (!tileSprites.ContainsKey(spriteName))
        {
            Debug.LogWarning("GetSprite - no sprite with name " + spriteName + " is found.");
            t.sprite = tileSprites[obj.Type.ToString()];
        }

        t.sprite = tileSprites[spriteName];
        tilemap.SetTile(new Vector3Int(obj.x, obj.y, 0), t);
    }

    public void RefreshAllTiles()
    {

        for (int x = 0; x < world.height; x++)
        {
            for (int y = 0; y < world.width; y++)
            {
                Tile tile = world.GetTile(x, y);
                UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                t.sprite = tileSprites[tile.Type];

                tilemap.SetTile(new Vector3Int(tile.x, tile.y, 0), t);
            }
        }
    }
}
