using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class ChestUIController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    //this script is attached to chest window prefabs

    [Header("UI Components")]
    public Button closeButton;
    public Button toggleChildButton;
    public GameObject toggleTargetChild;

    [Header("Linked Chest")]
    public Chest linkedChest;

    public RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 offset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);

        if (toggleChildButton != null)
            toggleChildButton.onClick.AddListener(ToggleChild);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Store the offset between mouse and panel position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            canvas.worldCamera,
            out offset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPoint))
        {
            rectTransform.anchoredPosition = localPoint - offset;
        }
    }

    private void CloseUI()
    {
        if (linkedChest != null)
        {
            linkedChest.Interact(); // Simply toggles this UI
        }
        else
        {
            gameObject.SetActive(false); // Fallback
        }
    }

    public void ToggleChild()
    {
        if (toggleTargetChild != null)
        {
            toggleTargetChild.SetActive(!toggleTargetChild.activeSelf);
        }
    }
}
