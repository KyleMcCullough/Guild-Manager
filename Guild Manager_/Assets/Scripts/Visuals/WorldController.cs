using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering.Universal;

using UnityEngine.SceneManagement;

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
    static bool loadingWorld = false;

    // Start is called before the first frame update
    void OnEnable()
    {
        if (loadingWorld)
        {
            loadingWorld = false;
            // paused = true;
            LoadSaveFile();
        }

        else
        {
            World = new World(100, 100);
        }

        Instance = this;
        Camera.main.transform.position = new Vector3(World.width / 2, World.height / 2, Camera.main.transform.position.z);
        
    }

    void Start()
    {
        RefreshStructures();
    }

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

    public void RegenerateWorld()
    {
        World.GenerateWorld(UnityEngine.Random.Range(1, 10000000).GetHashCode());
        StructureSpriteController structureSpriteController = GetComponent<StructureSpriteController>();

        structureSpriteController.RefreshAllStructures();
    }

    public void RefreshStructures()
    {
        StructureSpriteController structureSpriteController = GetComponent<StructureSpriteController>();
        structureSpriteController.RefreshAllStructures();
    }

	public void SaveWorld() {
		Debug.Log("SaveWorld button was clicked.");

		XmlSerializer serializer = new XmlSerializer(typeof(World));
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer, World);
		writer.Close();

        using (StreamWriter file = new StreamWriter(Application.persistentDataPath + "/gamedata.xml"))
        {
            file.WriteLine(writer.ToString());
        }
        Debug.Log(Application.persistentDataPath);
	}

    public void LoadWorld() {
		Debug.Log("LoadWorld button was clicked.");

		// Reload the scene to reset all data (and purge old references)
		loadingWorld = true;
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);

	}

	public void LoadSaveFile() {
		Debug.Log("CreateWorldFromSaveFile");
		// Create a world from our save file data.

		XmlSerializer serializer = new XmlSerializer( typeof(World) );
        
		TextReader reader = new StringReader(System.IO.File.ReadAllText(Application.persistentDataPath + "/gamedata.xml"));
        Debug.Log(System.IO.File.ReadAllText(Application.persistentDataPath + "/gamedata.xml"));

		World = (World)serializer.Deserialize(reader);
		reader.Close();
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
