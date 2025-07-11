using TMPro;
using UnityEngine;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance;

    public RectTransform panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI descriptionText;

    private Canvas canvas;

    void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();
        Hide();
    }

    public void Show(ItemSO data, DraggableItem item)
    {
        nameText.text = data.itemName;
        typeText.text = data.itemType.ToString(); // Example
        descriptionText.text = data.description;

        if (item.isRotated)
        {
            panel.position = item.topRight.position;
        }
        else
        {
            panel.position = item.bottomRight.position;
        }
        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        panel.gameObject.SetActive(false);
    }
}
