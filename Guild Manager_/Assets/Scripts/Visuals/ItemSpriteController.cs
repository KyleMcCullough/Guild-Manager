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

    World world
    {
        get { return WorldController.Instance.World; }
    }


    // Start is called before the first frame update
    void OnEnable()
    {
        world.RegisterItemChanged(OnItemChanged);
    }

    void OnItemChanged(Item item)
    {
        //TODO: Apply random rotation to item sprites to make it better visually.
        if (item.Type == ObjectType.Empty)
        {
            tilemap.SetTile(new Vector3Int(item.parent.x, item.parent.y, 0), null);
            return;
        }

        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        t.sprite = Data.GetSprite(item.Type);

        tilemap.SetTile(new Vector3Int(item.parent.x, item.parent.y, 0), t);
    }
}
