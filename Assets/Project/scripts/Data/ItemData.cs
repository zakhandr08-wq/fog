using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Fog/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public string description;
    public Sprite icon;
    public bool isStackable;
    public int maxStack = 1;
    public bool isCraftingMaterial;
}