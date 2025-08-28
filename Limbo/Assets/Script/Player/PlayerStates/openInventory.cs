using UnityEngine;

public class OpenInventory : MonoBehaviour
{
    public GameObject targetObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }


}
