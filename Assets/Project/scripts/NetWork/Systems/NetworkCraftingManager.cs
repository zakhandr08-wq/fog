using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class NetworkCraftingManager : NetworkBehaviour
{
    [Header("Recipes")]
    [SerializeField] private NetworkCraftingRecipe[] recipes;

    [Header("Placement (для world objects)")]
    [SerializeField] private float placeDistance = 5f;
    [SerializeField] private LayerMask groundLayer = ~0;

    // Singleton для лёгкого доступа из UI
    public static NetworkCraftingManager Instance { get; private set; }

    // Cache recipes by name
    private Dictionary<string, NetworkCraftingRecipe> recipesByName;

    private void Awake()
    {
        Instance = this;
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        recipesByName = new Dictionary<string, NetworkCraftingRecipe>();

        foreach (var recipe in recipes)
        {
            if (recipe != null && !recipesByName.ContainsKey(recipe.recipeName))
                recipesByName[recipe.recipeName] = recipe;
        }
    }

    public NetworkCraftingRecipe[] GetAllRecipes()
    {
        return recipes;
    }

    public NetworkCraftingRecipe GetRecipe(string recipeName)
    {
        if (recipesByName == null) BuildDictionary();

        recipesByName.TryGetValue(recipeName, out var recipe);
        return recipe;
    }

    /// <summary>
    /// Проверка на клиенте (для UI) — можно ли скрафтить
    /// </summary>
    public bool CanCraft(
        NetworkCraftingRecipe recipe,
        NetworkPlayerInventory inventory)
    {
        if (recipe == null || inventory == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            int have = inventory.GetCount(ingredient.itemId);
            if (have < ingredient.amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Клиент просит сервер выполнить крафт
    /// Вызывается через кнопку в UI
    /// </summary>
    public static void RequestCraft(string recipeName)
    {
        // Находим локального игрока
        var localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        var inventory =
            localPlayer.GetComponent<NetworkPlayerInventory>();

        if (inventory == null) return;

        // Отправляем команду через инвентарь игрока
        // (Command вызывается на NetworkBehaviour у игрока)
        inventory.CmdRequestCraft(recipeName);
    }

    /// <summary>
    /// Выполняется на сервере
    /// Проверяет ресурсы, удаляет, добавляет результат
    /// </summary>
    [Server]
    public bool ServerTryCraft(
        NetworkPlayerInventory inventory,
        string recipeName)
    {
        if (inventory == null) return false;

        var recipe = GetRecipe(recipeName);
        if (recipe == null)
        {
            Debug.LogWarning($"Server: recipe {recipeName} not found");
            return false;
        }

        // Проверка ресурсов
        foreach (var ingredient in recipe.ingredients)
        {
            if (!inventory.ServerHasItem(
                ingredient.itemId, ingredient.amount))
            {
                Debug.Log($"Server: {inventory.name} missing " +
                    $"{ingredient.itemId} x{ingredient.amount}");
                return false;
            }
        }

        // Удаляем ингредиенты
        foreach (var ingredient in recipe.ingredients)
        {
            inventory.ServerRemoveItem(
                ingredient.itemId, ingredient.amount);
        }

        // Добавляем результат или размещаем объект
        if (recipe.placesWorldObject
            && recipe.worldObjectPrefab != null)
        {
            SpawnWorldObject(recipe, inventory.transform);
        }
        else if (!string.IsNullOrEmpty(recipe.resultItemId))
        {
            inventory.ServerAddItem(
                recipe.resultItemId,
                recipe.resultItemName,
                recipe.resultAmount);
        }

        Debug.Log($"Server: {inventory.name} crafted {recipe.recipeName}");
        return true;
    }

    [Server]
    private void SpawnWorldObject(
        NetworkCraftingRecipe recipe,
        Transform playerTransform)
    {
        // Размещаем перед игроком
        Vector3 spawnPos = playerTransform.position
            + playerTransform.forward * placeDistance;

        // Опускаем на землю через Raycast
        Ray ray = new Ray(spawnPos + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, groundLayer))
        {
            spawnPos = hit.point;
        }

        GameObject obj = Instantiate(
            recipe.worldObjectPrefab, spawnPos, Quaternion.identity);

        NetworkServer.Spawn(obj);
    }
}