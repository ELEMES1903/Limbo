using UnityEngine;

public class AirState : PlayerStateBase
{
    private Vector3 airDirection;
    private float airTime = 0f;

    public AirState(PlayerStateManager manager, Vector3 direction) : base(manager)
    {
        airDirection = direction;
    }

    public override void EnterState()
    {
        manager.verticalVelocity = airDirection.y;
        manager.currentVelocity = new Vector3(airDirection.x, 0f, airDirection.z);
    }

    public override void UpdateState()
    {
        AirMovement();
        manager.Look();
    }

    public void AirMovement()
    {
        // Optional air control (left/right)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;
        Vector3 camForward = manager.cameraHolder.forward;
        Vector3 camRight = manager.cameraHolder.right;
        camForward.y = 0f;
        camRight.y = 0f;

        Vector3 inputDirection = (camForward * input.z + camRight * input.x).normalized;
        manager.currentVelocity = Vector3.Lerp(manager.currentVelocity, inputDirection * manager.moveSpeed, Time.deltaTime * 2f);

        // Apply gravity
        manager.verticalVelocity += manager.gravity * Time.deltaTime;

        // Combine and move
        Vector3 finalMove = manager.currentVelocity + Vector3.up * manager.verticalVelocity;
        manager.controller.Move(finalMove * Time.deltaTime);

        airTime += Time.deltaTime;
    }
    public override void ExitState() { }
}
