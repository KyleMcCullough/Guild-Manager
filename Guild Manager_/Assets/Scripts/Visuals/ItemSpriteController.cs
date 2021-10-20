using System.Security.Authentication;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ItemSpriteController : MonoBehaviour
{
    public Tilemap tilemap;
    public Dictionary<string, Sprite> itemSprites;

    World world
    {
        get { return WorldController.Instance.World; }
    }


    // Start is called before the first frame update
    void Start()
    {

        itemSprites = new Dictionary<string, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Items");

        // Changes pixels per unit.
        foreach (Sprite sprite in sprites)
        {
            itemSprites[sprite.name] = Sprite.Create(sprite.texture, new Rect(0, 0, sprite.texture.width, sprite.texture.height), new Vector2(0f, 0f), 32f);
        }

        world.RegisterItemChanged(OnItemChanged);
    }

    void OnItemChanged(Item item)
    {

        // TileBase mapTile = tilemap.GetTile(new Vector3Int(tile.x, tile.y, 0);

        // Trys to get the tile object from the tile data. Continues if it can sucessfully find and assign it.

        //TODO: Apply random rotation to item sprites to make it better visually.
        if (item.Type == ObjectType.Empty)
        {
            tilemap.SetTile(new Vector3Int(item.parent.x, item.parent.y,0), null);
            return;
        }

        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        t.sprite = itemSprites[item.Type];

        tilemap.SetTile(new Vector3Int(item.parent.x, item.parent.y, 0), t);
    }
}
