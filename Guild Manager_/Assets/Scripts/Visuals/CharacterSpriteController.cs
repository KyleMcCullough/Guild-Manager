using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CharacterSpriteController : MonoBehaviour
{

    Dictionary<Character, GameObject> characterGameObjects;

    World world
    {
        get { return WorldController.Instance.World; }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        characterGameObjects = new Dictionary<Character, GameObject>();
        world.RegisterCharacterCreated(OnCharacterCreated);
    }

    public void OnCharacterCreated(Character character)
    {

        GameObject obj = new GameObject();
        obj.name = "Character-" + character.name + "-" + character.id;
        obj.transform.position = new Vector3(character.x, character.y, 0);
        obj.transform.SetParent(this.transform, true);

        // Adds tile data and object to dictonary.
        characterGameObjects.Add(character, obj);

        SpriteRenderer sprite = obj.AddComponent<SpriteRenderer>();
        sprite.sprite = Data.GetSprite("character_front");
        sprite.sortingOrder = 3;

        // Create a child gameobject for the name label.
        GameObject trailingName = new GameObject();
        trailingName.transform.SetParent(obj.transform, true);
        trailingName.name = "TextMesh";

        TextMeshPro textMesh = trailingName.AddComponent<TextMeshPro>();
        textMesh.text = character.name;
        textMesh.text = textMesh.text.Replace("\r", "");
        textMesh.alignment = TMPro.TextAlignmentOptions.Center;
        textMesh.autoSizeTextContainer = true;
        textMesh.fontSize = 48;

        //FIXME: Name goes under structure objects.

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

        if (!character.spawned) {
            characterObject.SetActive(false);
            characterObject.GetComponentInChildren<TextMeshPro>().gameObject.SetActive(false);
        }

        else if (!characterObject.activeInHierarchy) {
            characterObject.SetActive(true);
            characterObject.GetComponentInChildren<TextMeshPro>().gameObject.SetActive(true);
        }

        characterObject.transform.position = new Vector3(character.x, character.y, 0);
        TextMeshPro text = characterObject.GetComponentInChildren<TextMeshPro>();

        // Try relocating the name label. This may fail in the same frame the character leaves the scene.
        try
        {
            text.transform.position = new Vector3(character.x, character.y + .7f, -.25f);
            text.transform.localScale = new Vector3(.06f, .06f, 1);
        }
        catch (System.Exception)
        {}

    }

    private void OnCharacterDeleted(Character character)
    {
        Destroy(characterGameObjects[character]);
        characterGameObjects.Remove(character);
    }

}
