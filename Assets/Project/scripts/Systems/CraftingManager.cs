using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [Header("Recipes")]
    [SerializeField] private CraftingRecipe[] allRecipes;

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlacementSystem placementSystem;

    private bool isCraftMenuOpen;
    private CraftingRecipe currentRecipe;

    public bool IsCraftMenuOpen => isCraftMenuOpen;

    private void Start()
    {
        if (playerInventory == null)
        {
            playerInventory =
                FindFirstObjectByType<PlayerInventory>();
        }

        if (placementSystem == null)
        {
            placementSystem =
                FindFirstObjectByType<PlacementSystem>();
        }

        // Subscribe to placement events
        if (placementSystem != null)
        {
            placementSystem.OnPlacementComplete += OnPlaced;
            placementSystem.OnPlacementCancelled += OnCancelled;
        }
    }

    private void OnDestroy()
    {
        if (placementSystem != null)
        {
            placementSystem.OnPlacementComplete -= OnPlaced;
            placementSystem.OnPlacementCancelled -= OnCancelled;
        }
    }

    private void Update()
    {
        if (placementSystem != null && placementSystem.IsPlacing)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleCraftMenu();
        }
    }

    private void ToggleCraftMenu()
    {
        isCraftMenuOpen = !isCraftMenuOpen;

        if (isCraftMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public CraftingRecipe[] GetAllRecipes()
    {
        return allRecipes;
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        if (playerInventory == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (!playerInventory.HasItem(
                ingredient.item, ingredient.amount))
                return false;
        }
        return true;
    }

    public bool TryCraft(CraftingRecipe recipe)
    {
        if (!CanCraft(recipe)) return false;

        // Remove ingredients
        foreach (var ingredient in recipe.ingredients)
        {
            playerInventory.RemoveItem(
                ingredient.item, ingredient.amount);
        }

        if (recipe.placesWorldObject
            && recipe.worldObjectPrefab != null)
        {
            currentRecipe = recipe;

            isCraftMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            placementSystem.StartPlacement(
                recipe.worldObjectPrefab);
        }
        else if (recipe.result != null)
        {
            // Добавить в инвентарь
            bool added = playerInventory.TryAddItem(
                recipe.result, recipe.resultAmount);

            if (added)
            {
                Debug.Log($"Скрафчено: {recipe.recipeName}");

                // Автоматически взять в руку
                AutoEquipItem(recipe.result);
            }

            // Закрыть меню
            isCraftMenuOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        return true;
    }

    private void AutoEquipItem(ItemData item)
    {
        // Найти в каком слоте лежит предмет
        for (int i = 0; i < playerInventory.MainInventory.Length; i++)
        {
            var slot = playerInventory.MainInventory[i];
            if (!slot.IsEmpty && slot.item == item)
            {
                // Взять в руку
                var handSystem =
                    FindFirstObjectByType<PlayerHandSystem>();
                if (handSystem != null)
                {
                    handSystem.SelectSlot(i);
                }
                return;
            }
        }
    }


    private void OnPlaced()
    {
        Debug.Log($"Построено: {currentRecipe?.recipeName}");
        currentRecipe = null;
    }

    private void OnCancelled()
    {
        if (currentRecipe == null) return;

        Debug.Log($"Отмена. Возврат ресурсов: " +
            $"{currentRecipe.recipeName}");

        // Return all ingredients
        foreach (var ingredient in currentRecipe.ingredients)
        {
            playerInventory.TryAddItem(
                ingredient.item, ingredient.amount);

            Debug.Log($"  Возвращено: " +
                $"{ingredient.item.itemName} " +
                $"x{ingredient.amount}");
        }

        currentRecipe = null;
    }
}
