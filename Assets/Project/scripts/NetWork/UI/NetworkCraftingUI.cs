using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NetworkCraftingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject craftPanel;
    [SerializeField] private Transform recipeListParent;
    [SerializeField] private GameObject recipeButtonPrefab;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.C;

    private bool isOpen;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    private void Start()
    {
        if (craftPanel != null)
            craftPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"C pressed! isOpen: {isOpen}, name: {name}");
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        isOpen = !isOpen;

        Debug.Log($"ToggleMenu called! isOpen: {isOpen}, " +
            $"craftPanel: {(craftPanel != null ? "OK" : "NULL")}");

        if (craftPanel != null)
            craftPanel.SetActive(isOpen);

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshRecipes();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void RefreshRecipes()
    {
        // Удаляем старые кнопки
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();

        // Получаем менеджер и инвентарь
        var manager = NetworkCraftingManager.Instance;
        if (manager == null) return;

        var localPlayer = Mirror.NetworkClient.localPlayer;
        if (localPlayer == null) return;

        var inventory = localPlayer.GetComponent<NetworkPlayerInventory>();
        if (inventory == null) return;

        // Создаём кнопки для каждого рецепта
        var recipes = manager.GetAllRecipes();

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            var buttonObj = Instantiate(
                recipeButtonPrefab, recipeListParent);
            spawnedButtons.Add(buttonObj);

            var text = buttonObj
                .GetComponentInChildren<TextMeshProUGUI>();

            bool canCraft = manager.CanCraft(recipe, inventory);

            // Формируем текст рецепта
            string ingredientsText = "";
            foreach (var ing in recipe.ingredients)
            {
                int have = inventory.GetCount(ing.itemId);
                string color = have >= ing.amount
                    ? "green" : "red";
                ingredientsText +=
                    $"<color={color}>{ing.itemName} " +
                    $"{have}/{ing.amount}</color>  ";
            }

            text.text = $"<b>{recipe.recipeName}</b>\n" +
                        $"<size=80%>{ingredientsText}</size>";

            // Кнопка
            var button = buttonObj.GetComponent<Button>();
            button.interactable = canCraft;

            var capturedRecipe = recipe;
            button.onClick.AddListener(() =>
            {
                NetworkCraftingManager.RequestCraft(
                    capturedRecipe.recipeName);

                // Обновляем UI после клика
                Invoke(nameof(RefreshRecipes), 0.2f);
            });
        }
    }
}