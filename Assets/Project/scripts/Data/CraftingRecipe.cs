using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Fog/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public string description;

    [Serializable]
    public struct Ingredient
    {
        public ItemData item;
        public int amount;
    }

    public Ingredient[] ingredients;
    public ItemData result;
    public int resultAmount = 1;

    // Some recipes don't give items
    // but place objects (like bonfire)
    public bool placesWorldObject;
    public GameObject worldObjectPrefab;
}
