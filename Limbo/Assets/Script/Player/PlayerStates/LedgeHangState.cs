using UnityEngine;

public class LedgeHangState : PlayerStateBase
{
    private Vector3 ledgePoint;
    
    // Tunable parameters
    private float horizontalOffset = 1f;
    private float verticalOffset = -1.0f;
    private float hangMoveSpeed = 2f;
    private float rotationSpeed = 5f;

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
        // Cast forward from camera to get wall normal
        Vector3 origin = manager.cameraHolder.position;
        Vector3 forward = manager.cameraHolder.forward;

        if (Physics.Raycast(origin, forward, out RaycastHit wallHit, manager.ledgeRayDistance, manager.wallLayer))
        {
            Vector3 wallNormal = wallHit.normal;
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);

            // Get horizontal input (A/D)
            float input = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(input) > 0.1f)
            {
                // Move parallel to wall
                Vector3 moveDir = -wallRight * input;
                manager.controller.Move(moveDir * hangMoveSpeed * Time.deltaTime);

                // Smoothly rotate to align with wall
                Quaternion targetRotation = Quaternion.LookRotation(-wallNormal);
                manager.transform.rotation = Quaternion.Slerp(manager.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    public override void ExitState() { }
}
