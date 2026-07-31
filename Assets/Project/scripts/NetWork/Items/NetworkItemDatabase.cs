using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Простой синглтон-словарь предметов для тестов.
/// Позже заменим на ScriptableObject-ы.
/// </summary>
public class NetworkItemDatabase : MonoBehaviour
{
    public static NetworkItemDatabase Instance { get; private set; }

    [System.Serializable]
    public class ItemInfo
    {
        public string itemId;
        public string itemName;
        public GameObject worldPrefab; // для спавна в мире
    }

    [Header("Items")]
    [SerializeField] private ItemInfo[] items;

    private Dictionary<string, ItemInfo> itemsById;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        itemsById = new Dictionary<string, ItemInfo>();

        foreach (var item in items)
        {
            if (!itemsById.ContainsKey(item.itemId))
                itemsById[item.itemId] = item;
        }
    }

    public ItemInfo GetItem(string itemId)
    {
        if (itemsById == null) BuildDictionary();

        itemsById.TryGetValue(itemId, out var info);
        return info;
    }

    public bool HasItem(string itemId)
    {
        if (itemsById == null) BuildDictionary();
        return itemsById.ContainsKey(itemId);
    }
}