using System.Security.Authentication;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StructureSpriteController : MonoBehaviour
{
    public Tilemap tilemap;
    public Dictionary<string, Sprite> structureSprites;

    World world {
        get { return WorldController.Instance.World; } 
    }


    // Start is called before the first frame update
    void Start()
    {

        structureSprites = new Dictionary<string, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/InstalledObjects");

        // Changes pixels per unit.
        foreach (Sprite sprite in sprites)
        {
            Sprite s = Sprite.Create(sprite.texture, new Rect (0, 0, sprite.texture.width, sprite.texture.height), new Vector2(0f, 0f), 32f);
            structureSprites[sprite.name] = s;
        }

        world.RegisterStructureChanged(OnStructureChanged);
    }

    void OnStructureChanged(Structure structure)
    {
        
        // TileBase mapTile = tilemap.GetTile(new Vector3Int(tile.x, tile.y, 0);

        // Trys to get the tile object from the tile data. Continues if it can sucessfully find and assign it.
        // if (tileGameObjects.TryGetValue(tile, out GameObject tileObject)) {

        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();

        AssignSprite(structure);
        // t.sprite = GetSprite(structure);
        // tilemap.SetTile(new Vector3Int(structure.Parent.x, structure.Parent.y, 0), t);

        // else {
        //     Debug.LogError("OnTileTypeChanged - unrecognized tile type.");
        // }
    }

    public void AssignSprite(Structure obj) {
        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();

        if (!obj.linksToNeighbour) {
            t.sprite = structureSprites[obj.Type.ToString() + "_"];
            tilemap.SetTile(new Vector3Int(obj.Parent.x, obj.Parent.y, 0), t);
            return;
        }

        string spriteName = obj.Type + "_";

        // Check for neighbours North, East, South, West.
        Tile tile;

        int x = obj.Parent.x;
        int y = obj.Parent.y;

        tile = world.GetTile(x, y + 1);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type) {
            spriteName += "N";
        }

        tile = world.GetTile(x + 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type) {
            spriteName += "E";
        }

        tile = world.GetTile(x, y - 1);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type) {
            spriteName += "S";
        }

        tile = world.GetTile(x - 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == obj.Type) {
            spriteName += "W";
        }

        if (!structureSprites.ContainsKey(spriteName)) {
            Debug.LogWarning("GetSprite - no sprite with name " + spriteName + " is found.");
            t.sprite = structureSprites[obj.Type.ToString() + "_"];
        }

        t.sprite = structureSprites[spriteName];

        tilemap.SetTile(new Vector3Int(obj.Parent.x, obj.Parent.y, 0), t);

        // Sets opacity if it is not constructed yet.
        if (!obj.IsConstructed) {
            tilemap.SetTileFlags(new Vector3Int(obj.Parent.x, obj.Parent.y, 0), TileFlags.None);
            tilemap.SetColor(new Vector3Int(obj.Parent.x, obj.Parent.y, 0), new Color(1f,1f,1f,.5f));
        }
    }

    public void OnStructureComplete(Tile tile) {
        tile.structure.CompleteStructure();
    }

}
