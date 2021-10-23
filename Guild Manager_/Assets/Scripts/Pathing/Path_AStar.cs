using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class Path_AStar
{

    Stack<Tile> path;
    public Path_AStar(World world, Tile tileStart, Tile tileEnd)
    {

        // Checks if the tilegraph is valid. If not, refresh it.
        if (world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world);
        }

        Dictionary<Tile, Path_Node<Tile>> nodes = world.tileGraph.nodes;

        if (!nodes.ContainsKey(tileStart))
        {
            Debug.LogError("Path_AStar: The starting tile is not in the list of nodes.");
            return;
        }

        if (!nodes.ContainsKey(tileEnd))
        {
            Debug.LogError("Path_AStar: The ending tile is not in the list of nodes.");
            return;
        }

        Path_Node<Tile> start = nodes[tileStart];
        Path_Node<Tile> goal = nodes[tileEnd];


        List<Path_Node<Tile>> ClosedSet = new List<Path_Node<Tile>>();
        // List<Path_Node<Tile>> OpenSet = new List<Path_Node<Tile>>();

        // OpenSet.Add(start);

        SimplePriorityQueue<Path_Node<Tile>> OpenSet = new SimplePriorityQueue<Path_Node<Tile>>();
        OpenSet.Enqueue(start, 0);

        Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From = new Dictionary<Path_Node<Tile>, Path_Node<Tile>>();

        Dictionary<Path_Node<Tile>, float> g_score = new Dictionary<Path_Node<Tile>, float>();

        foreach (Path_Node<Tile> n in nodes.Values)
        {
            g_score[n] = Mathf.Infinity;
        }

        g_score[start] = 0;

        Dictionary<Path_Node<Tile>, float> f_score = new Dictionary<Path_Node<Tile>, float>();

        foreach (Path_Node<Tile> n in nodes.Values)
        {
            f_score[n] = Mathf.Infinity;
        }

        f_score[start] = heuristic_cost_estimate(start, goal);

        while (OpenSet.Count > 0)
        {
            Path_Node<Tile> current = OpenSet.Dequeue();

            if (current == goal)
            {
                reconstruct_path(Came_From, current);
                return;
            }

            ClosedSet.Add(current);

            foreach (Path_Edge<Tile> edge_neighbour in current.edges)
            {
                Path_Node<Tile> neighbour = edge_neighbour.node;

                if (ClosedSet.Contains(neighbour))
                {
                    continue;
                }

                float movement_cost_to_neighbour = neighbour.data.movementCost * distance_detween(current, neighbour);

                float tentative_g_score = g_score[current] + movement_cost_to_neighbour;

                if (OpenSet.Contains(neighbour) && tentative_g_score >= g_score[neighbour]) continue;

                Came_From[neighbour] = current;
                g_score[neighbour] = tentative_g_score;
                f_score[neighbour] = g_score[neighbour] + heuristic_cost_estimate(neighbour, goal);

                if (!OpenSet.Contains(neighbour))
                {
                    OpenSet.Enqueue(neighbour, f_score[neighbour]);
                }
            }
        }



        // foreach (Tile t in goal.data.GetNeighbors())
        // {
        //     if (!t.structure.canCreateRooms)
        //     {
        //         reconstruct_path(Came_From, nodes[t]);
        //     }
        // }

        // If we have reached here, there is no path.
        return;
    }

    float distance_detween(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        // We can make assumptions since we are on a grid.

        if (Mathf.Abs((a.data.x - b.data.x) + (a.data.y - b.data.y)) == 1)
        {
            return 1f;
        }
        if (Mathf.Abs((a.data.x - b.data.x) * (a.data.y - b.data.y)) == 1) 
        {
            return 1.41421356f;
        }

        return Mathf.Sqrt(Mathf.Pow(a.data.x - b.data.x, 2) +
                    Mathf.Pow(a.data.y - b.data.y, 2));

    }

    float heuristic_cost_estimate(Path_Node<Tile> a, Path_Node<Tile> b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.data.x - b.data.x, 2) +
                          Mathf.Pow(a.data.y - b.data.y, 2));
    }

    void reconstruct_path(Dictionary<Path_Node<Tile>, Path_Node<Tile>> Came_From, Path_Node<Tile> current)
    {
        // Current is the goal at this point.
        Stack<Tile> total_path = new Stack<Tile>();
        total_path.Push(current.data);

        current = Came_From[current];
        while (Came_From.ContainsKey(current))
        {
            total_path.Push(current.data);
            current = Came_From[current];
        }

        // At this point, total_path is a stack with the correct path.
        path = total_path;
    }

    public Tile Dequeue()
    {
        return path.Pop();
    }

    public int Length()
    {
        if(path == null) return 0;

        return path.Count;
    }
}
