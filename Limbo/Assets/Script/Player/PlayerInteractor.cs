using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactRange = 5f;
    public LayerMask interactableLayer;

    void Update()
    {
        // Create a ray from the center of the screen
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        // Always draw the ray, even if it hits nothing
        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.green);

        // Check if the ray hits something interactable
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            if (Input.GetMouseButtonDown(1)) // Right-click
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }
}
