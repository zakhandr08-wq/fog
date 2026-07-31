using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "New Recipe",
    menuName = "Fog Network/Crafting Recipe")]
public class NetworkCraftingRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName;
    public string description;

    [Header("Ingredients")]
    public Ingredient[] ingredients;

    [Header("Result")]
    public string resultItemId;
    public string resultItemName;
    public int resultAmount = 1;

    [Header("World Object (optional)")]
    public bool placesWorldObject;
    public GameObject worldObjectPrefab;

    [System.Serializable]
    public struct Ingredient
    {
        public string itemId;
        public string itemName;
        public int amount;
    }
}