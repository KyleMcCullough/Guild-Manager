using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    [SerializeField]
    int NightTimePercent = 30;
    public static WorldController Instance {get; protected set;}
    public World World {get; protected set;}

    // Start is called before the first frame update
    void Awake()
    {
        World = new World(100, 100);
        Instance = this;
        Camera.main.transform.position = new Vector3(World.width / 2, World.height / 2, Camera.main.transform.position.z);
    }

    // Update is called once per frame
    void Update() 
    {
        // TODO: Add pause/unpause, speed controls.
        World.Update(Time.deltaTime);
    }

    public Tile GetTileAtCoordinate(Vector3 coord) {
        return World.GetTile(Mathf.FloorToInt(coord.x), Mathf.FloorToInt(coord.y));
    }
}
