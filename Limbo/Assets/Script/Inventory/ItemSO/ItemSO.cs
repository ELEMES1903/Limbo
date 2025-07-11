using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    public ItemType itemType;
    [TextArea]
    public string description;

    [Header("Sprite Images")]
    public Sprite gridIcon;
    public Sprite slotIcon;
    
    [Header("UI Properties")]
    public int width = 1;
    public int height = 1;
    public bool rotatable;

    [Header("Container")]
    public bool isContainer;
    public int containerColumns;
    public int containerRows;
    

    // Add stats here
    public int value;
    public int durability;
    public float weight;
}
