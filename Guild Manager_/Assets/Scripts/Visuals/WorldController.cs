using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering.Universal;

public class WorldController : MonoBehaviour
{
    int currentSpeed = 1;
    bool paused = false;

    [SerializeField]
    public Color dayColor = new Color(1.0f, 1.0f, 1.0f);
    [SerializeField]
    public Color nightColor = new Color(0.25f, 0.25f, 0.6f);
    [SerializeField]
    Light2D GlobalLight;
    public static WorldController Instance { get; protected set; }
    public World World { get; protected set; }

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
        UpdateSpeed();
        if (paused) return;

        World.Update(Time.deltaTime * currentSpeed);
        UpdateDayCycle();
    }

    // Changes lighting on day/night switch.
    void UpdateDayCycle()
    {
        Color timeColor;
        if (World.IsDayTime())
        {
            timeColor = dayColor;
        }

        else
        {
            timeColor = nightColor;
        }

        GlobalLight.color = Color.Lerp(GlobalLight.color, timeColor, 0.0003f);
    }

    public Tile GetTileAtCoordinate(Vector3 coord)
    {
        return World.GetTile(Mathf.FloorToInt(coord.x), Mathf.FloorToInt(coord.y));
    }

    void UpdateSpeed()
    {
        if (Input.GetKeyDown("space"))
        {
            if (paused)
            {
                paused = false;
            }

            else
            {
                paused = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentSpeed = 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentSpeed = 3;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentSpeed = 6;
        }
    }
}
