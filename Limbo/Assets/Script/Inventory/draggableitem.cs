using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    private Canvas canvas;
    private RectTransform rectTransform;
    public ItemSO itemData;
    private CanvasGroup canvasGroup;
    public GridVisualizer currentGrid;

    //public GridVisualizer gridVisualizer;

    //---------------------------
    [Header("Tooltip UI")]
    private RectTransform itemRect;
    public RectTransform topRight;
    public RectTransform bottomRight;
    //---------------------------
    [Header("Revert References")]
    private Vector2 originalPosition;
    private Transform originalParent;
    public List<GridTile> occupiedTiles = new();
    public GridTile originTile;

    //---------------------------
    [Header("Item Stats")]
    public int itemWidth = 1; // in tiles
    public int itemHeight = 1; // in tiles
    //---------------------------
    [Header("Container")]
    private GameObject inventoryPanel;
    private bool isClosed = true;
    public bool isContainer;
    public List<DraggableItem> containedItems = new List<DraggableItem>();
    //---------------------------
    [Header("Rotation")]
    public bool isRotated = false;
    private bool isTempRotated = false;
    //---------------------------
    [Header("Image")]
    public Image itemImage;
    private Sprite originalSprite;
    //---------------------------
    [Header("Equipment Slot")]
    public EquipmentSlot currentSlot;
    public bool isInSlot;

    void Awake()
    {
        itemImage = transform.Find("itemImage")?.GetComponent<Image>();
        itemRect = transform.Find("itemImage")?.GetComponent<RectTransform>();

        topRight = itemImage.transform.Find("TopRight")?.GetComponent<RectTransform>();
        bottomRight = itemImage.transform.Find("BottomRight")?.GetComponent<RectTransform>();
        if (!isRotated)
        {
            topRight.anchoredPosition = new Vector3(itemRect.rect.width, 0, 0);
            bottomRight.anchoredPosition = new Vector3(itemRect.rect.width, -itemRect.rect.height, 0);
        }
        else
        {
            topRight.anchoredPosition = new Vector3(-itemRect.rect.width, -itemRect.rect.height, 0);
            bottomRight.anchoredPosition = new Vector3(-itemRect.rect.width, 0, 0);
        }
        
        if (isContainer)
        {
            inventoryPanel = transform.Find("Inventory Panel").gameObject;
            inventoryPanel.SetActive(false); // Start closed
        }

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        
        // Set dimensions based on item data
        itemWidth = itemData.width;
        itemHeight = itemData.height;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.position;
        originalParent = transform.parent;

        // Reparent to top-level canvas or container for dragging (assumes canvas variable set)
        transform.SetParent(canvas.transform, true);

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        //Update Sprite temporarily
        itemImage.sprite = itemData.gridIcon;
        itemImage.rectTransform.sizeDelta = new Vector2(60 * itemData.width, 60 * itemData.height);
        if(isTempRotated)
            itemImage.rectTransform.localEulerAngles = new Vector3(0, 0, -90);

        foreach (var tile in occupiedTiles)
                tile.currentItem = null;

        occupiedTiles.Clear();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
        if (droppedOn != null)
        {
            // Case 1: Drop on a GridTile
            if (droppedOn.TryGetComponent(out GridTile targetTile))
            {
                if (TrySnapToTile(targetTile, fromRotation: false))
                {
                    // Clear old slot
                    if (currentSlot != null)
                    {
                        currentSlot.ClearItem();
                        currentSlot = null;
                    }
                        if (isContainer && isInSlot)
                        {
                            foreach (DraggableItem containedItem in containedItems)
                            {
                                containedItem.isInSlot = false;
                                containedItem.changeImage();
                                containedItem.TrySnapToTile(containedItem.originTile, fromRotation: false); 
                            }
                        }
                    originTile = targetTile;
                    isInSlot = false;
                    changeImage();                  
                }
                //originalSprite = itemData.gridIcon;
                return;
            }

            // Case 2: Drop on an EquipmentSlot
            if (droppedOn.TryGetComponent(out EquipmentSlot equipmentSlot))
            {
                if (itemData.itemType == equipmentSlot.acceptedItemType && equipmentSlot.currentItem == null)
                {
                    // Clear old
                    foreach (var tile in occupiedTiles)
                        tile.currentItem = null;

                    //store items inside this item to a list for when moving this
                    if (isContainer)
                    {
                        containedItems.Clear();
                        foreach (Transform child in inventoryPanel.transform)
                        {
                            DraggableItem containedItem = child.GetComponent<DraggableItem>();
                            if (containedItem != null)
                            {
                                containedItems.Add(containedItem);
                            }
                        }
                        //close the grid
                        if (inventoryPanel.activeInHierarchy)
                        {
                            inventoryPanel.SetActive(false);
                            isClosed = true; // sync state 
                        }
                    }

                    // Update parent, position, current slot, sprite
                        transform.SetParent(equipmentSlot.transform, false);
                    rectTransform.position = equipmentSlot.transform.position;
                    currentSlot = equipmentSlot;
                    equipmentSlot.currentItem = this;

                    isInSlot = true;
                    changeImage();
                    equipmentSlot.NewItemUpdate();

                    
                    
                    return;
                }
            }
        }
        changeImage();

        // Fallback: revert to original position, parent
        rectTransform.position = originalPosition;
        transform.SetParent(originalParent, true);
        foreach (var tile in occupiedTiles)
            tile.currentItem = this;
    }

    public void changeImage()
    {
        int number;

        if (itemHeight == itemWidth && itemHeight == 1)
        {
            //1x1 items dont need image setting change
            return;
        }

        if (isInSlot) //if in a slot
        {
            //all slot is 1x1 so update all items to accomodate and fit to 1x1 space
            itemImage.rectTransform.sizeDelta = new Vector2(60, 60);
            itemImage.rectTransform.pivot = new Vector2(0f, 1f);
            itemImage.rectTransform.anchoredPosition = new Vector2(0, 0);
            itemImage.sprite = itemData.slotIcon;

            //temporarily unrotate to fit to slot tile
            if (isRotated)
            {
                itemImage.rectTransform.localEulerAngles = new Vector3(0, 0, 0);
                isTempRotated = true;
            }

            return;
        }
        else // if on grid
        {
            //changes all items need on grid
            itemImage.rectTransform.sizeDelta = new Vector2(60 * itemData.width, 60 * itemData.height);
            itemImage.sprite = itemData.gridIcon;

            //re-rotate to rework with grid tiles
            if (isRotated)
            {
                itemImage.rectTransform.localEulerAngles = new Vector3(0, 0, -90);
                isTempRotated = false;
            }

            //check if square and apply pivot update
            if (itemHeight == itemWidth) //2x2, 3x3 etc.
            {
                itemImage.rectTransform.pivot = new Vector2(0f, 1f);
                itemImage.rectTransform.anchoredPosition = new Vector2(0, 0);
                return;
            }

            //changes all non-square items on grid need
            itemImage.rectTransform.anchoredPosition = new Vector2(30, -30);
            
            //find the greatest number in terms of height or width in an item
            number = Mathf.Max(itemWidth, itemHeight);

            //update pivot according to item size
            if (number == 2) //1x2
                itemImage.rectTransform.pivot = new Vector2(0.25f, 0.5f);
            else if (number == 3)//1x3
                itemImage.rectTransform.pivot = new Vector2(0.16655f, 0.5f);
            else if (number == 4)//1x4
                itemImage.rectTransform.pivot = new Vector2(0.125f, 0.5f);
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // if right click + if item is rotatable + its not in an equipment slot, then rotate
        if (eventData.button == PointerEventData.InputButton.Right && itemData.rotatable && !isInSlot)
        {
            RotateItem();
        }
        //if double right click + is container, open/close
        if (eventData.clickCount == 2 && isContainer && !isInSlot)
        {
            isClosed = !isClosed;
            inventoryPanel.SetActive(!isClosed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        updateItemToolTip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Instance.Hide();
    }

    public void updateItemToolTip()
    {
        if (itemData != null)
        {
            ItemTooltipUI.Instance.Show(itemData, this);
        }
    }
    private void RotateItem()
    {
        isRotated = !isRotated;

        // Swap size
        (itemWidth, itemHeight) = (itemHeight, itemWidth);

        // Rotate
        Transform visual = transform.Find("itemImage");
        float targetZ = isRotated ? -90f : 0f;
        visual.localRotation = Quaternion.Euler(0, 0, targetZ);

        // Try to re-place at current tile
        if (occupiedTiles.Count > 0)
        {
            GridTile anchorTile = occupiedTiles[0];
            TrySnapToTile(anchorTile, fromRotation: true);
        }
        updateItemToolTip();
    }

    private GameObject GetTileUnderItem()
    {
        foreach (var tile in occupiedTiles)
        {
            return tile.gameObject;
        }
        return null;
    }


    public bool TrySnapToTile(GridTile targetTile, bool fromRotation)
    {
        int startRow = targetTile.row;
        int startCol = targetTile.column;

        GridVisualizer grid = targetTile.parentGrid;
        var candidateTiles = grid.GetTilesForItem(startRow, startCol, itemWidth, itemHeight);
        if (candidateTiles != null && grid.AreTilesFree(candidateTiles, this))

        {
            // Clear old tiles
            foreach (var tile in occupiedTiles)
                tile.currentItem = null;

            // Occupy new
            occupiedTiles = candidateTiles;
            foreach (var tile in occupiedTiles)
                tile.currentItem = this;

            // Snap to top-left tile
            RectTransform tileRect = targetTile.GetComponent<RectTransform>();
            rectTransform.position = tileRect.position; // world space alignment
            
            //if (fromRotation){updateItemToolTip();};
            currentGrid = grid;
            transform.SetParent(grid.transform);

            return true;
        }
        else
        {
            if (fromRotation)
            {
                Debug.Log("cannot rotate");
                // Undo rotation bool
                isRotated = !isRotated;

                // Revert size
                (itemWidth, itemHeight) = (itemHeight, itemWidth);

                // Revert visual rotation
                if (transform.childCount > 0)
                {
                    Transform visual = transform.GetChild(0);
                    visual.localRotation = Quaternion.Euler(0, 0, isRotated ? -90 : 0);
                }
            }
            else
            {
                Debug.Log("invalid placement");
                // Revert to original position
                rectTransform.position = originalPosition;
                transform.SetParent(originalParent, true);
                foreach (var tile in occupiedTiles)
                    tile.currentItem = this;
            }

            return false;
        }
    }
}
