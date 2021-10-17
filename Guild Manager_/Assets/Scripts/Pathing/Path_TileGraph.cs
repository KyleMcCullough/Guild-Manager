using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Constructs simple path-finding compatible graph of the world. Each tile
// is a node. Each walkable neighbour is linked via edge connection.
public class Path_TileGraph
{

    public Dictionary<Tile, Path_Node<Tile>> nodes;
    public Path_TileGraph(World world) {
        
        nodes = new Dictionary<Tile, Path_Node<Tile>>();

        // Loop through all tiles. Create a node for each tile.
        // Do we create nodes for non-floor tile? 'Water, some objects like scarecrows'.
        // Do we create nodes that cannot be walked on? 'Walls'? Maybe, to stop players from being stuck after building.
        for (int x = 0; x < world.width; x++) {
            for (int y = 0; y < world.height; y++) {
                
                Tile tile = world.GetTile(x, y);

                // if (tile.movementCost > 0) {
                Path_Node<Tile> node = new Path_Node<Tile>();
                node.data = tile;
                nodes.Add(tile, node);
                // }
            }
        }

        // Loop through all tiles again and create edges for neighbours.
        foreach (Tile tile in nodes.Keys)
        {

            Path_Node<Tile> node = nodes[tile];
            List<Path_Edge<Tile>> edges = new List<Path_Edge<Tile>>();

            // Get list of all neighbours for the tile. If the tile can be 
            // walked on, create an edge to it.
            Tile[] neighbours = tile.GetNeighbors(true);

            for (int i = 0; i < neighbours.Length; i++)
            {

                // The neighbour exists and can be walked on. If it is, create an edge.
                if (neighbours[i] != null && neighbours[i].movementCost > 0 || neighbours[i] != null && neighbours[i].structure.IsDoor()) {

                    // Make sure there is no diagonal clipping. If it is clipping, don't create an edge.
                    if (IsClippingCorner(tile, neighbours[i])) {
                        continue;
                    }

                    Path_Edge<Tile> edge = new Path_Edge<Tile>();
                    edge.cost = neighbours[i].movementCost;
                    edge.node = nodes[neighbours[i]];

                    // Adds edge to temp list.
                    edges.Add(edge);
                }
                node.edges = edges.ToArray();
            }
        }
    }

    bool IsClippingCorner(Tile curr, Tile neigh)
    {
        if (Mathf.Abs(curr.x - neigh.x) + Mathf.Abs(curr.y - neigh.y) == 2)
        {
            int dX = curr.x - neigh.x;
            int dY = curr.y - neigh.y;
            
            if (curr.world.GetTile(curr.x - dX, curr.y).movementCost == 0)
            {
                // East or west is unwalkable.
                return true;
            }

            if (curr.world.GetTile(curr.x, curr.y - dY).movementCost == 0)
            {
                // East or west is unwalkable.
                return true;
            }
        }
        return false;
    }
}
