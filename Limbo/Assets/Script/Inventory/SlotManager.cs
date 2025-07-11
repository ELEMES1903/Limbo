using UnityEngine;
using System.Collections.Generic;

public class SlotManager : MonoBehaviour
{
    public List<EquipmentSlot> hotbarSlots = new List<EquipmentSlot>();
    public List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();
    public List<GameObject> allItems = new List<GameObject>();
    public void Start()
    {
        hotbarSlots.Clear();
        equipmentSlots.Clear();

        foreach (Transform child in transform)
        {
            if (child.name == "hotbar slot")
            {
                EquipmentSlot containedItem = child.GetComponent<EquipmentSlot>();
                if (containedItem != null)
                {
                    hotbarSlots.Add(containedItem);
                }
            }
        }
        foreach (Transform child in transform)
        {
            if (child.name == "equipment slot")
            {
                EquipmentSlot containedItem = child.GetComponent<EquipmentSlot>();
                if (containedItem != null)
                {
                    equipmentSlots.Add(containedItem);
                }
            }
        }

    }
    public void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            GetAllEquippedItems();
        }
    }
    public void GetAllEquippedItems()
    {
        allItems.Clear();

        foreach (EquipmentSlot slot in equipmentSlots)
        {
            slot.NewItemUpdate(); // Ensure containedItems is up to date

            foreach (GameObject item in slot.containedItems)
            {
                if (item != null)
                {
                    allItems.Add(item);
                }
            }
        }

        for (int i = 0; i < allItems.Count && i < hotbarSlots.Count; i++)
        {
            if (allItems[i] != null && hotbarSlots[i] != null)
            {

                DraggableItem item = allItems[i].transform.GetComponent<DraggableItem>();
                item.isInSlot = true;
                allItems[i].transform.SetParent(hotbarSlots[i].transform, false);
                item.changeImage();
                allItems[i].transform.position = hotbarSlots[i].transform.position;
            }
        }

    }

}

