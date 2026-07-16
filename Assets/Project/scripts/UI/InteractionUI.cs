using UnityEngine;
using UnityEngine.UI;
using TMPro; // <--- Добавили библиотеку TMP

public class InteractionUI : MonoBehaviour
{
    // Заменили Text на TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image progressBar;
    [SerializeField] private PlayerInteraction playerInteraction;

    private void OnEnable()
    {
        playerInteraction.OnPromptChanged += ShowPrompt;
        playerInteraction.OnProgressChanged += UpdateProgress;
        playerInteraction.OnInteractionStopped += HideAll;
    }

    private void OnDisable()
    {
        playerInteraction.OnPromptChanged -= ShowPrompt;
        playerInteraction.OnProgressChanged -= UpdateProgress;
        playerInteraction.OnInteractionStopped -= HideAll;
    }

    private void Start()
    {
        HideAll();
    }

    private void ShowPrompt(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            promptText.gameObject.SetActive(false);
            return;
        }

        promptText.text = prompt;
        promptText.gameObject.SetActive(true);
    }

    private void UpdateProgress(float progress)
    {
        progressBar.gameObject.SetActive(true);
        progressBar.fillAmount = progress;
    }

    private void HideAll()
    {
        promptText.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
    }
}