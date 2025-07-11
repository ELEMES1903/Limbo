using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LedgeMovement : MonoBehaviour
{
    [Header("Ledge Settings")]
    public float ledgeGrabRange = 1f;
    public float ledgeHeightCheck = 1.5f;
    public LayerMask ledgeLayer;
    public Transform ledgeGrabPoint;

    [Header("Shimmy Settings")]
    public float shimmySpeed = 2f;

    private PlayerMovement player;
    private CharacterController controller;
    private Vector3 ledgeNormal;
    private Vector3 ledgePoint;

    private void Start()
    {
        player = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        switch (player.currentState)
        {
            case PlayerMovement.PlayerState.Normal:
                CheckForLedge();
                break;
            case PlayerMovement.PlayerState.Hanging:
                HandleLedgeMovement();
                break;
        }
    }

    void CheckForLedge()
    {
        if (Physics.Raycast(ledgeGrabPoint.position, transform.forward, out RaycastHit hit, ledgeGrabRange, ledgeLayer))
        {
            Vector3 ledgeCheckPos = hit.point + Vector3.up * ledgeHeightCheck;
            if (!Physics.Raycast(ledgeCheckPos, Vector3.down, out RaycastHit ledgeHit, ledgeHeightCheck, ledgeLayer))
                return;

            ledgePoint = ledgeHit.point;
            ledgeNormal = hit.normal;
            StartLedgeGrab();
        }
    }

    void StartLedgeGrab()
    {
        player.currentState = PlayerMovement.PlayerState.Hanging;
        controller.enabled = false;
        transform.position = ledgePoint - (ledgeNormal * 0.5f);
        transform.forward = -ledgeNormal;
    }

    void HandleLedgeMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");

        // Update ledge normal based on forward raycast
        if (Physics.Raycast(ledgeGrabPoint.position, transform.forward, out RaycastHit hit, ledgeGrabRange, ledgeLayer))
        {
            ledgeNormal = hit.normal;

            // Align player rotation to wall
            Quaternion targetRotation = Quaternion.LookRotation(-ledgeNormal, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        // Calculate movement direction along the ledge curve
        Vector3 ledgeDirection = Vector3.Cross(Vector3.up, ledgeNormal).normalized;
        Vector3 shimmyMovement = ledgeDirection * horizontal * shimmySpeed * Time.deltaTime;

        transform.position += shimmyMovement;

        // Climb or drop off ledge
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClimbLedge();
        }
        else if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            DropLedge();
        }
    }


    void ClimbLedge()
    {
        Vector3 climbUp = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
        transform.position = climbUp;
        controller.enabled = true;
        player.currentState = PlayerMovement.PlayerState.Normal;
    }

    void DropLedge()
    {
        transform.position += Vector3.down * 1f;
        controller.enabled = true;
        player.currentState = PlayerMovement.PlayerState.Normal;
    }
    private void OnDrawGizmos()
    {
        if (player != null && player.currentState == PlayerMovement.PlayerState.Hanging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(ledgeGrabPoint.position, -ledgeNormal);
        }
    }

}
