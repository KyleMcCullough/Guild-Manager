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
    MouseController mouseController;
    BuildController buildController;
    Tile lastPreviewTile = null;
    Facing lastPreviewDirection = Facing.None;
    public bool dragging {get; private set;} = false;
    Vector3 lastMousePosition;


    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        this.mouseController = FindObjectOfType<MouseController>();
        this.buildController = FindObjectOfType<BuildController>();
        lastMousePosition = new Vector3(0,0,0);
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

        if (structureType != "select" && !Data.structureData[structureType].linksToNeighbours)
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

        if (y + 1 > structure.parent.world.height) {
            tile = world.GetTile(x, y + 1);
            if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile || tilemap.HasTile(new Vector3Int(tile.x, tile.y, 0)))
            {
                spriteName += "N";
            }
        }

        if (x + 1 > structure.parent.world.width) {
            tile = world.GetTile(x + 1, y);
            if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile || tilemap.HasTile(new Vector3Int(tile.x, tile.y, 0)))
            {
                spriteName += "E";
            }
        }

        if (y - 1 >= 0) {
            tile = world.GetTile(x, y - 1);
            if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile || tilemap.HasTile(new Vector3Int(tile.x, tile.y, 0)))
            {
                spriteName += "S";
            }
        }

        if (x - 1 >= 0) {
            tile = world.GetTile(x - 1, y);
            if (tile != null && tile.structure != null && tile.structure.Type == structureType || previewTile != null && previewTile == tile || tilemap.HasTile(new Vector3Int(tile.x, tile.y, 0)))
            {
                spriteName += "W";
            }
        }

        if (!Data.ContainsSprite(spriteName))
        {
            Debug.LogWarning("GetSprite - no sprite with name " + spriteName + " is found.");
            return structure.Type.ToString() + "_";
        }

        return spriteName;
    }

    public void PlacePreviewSprite(string structureType, Facing direction, int width, int height)
    {
        if (dragging) return;

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
        t.sprite = Data.GetSprite(GetSpriteName(tile.structure, structureType, direction, null, width, height));

        tilemap.SetTile(new Vector3Int(xPos, yPos, 0), t);
        this.UpdateAdjacentSprites(tile);

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
            t.sprite = Data.GetSprite(this.GetSpriteName(n.structure, n.structure.Type, n.structure.facingDirection, tile));
            tilemap.SetTile(new Vector3Int(n.x, n.y, 0), t);

            // Sets opacity if it is not constructed yet.
            if (!n.structure.IsConstructed)
            {
                tilemap.SetTileFlags(new Vector3Int(n.x, n.y, 0), TileFlags.None);
                tilemap.SetColor(new Vector3Int(n.x, n.y, 0), new Color(1f, 1f, 1f, .5f));
            }
        }
    }

    public void UpdateDragging(int startX, int startY, int endX, int endY)
    {
        if (!dragging) dragging = true;

        Tile currentPreviewTile = world.GetTile(Mathf.FloorToInt(mouseController.mousePosition.x), Mathf.FloorToInt(mouseController.mousePosition.y));
        if (buildController.buildObject == ObjectType.Empty && buildController.buildType == 1 || lastPreviewTile == currentPreviewTile) return;
        lastPreviewTile = currentPreviewTile;
        
        tilemap.ClearAllTiles();

        // Creates empty tiles at locations, this is used for correct sprite generation.
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>());
            }
        }

        // Goes through all tiles and assigns sprites to them.
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {

                Tile tile = world.GetTile(x, y);

                UnityEngine.Tilemaps.Tile t = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();

                // Changes sprites to the selection type.
                switch (buildController.buildType)
                {
                    case 1:
                    {
                        t.sprite = Data.GetSprite(this.GetSpriteName(tile.structure, buildController.buildObject, tile.structure.facingDirection, tile));
                        break;
                    }

                    case 3:
                    {
                        t.sprite = Data.GetSprite(this.GetSpriteName(tile.structure, "select", tile.structure.facingDirection, tile));
                        break;
                    }
                }

                tilemap.SetTile(new Vector3Int(tile.x, tile.y, 0), t);
                
                if (buildController.buildType != 2)
                {
                    tilemap.SetTileFlags(new Vector3Int(tile.x, tile.y, 0), TileFlags.None);
                    tilemap.SetColor(new Vector3Int(tile.x, tile.y, 0), new Color(1f, 1f, 1f, .5f));
                }

                UpdateAdjacentSprites(tile);
            }
        }
    }

    public void EndDragging()
    {
        tilemap.ClearAllTiles();
        dragging = false;
    }
}
