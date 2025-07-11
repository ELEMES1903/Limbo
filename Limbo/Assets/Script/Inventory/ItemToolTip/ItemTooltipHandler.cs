using UnityEngine;
using UnityEngine.EventSystems;

public class ItemTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

// CURRENTLY NOT BEING USED AT ALL, EVERYTHING HAS BEEN MOVED  
    private DraggableItem item;
    public RectTransform rectTransform;

    void Awake()
    {
        item = GetComponent<DraggableItem>();
        rectTransform = transform.GetChild(0).GetComponent<RectTransform>();

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
        if (item.itemData != null)
        {
            // Get world corners
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 bottomRight = corners[3]; // bottom-right in world space
            Vector3 topRight = corners[2]; // bottom-right in world space
            Vector3 corner;
            if(item.isRotated)
            {
                corner = topRight;
            } else {
                corner = bottomRight;
            }
            //ItemTooltipUI.Instance.Show(item.itemData, corner);
        }
    }
}
