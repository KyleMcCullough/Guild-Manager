using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{

    Dictionary<Character, GameObject> characterGameObjects;
    Dictionary<string, Sprite> characterSprites;

    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        characterGameObjects = new Dictionary<Character, GameObject>();
        characterSprites = new Dictionary<string, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters");

        foreach (Sprite sprite in sprites)
        {
            characterSprites[sprite.name] = sprite;
        }

        world.RegisterCharacterCreated(OnCharacterCreated);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnCharacterCreated(Character character)
    {

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
        
        // Registers callback.
        character.RegisterOnChangedCallback(OnCharacterChanged);
        character.RegisterOnDeletedCallback(OnCharacterDeleted);
    }

    private void OnCharacterChanged(Character character)
    {

        // This means the character has been deleted. This will only be called on the same frame it is deleted.
        if (!characterGameObjects.ContainsKey(character))
        {
            return;
        }

        GameObject characterObject = characterGameObjects[character];
        characterObject.transform.position = new Vector3(character.x, character.y, 0);
    }

    private void OnCharacterDeleted(Character character)
    {
        Destroy(characterGameObjects[character]);
        characterGameObjects.Remove(character);
    }

}
