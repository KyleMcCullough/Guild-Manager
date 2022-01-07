using System.Security.Authentication;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PreviewSpriteController : MonoBehaviour
{
    public Tilemap tilemap;
    Dictionary<string, Sprite> structureSprites;
    MouseController mouseController;
    Tile lastPreviewTile = null;
    Facing lastPreviewDirection = Facing.None;

    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        this.mouseController = FindObjectOfType<MouseController>();
        structureSprites = new Dictionary<string, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Structures");

        // Changes pixels per unit.
        foreach (Sprite sprite in sprites)
        {
            Sprite s = Sprite.Create(sprite.texture, new Rect(0, 0, sprite.texture.width, sprite.texture.height), new Vector2(0f, 0f), 32f);
            structureSprites[sprite.name] = s;
        }
    }

    public string GetSpriteName(Structure structure, string structureType, Facing direction, Tile previewTile = null, int width = 1, int height = 1)
    {
        if (previewTile == null && direction == Facing.South || direction == Facing.West)
        {
            int spriteX = structure.parent.x;
            int spriteY = structure.parent.y;

            if (width > 1)
                spriteX -= width;

            if (height > 1)
                spriteY -= height;

            structure = world.GetTile(spriteX, spriteY).structure;
        }


        if (!Data.structureData[structureType].linksToNeighbours)
        {
            if (Data.GetStructureData(structureType).rotates)
            {
                return structureType + "_" + direction.ToString();
            }

            else
            {
                return structureType + "_";
            }
        }

        Tile tile;
        string spriteName = structureType + "_";

        int x = structure.parent.x;
        int y = structure.parent.y;

        tile = world.GetTile(x, y + 1);
        if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile)
        {
            spriteName += "N";
        }

        tile = world.GetTile(x + 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile)
        {
            spriteName += "E";
        }

        tile = world.GetTile(x, y - 1);
        if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile)
        {
            spriteName += "S";
        }

        tile = world.GetTile(x - 1, y);
        if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile)
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

    public void PlacePreviewSprite(string structureType, Facing direction, int width, int height)
    {
        if (structureType == ObjectType.Empty)
        {
            tilemap.ClearAllTiles();
            return;
        }

        // Turn mouse position into coords
        int xPos = Mathf.FloorToInt(mouseController.mousePosition.x);
        int yPos = Mathf.FloorToInt(mouseController.mousePosition.y);

        // Adjust selected structure for placing south or west to properly align sprite to tiles.
        if (direction == Facing.South || direction == Facing.West)
        {
            if (width > 1)
                xPos -= (width - 1);

            if (height > 1)
                yPos -= (height - 1);
        }

        Tile tile = world.GetTile(xPos, yPos);

        if (lastPreviewTile == tile && lastPreviewDirection == direction || (xPos >= world.width || yPos >= world.height || xPos < 0 || yPos < 0)) return;

        // Clears previous preview as well as all changed existing structures.
        tilemap.ClearAllTiles();

        lastPreviewTile = tile;
        lastPreviewDirection = direction;

        UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        t.sprite = structureSprites[GetSpriteName(tile.structure, structureType, direction, null, width, height)];

        tilemap.SetTile(new Vector3Int(xPos, yPos, 0), t);
        this.UpdateAdjacentSprites(tile);

        //FIXME: Multi-tile structures are being properly placed and validated.
        // If structure placement is invalid give the sprite a red tint
        if (WorldController.Instance.World.IsStructurePlacementValid(structureType, tile, direction, width, height))
        {
            tilemap.SetTileFlags(new Vector3Int(xPos, yPos, 0), TileFlags.None);
            tilemap.SetColor(new Vector3Int(xPos, yPos, 0), new Color(1f, 1f, 1f, .5f));
        }

        else
        {
            tilemap.SetTileFlags(new Vector3Int(xPos, yPos, 0), TileFlags.None);
            tilemap.SetColor(new Vector3Int(xPos, yPos, 0), new Color(1f, .02f, .02f, .5f));
        }
    }

    void UpdateAdjacentSprites(Tile tile)
    {
        foreach (Tile n in tile.GetNeighbors())
        {
            if (n == null || n.structure == null || n.structure.Type == ObjectType.Empty || !n.structure.linksToNeighbour) continue;

            UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            t.sprite = structureSprites[this.GetSpriteName(n.structure, n.structure.Type, n.structure.facingDirection, tile)];
            tilemap.SetTile(new Vector3Int(n.x, n.y, 0), t);

            // Sets opacity if it is not constructed yet.
            if (!n.structure.IsConstructed)
            {
                tilemap.SetTileFlags(new Vector3Int(n.x, n.y, 0), TileFlags.None);
                tilemap.SetColor(new Vector3Int(n.x, n.y, 0), new Color(1f, 1f, 1f, .5f));
            }
        }
    }
}
