using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{

    Dictionary<Character, GameObject> characterGameObjects;
    Dictionary<string, Sprite> characterSprites;
    
    World world {
        get { return WorldController.Instance.World; } 
    }

    // Start is called before the first frame update
    void Start()
    {
        characterGameObjects = new Dictionary<Character, GameObject>();
        characterSprites = new Dictionary<string, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters");

        foreach (Sprite sprite in sprites)
        {
            characterSprites[sprite.name] = sprite;
        }

        world.RegisterCharacterCreated(OnCharacterCreated);
        // world.RegisterInstallObject(OnCharacterCreated);

        Character c = world.CreateCharacter(world.GetTile(world.width / 2, world.height / 2));

        // c.SetDestination(world.GetTileAt(world.width / 2 + 5, world.height / 2));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

public void OnCharacterCreated(Character character) {
        // Creates a game object linked to tile.
        //FIxME: Doesn't consider multi-tile objects or rotated objects.
        Debug.Log("Entered character creation.");

        GameObject obj = new GameObject();
        obj.name = "Character";
        obj.transform.position = new Vector3(character.x, character.y, 0);
        obj.transform.SetParent(this.transform, true);

        // Adds tile data and object to dictonary.
        characterGameObjects.Add(character, obj);

        // FIxME: Assumes object must be a wall. Currently hardcoded.
        SpriteRenderer sprite = obj.AddComponent<SpriteRenderer>();
        sprite.sprite = characterSprites["p1_front"];
        sprite.sortingOrder = 3;
        
        // Applies a transparency for not yet built objects.
        // Color color = sprite.color;
        // color.a = .3f;
        // sprite.color = color;

        // Registers callback.
        character.RegisterOnChangedCallback(OnCharacterChanged);
    }
    
    private void OnCharacterChanged(Character character) {

        // Ensures graphics are correct.
        if (!characterGameObjects.ContainsKey(character)) {
            Debug.LogError("OnCharacterChanged - Trying to change visuals not in our map.");
            return;
        }

        GameObject characterObject = characterGameObjects[character];

        characterObject.transform.position = new Vector3(character.x, character.y, 0);
    }

}
