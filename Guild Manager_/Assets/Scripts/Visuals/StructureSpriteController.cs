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

    World world
    {
        get { return WorldController.Instance.World; }
    }


    // Start is called before the first frame update
    void OnEnable()
    {

        structureSprites = new Dictionary<string, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Structures");

        // Changes pixels per unit.
        foreach (Sprite sprite in sprites)
        {
            Sprite s = Sprite.Create(sprite.texture, new Rect(0, 0, sprite.texture.width, sprite.texture.height), new Vector2(0f, 0f), 32f);
            structureSprites[sprite.name] = s;
        }

        world.RegisterStructureChanged(OnStructureChanged);
    }

    void OnStructureChanged(Structure structure)
    {
        // Tries to get the tile object from the tile data. Continues if it can sucessfully find and assign it.
        AssignSprite(structure);
    }

    public void AssignSprite(Structure obj)
    {
        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();

        string trailing = obj.facingDirection.ToString();
        int posX = obj.parent.x;
        int posY = obj.parent.y;

        if (obj.facingDirection == Facing.South)
        {
            posY -= (obj.height - 1);
        }

        else if (obj.facingDirection == Facing.West)
        {
            posX -= (obj.width - 1);
        }

        if (obj.Type == ObjectType.Empty)
        {
            tilemap.SetTile(new Vector3Int(obj.parent.x,obj.parent.y,0), null);
            return;
        }

        string spriteName = GetSpriteName(obj);

        t.sprite = structureSprites[spriteName];
        tilemap.SetTile(new Vector3Int(obj.parent.x, obj.parent.y, 0), t);

        // Sets opacity if it is not constructed yet.
        if (!obj.IsConstructed)
        {
            tilemap.SetTileFlags(new Vector3Int(obj.parent.x, obj.parent.y, 0), TileFlags.None);
            tilemap.SetColor(new Vector3Int(obj.parent.x, obj.parent.y, 0), new Color(1f, 1f, 1f, .5f));
        }
    }

    public string GetSpriteName(Structure structure)
    {
        if (!structure.linksToNeighbour)
        {
            if (Data.GetStructureData(structure.Type).rotates)
            {
                return structure.Type + "_" + structure.facingDirection.ToString();;
            }

            else
            {
                return structure.Type + "_";
            }
        }

        Tile tile;
        string spriteName = structure.Type + "_";

        int x = structure.parent.x;
        int y = structure.parent.y;

        tile = world.GetTile(x, y + 1);
        if (tile != null && tile.structure != null && tile.structure.Type == structure.Type)
        {
            spriteName += "N";
        }

        tile = world.GetTile(x + 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == structure.Type)
        {
            spriteName += "E";
        }

        tile = world.GetTile(x, y - 1);
        if (tile != null && tile.structure != null && tile.structure.Type == structure.Type)
        {
            spriteName += "S";
        }

        tile = world.GetTile(x - 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == structure.Type)
        {
            spriteName += "W";
        }

        if (!structureSprites.ContainsKey(spriteName))
        {
            Debug.LogWarning("GetSprite - no sprite with name " + spriteName + " is found.");
            return structure.Type.ToString() + "_";
        }

        return spriteName;
    }

    public void RefreshAllStructures()
    {

        for (int x = 0; x < world.height; x++)
        {
            for (int y = 0; y < world.width; y++)
            {
                Structure structure = world.GetTile(x, y).structure;

                if (structure == null || structure.Type == ObjectType.Empty || structure.Type == null)
                {
                    tilemap.SetTile(new Vector3Int(structure.parent.x, structure.parent.y, 0), null);
                    continue;
                }

                AssignSprite(structure);
            }
        }
    }
}
