using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Chest : MonoBehaviour, IInteractable
{

    [Header("UI and Prefab")]
    public GameObject chestGridPrefab;
    private GameObject chestGridInstance;
    private ChestUIController uiController;

    [Header("Grid Size")]
    public int gridRows = 4;
    public int gridColumns = 4;

    [Header("Detection Settings")]
    public float detectionRadius = 2.5f;
    private SphereCollider detectionCollider;

    [Header("Window Positioning")]
    public RectTransform windowOriginPoint;


    void Start()
    {
        // Instantiate and setup UI
        chestGridInstance = Instantiate(chestGridPrefab);
        chestGridInstance.transform.SetParent(GameObject.Find("Canvas").transform, false);
        chestGridInstance.SetActive(false);

        // Configure grid
        GridVisualizer visualizer = chestGridInstance.transform.Find("Inventory Panel")?.GetComponent<GridVisualizer>();
        if (visualizer != null)
        {
            visualizer.rows = gridRows;
            visualizer.columns = gridColumns;
        }

        // Link UI controller
        uiController = chestGridInstance.GetComponent<ChestUIController>();
        if (uiController != null)
        {
            uiController.linkedChest = this;
            uiController.toggleTargetChild = chestGridInstance.transform.Find("Inventory Panel")?.gameObject;
        }

        // Configure detection collider
        detectionCollider = GetComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;

        //set window position
        uiController.rectTransform.position = windowOriginPoint.position;
    }

    public void Interact()
    {
        if (chestGridInstance != null)
        {
            chestGridInstance.SetActive(!chestGridInstance.activeSelf);

            //reset pos of window
            uiController.rectTransform.position = windowOriginPoint.position;

            if (!uiController.toggleTargetChild.activeSelf)
            {
                uiController.ToggleChild();
            }
        } 
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && chestGridInstance.activeSelf)
        {
            Interact();
            
        }
    }

    // Show sphere in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
