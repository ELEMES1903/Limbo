using UnityEngine;

public class LedgeHangState : PlayerStateBase
{


    [Header("LedgeHang Movement")]
    private Vector3 ledgePoint;
    private float horizontalOffset = 0.7f;
    private float verticalOffset = -1.0f;
    private float hangMoveSpeed = 2f;
    private float rotationSpeed = 5f;

    [Header("Ledge Camera")]
    private float yaw = 0f;
    private float pitch = 0f;
    public float maxYaw = 90f;
    public float maxPitch = 90f;

    [Header("Ledge Jump")]
    public float ledgeJumpDistance = 15f;
    public float ledgeJumpHeight = 4f;

    public LedgeHangState(PlayerStateManager manager, Vector3 ledgePoint) : base(manager)
    {
        this.ledgePoint = ledgePoint;
    }

    public override void EnterState()
    {
        manager.controller.enabled = false;

        // Reposition to ledge
        Vector3 hangPosition = ledgePoint - manager.transform.forward * horizontalOffset + Vector3.up * verticalOffset;
        manager.transform.position = hangPosition;

        manager.controller.enabled = true;
    }

    public override void UpdateState()
    {
        LedgeMovement();
        LookLimited();
        //manager.Look();
        LedgeJump();
    }

    public void LedgeMovement()
    {
        // Raycasting to find wall for alignment and movement direction
        Vector3 origin = manager.ledgeRaycastOrigin.position;
        Vector3 forward = manager.ledgeRaycastOrigin.forward;

        float angleOffset = 25f;
        float rayDistance = manager.ledgeRayDistance;

        // Default: no wall
        bool gotWall = false;
        RaycastHit wallHit;

        // Main center ray
        if (Physics.Raycast(origin, forward, out wallHit, rayDistance, manager.wallLayer))
        {
            gotWall = true;
        }
        else
        {
            // Try left ray
            Vector3 leftDir = Quaternion.AngleAxis(-angleOffset, Vector3.up) * forward;
            if (Physics.Raycast(origin, leftDir, out wallHit, rayDistance, manager.wallLayer))
            {
                gotWall = true;
                forward = leftDir;
            }
            else
            {
                // Try right ray
                Vector3 rightDir = Quaternion.AngleAxis(angleOffset, Vector3.up) * forward;
                if (Physics.Raycast(origin, rightDir, out wallHit, rayDistance, manager.wallLayer))
                {
                    gotWall = true;
                    forward = rightDir;
                }
            }
        }

        // Visual Debugging
        Debug.DrawRay(origin, forward * rayDistance, Color.red, 0.1f); // active ray
        Debug.DrawRay(origin, Quaternion.AngleAxis(-angleOffset, Vector3.up) * manager.ledgeRaycastOrigin.forward * rayDistance, Color.magenta, 0.1f); // left
        Debug.DrawRay(origin, Quaternion.AngleAxis(angleOffset, Vector3.up) * manager.ledgeRaycastOrigin.forward * rayDistance, Color.cyan, 0.1f); // right

        if (!gotWall) return;

        Vector3 wallNormal = wallHit.normal;
        float input = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(input) > 0.1f)
        {
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);
            Vector3 moveDir = -wallRight * input;

            manager.controller.Move(moveDir * hangMoveSpeed * Time.deltaTime);

            // Smoothly rotate to face the wall
            Quaternion targetRotation = Quaternion.LookRotation(-wallNormal);
            manager.transform.rotation = Quaternion.Slerp(manager.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void LookLimited()
    {
        float mouseX = Input.GetAxis("Mouse X") * manager.mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * manager.mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        manager.pitch -= mouseY;

        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        manager.pitch = Mathf.Clamp(manager.pitch, -maxPitch, maxPitch);

        manager.cameraHolder.localRotation = Quaternion.Euler(manager.pitch, yaw, 0f);
    }

    public void LedgeJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            Vector3 jumpDirection = manager.cameraHolder.forward * ledgeJumpDistance + Vector3.up * ledgeJumpHeight;
            manager.SwitchState(new AirState(manager, jumpDirection));
        }
    }
    public override void ExitState()
    {
        //reset camera to be compatible with look()
        // Get the current yaw from the camera holder
        float cameraYaw = manager.cameraHolder.rotation.eulerAngles.y;

        // Apply that yaw to the player transform
        manager.transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);

        // Reset the cameraHolder's local yaw
        Vector3 cameraLocalEuler = manager.cameraHolder.localEulerAngles;
        cameraLocalEuler.y = 0f;
        manager.cameraHolder.localEulerAngles = cameraLocalEuler;
    }
}
