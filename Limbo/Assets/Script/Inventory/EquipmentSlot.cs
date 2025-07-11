using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class EquipmentSlot : MonoBehaviour
{

    public ItemType acceptedItemType;
    public DraggableItem currentItem;
    public List<GameObject> containedItems = new List<GameObject>();
    //public SlotManager sm;
    public void Start()
    {
        //sm = transform.parent.GetComponent<SlotManager>();
    }

    public void NewItemUpdate()
    {
        containedItems.Clear();
        if (currentItem == null)
        return;

        if (currentItem.isContainer)
        {
            GameObject inventoryPanel = currentItem.transform.Find("Inventory Panel").gameObject;

            foreach (Transform child in inventoryPanel.transform)
            {
                DraggableItem containedItem = child.GetComponent<DraggableItem>();
                if (containedItem != null)
                {
                    containedItems.Add(containedItem.gameObject);
                }
            }
        }
        
        
        /*
        if (sm != null)
        {
            //disable slot.NewItemUpdate in slotmanager if enabling this
            sm.GetAllEquippedItems();
        }   
        */
        
    }

    public void ClearItem()
    {
        currentItem = null;
    }
}
