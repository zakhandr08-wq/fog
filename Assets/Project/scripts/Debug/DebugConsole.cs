using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour
{
    private bool wasPlayerControllerEnabled;
    private bool wasPlayerInteractionEnabled;
    private bool wasEntityControllerEnabled;
    private bool wasEntityAbilitiesEnabled;
    private bool wasCraftingEnabled;
    private bool wasPlacementEnabled;
    [Header("UI References")]
    [SerializeField] private GameObject consolePanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private int maxLines = 50;
    [SerializeField] private int maxHistory = 20;

    // Public для других систем чтобы проверять
    public static bool IsOpen { get; private set; }

    private List<string> outputLines = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;

    private Dictionary<string, Command> commands =
        new Dictionary<string, Command>();

    private CursorLockMode previousCursorState;
    private bool previousCursorVisible;

    // Кешируем контроллеры для отключения
    private PlayerController playerController;
    private PlayerInteraction playerInteraction;
    private EntityController entityController;
    private EntityAbilitiesManager entityAbilities;
    private CraftingManager craftingManager;
    private PlacementSystem placementSystem;
    

    private class Command
    {
        public string name;
        public string description;
        public Action<string[]> action;
    }

    private void Start()
    {
        RegisterCommands();

        if (consolePanel != null)
            consolePanel.SetActive(false);

        CacheGameSystems();

        Print("=== Консоль отладки ===");
        Print("~ — открыть/закрыть");
        Print("'help' — список команд");
    }

    private void CacheGameSystems()
    {
        playerController =
            FindFirstObjectByType<PlayerController>();
        playerInteraction =
            FindFirstObjectByType<PlayerInteraction>();
        entityController =
            FindFirstObjectByType<EntityController>();
        entityAbilities =
            FindFirstObjectByType<EntityAbilitiesManager>();
        craftingManager =
            FindFirstObjectByType<CraftingManager>();
        placementSystem =
            FindFirstObjectByType<PlacementSystem>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (!IsOpen) return;

        // Enter — выполнить
        if (Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrWhiteSpace(inputField.text))
            {
                ExecuteCommand(inputField.text);
                inputField.text = "";
                inputField.ActivateInputField();
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
            NavigateHistory(1);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            NavigateHistory(-1);
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;

        if (consolePanel != null)
            consolePanel.SetActive(IsOpen);

        if (IsOpen)
        {
            previousCursorState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            DisableGameSystems();
        }
        else
        {
            Cursor.lockState = previousCursorState;
            Cursor.visible = previousCursorVisible;

            EnableGameSystems();
        }
    }

    private void DisableGameSystems()
    {
        // Просто ставим Time.timeScale = 0? 
        // Нет, это заморозит анимации.
        // Лучше отключить конкретные скрипты.

        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
            wasPlayerControllerEnabled = true;
        }

        if (playerInteraction != null && playerInteraction.enabled)
        {
            playerInteraction.enabled = false;
            wasPlayerInteractionEnabled = true;
        }

        if (entityController != null && entityController.enabled)
        {
            entityController.enabled = false;
            wasEntityControllerEnabled = true;
        }

        if (entityAbilities != null && entityAbilities.enabled)
        {
            entityAbilities.enabled = false;
            wasEntityAbilitiesEnabled = true;
        }

        if (craftingManager != null && craftingManager.enabled)
        {
            craftingManager.enabled = false;
            wasCraftingEnabled = true;
        }

        if (placementSystem != null && placementSystem.enabled)
        {
            placementSystem.enabled = false;
            wasPlacementEnabled = true;
        }
    }

    private void EnableGameSystems()
    {
        // Включаем ТОЛЬКО те что были включены до консоли
        if (playerController != null && wasPlayerControllerEnabled)
            playerController.enabled = true;

        if (playerInteraction != null && wasPlayerInteractionEnabled)
            playerInteraction.enabled = true;

        if (entityController != null && wasEntityControllerEnabled)
            entityController.enabled = true;

        if (entityAbilities != null && wasEntityAbilitiesEnabled)
            entityAbilities.enabled = true;

        if (craftingManager != null && wasCraftingEnabled)
            craftingManager.enabled = true;

        if (placementSystem != null && wasPlacementEnabled)
            placementSystem.enabled = true;

        // Сброс флагов
        wasPlayerControllerEnabled = false;
        wasPlayerInteractionEnabled = false;
        wasEntityControllerEnabled = false;
        wasEntityAbilitiesEnabled = false;
        wasCraftingEnabled = false;
        wasPlacementEnabled = false;
    }

    
    private void NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0) return;

        historyIndex += direction;
        historyIndex = Mathf.Clamp(
            historyIndex, -1, commandHistory.Count - 1);

        if (historyIndex >= 0)
        {
            inputField.text = commandHistory[
                commandHistory.Count - 1 - historyIndex];
            inputField.caretPosition = inputField.text.Length;
        }
        else
        {
            inputField.text = "";
        }
    }

    private void ExecuteCommand(string input)
    {
        input = input.Trim();

        commandHistory.Add(input);
        if (commandHistory.Count > maxHistory)
            commandHistory.RemoveAt(0);
        historyIndex = -1;

        Print($"> {input}");

        string[] parts = input.Split(' ');
        string commandName = parts[0].ToLower();

        string[] args = new string[parts.Length - 1];
        Array.Copy(parts, 1, args, 0, args.Length);

        if (commands.TryGetValue(commandName, out Command cmd))
        {
            try
            {
                cmd.action(args);
            }
            catch (Exception e)
            {
                Print($"<color=red>Ошибка: {e.Message}</color>");
            }
        }
        else
        {
            Print($"<color=red>Неизвестная команда</color>");
        }
    }

    public void Print(string message)
    {
        outputLines.Add(message);

        while (outputLines.Count > maxLines)
            outputLines.RemoveAt(0);

        if (outputText != null)
            outputText.text = string.Join("\n", outputLines);

        // Скролл вниз
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    private void Register(
        string name, string desc, Action<string[]> action)
    {
        commands[name.ToLower()] = new Command
        {
            name = name,
            description = desc,
            action = action
        };
    }

    // === Команды (оставь как было) ===
    private void RegisterCommands()
    {
        Register("help", "Показать список команд", (args) =>
        {
            Print("=== Команды ===");
            foreach (var cmd in commands.Values)
                Print($"<color=yellow>{cmd.name}</color> " +
                    $"— {cmd.description}");
        });

        Register("clear", "Очистить консоль", (args) =>
        {
            outputLines.Clear();
            if (outputText != null) outputText.text = "";
        });

        Register("fullheal", "Полный сброс здоровья", (args) =>
        {
            var h = FindFirstObjectByType<PlayerHealth>();
            if (h != null)
            {
                h.FullReset();
                Print("<color=green>Полностью восстановлен</color>");
            }
        });
        Register("heal", "Полное исцеление (только если не упал)",
        (args) =>
        {
            var h = FindFirstObjectByType<PlayerHealth>();
            if (h != null)
            {
                h.Heal();
                Print("<color=green>Исцелён</color>");
            }
        });


        Register("damage", "Ранение", (args) =>
        {
            var h = FindFirstObjectByType<PlayerHealth>();
            if (h != null) h.TakeDamage();
        });

        Register("down", "Сбить с ног", (args) =>
        {
            var h = FindFirstObjectByType<PlayerHealth>();
            if (h != null) h.Down();
        });

        Register("revive", "Поднять упавшего", (args) =>
        {
            var h = FindFirstObjectByType<PlayerHealth>();
            if (h != null)
            {
                h.Revive();
                Print("<color=green>Поднят</color>");
            }
        });

        Register("kill", "Убить", (args) =>
        {
            var h = FindFirstObjectByType<PlayerHealth>();
            if (h != null) h.Kill();
        });

        Register("sanity", "Установить рассудок", (args) =>
        {
            if (args.Length == 0) return;
            if (float.TryParse(args[0], out float value))
            {
                var s = FindFirstObjectByType<PlayerSanity>();
                if (s != null)
                {
                    float diff = value - s.CurrentSanity;
                    if (diff > 0) s.RestoreSanity(diff);
                    else s.DrainSanity(-diff);
                    Print($"Рассудок: {value}");
                }
            }
        });

        Register("give", "give <id> [count]", (args) =>
        {
            if (args.Length == 0)
            {
                Print("Использование: give <id> [count]");
                return;
            }

            string itemId = args[0].ToLower();
            int count = 1;
            if (args.Length > 1) int.TryParse(args[1], out count);

            var inv = FindFirstObjectByType<PlayerInventory>();
            if (inv == null) return;

            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            ItemData found = null;
            foreach (var item in allItems)
                if (item.itemId.ToLower() == itemId)
                {
                    found = item;
                    break;
                }

            if (found == null)
            {
                Print($"<color=red>Не найдено: {itemId}</color>");
                return;
            }

            if (inv.TryAddItem(found, count))
                Print($"<color=green>+{found.itemName} " +
                    $"x{count}</color>");
        });

        Register("tp", "tp <x> <y> <z>", (args) =>
        {
            if (args.Length < 3) return;
            if (float.TryParse(args[0], out float x)
                && float.TryParse(args[1], out float y)
                && float.TryParse(args[2], out float z))
            {
                var p = FindFirstObjectByType<PlayerController>();
                if (p != null)
                {
                    var cc = p.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    p.transform.position = new Vector3(x, y, z);
                    if (cc != null) cc.enabled = true;
                    Print($"Телепорт: {x} {y} {z}");
                }
            }
        });

        Register("pos", "Позиция игрока", (args) =>
        {
            var p = FindFirstObjectByType<PlayerController>();
            if (p != null)
            {
                Vector3 pos = p.transform.position;
                Print($"X:{pos.x:F1} Y:{pos.y:F1} Z:{pos.z:F1}");
            }
        });

        Register("timescale", "Скорость времени", (args) =>
        {
            if (args.Length == 0)
            {
                Print($"{Time.timeScale}");
                return;
            }
            if (float.TryParse(args[0], out float v))
            {
                Time.timeScale = Mathf.Clamp(v, 0.1f, 5f);
                Print($"Timescale: {Time.timeScale}");
            }
        });

        Register("mode", "Player/Entity", (args) =>
        {
            var m = FindFirstObjectByType<GameModeManager>();
            if (m != null)
            {
                m.ToggleMode();
                Print("Переключено");
            }
        });
    }
}