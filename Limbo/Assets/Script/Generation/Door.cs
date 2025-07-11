using UnityEngine;

public class Door : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 1f);
    }
    
    public bool isConnected = false;
}
