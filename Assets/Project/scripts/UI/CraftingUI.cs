using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CraftingManager craftingManager;
    [SerializeField] private RectTransform craftPanel;
    [SerializeField] private Transform recipeListParent;
    [SerializeField] private GameObject recipeButtonPrefab;

    [Header("Animation")]
    [SerializeField] private float slideSpeed = 8f;

    private bool wasOpen;
    private float hiddenX;
    private float shownX;

    private void Awake()
    {
        // Panel slides from right
        shownX = craftPanel.anchoredPosition.x;
        hiddenX = shownX + 400f;

        var pos = craftPanel.anchoredPosition;
        pos.x = hiddenX;
        craftPanel.anchoredPosition = pos;
    }

    private void Update()
    {
        bool isOpen = craftingManager.IsCraftMenuOpen;

        // Refresh when opened
        if (isOpen && !wasOpen)
        {
            RefreshRecipes();
        }
        wasOpen = isOpen;

        // Animate
        float targetX = isOpen ? shownX : hiddenX;
        var pos = craftPanel.anchoredPosition;
        pos.x = Mathf.Lerp(pos.x, targetX,
            Time.deltaTime * slideSpeed);
        craftPanel.anchoredPosition = pos;
    }

    private void RefreshRecipes()
    {
        // Clear old buttons
        foreach (Transform child in recipeListParent)
        {
            Destroy(child.gameObject);
        }

        // Create button for each recipe
        var recipes = craftingManager.GetAllRecipes();

        foreach (var recipe in recipes)
        {
            var buttonObj = Instantiate(
                recipeButtonPrefab, recipeListParent);

            var text = buttonObj.GetComponentInChildren
                <TextMeshProUGUI>();

            bool canCraft = craftingManager.CanCraft(recipe);

            // Build ingredient text
            string ingredients = "";
            foreach (var ing in recipe.ingredients)
            {
                int have = 0;
                var inv = FindFirstObjectByType<PlayerInventory>();
                if (inv != null)
                    have = inv.CountItem(ing.item);

                string color = have >= ing.amount
                    ? "green" : "red";

                ingredients +=
                    $"<color={color}>{ing.item.itemName}" +
                    $" {have}/{ing.amount}</color>  ";
            }

            text.text = $"<b>{recipe.recipeName}</b>\n" +
                        $"<size=80%>{ingredients}</size>";

            // Button
            var button = buttonObj.GetComponent<Button>();
            var capturedRecipe = recipe;

            button.onClick.AddListener(() =>
            {
                if (craftingManager.TryCraft(capturedRecipe))
                {
                    RefreshRecipes();
                }
            });

            button.interactable = canCraft;

            // Visual feedback
            if (!canCraft)
            {
                var colors = button.colors;
                colors.disabledColor =
                    new Color(0.3f, 0.3f, 0.3f, 0.8f);
                button.colors = colors;
            }
        }
    }
}
