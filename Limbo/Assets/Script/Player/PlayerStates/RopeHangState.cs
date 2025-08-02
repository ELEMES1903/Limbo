using Unity.VisualScripting;
using UnityEngine;

public class RopeHangState : PlayerStateBase
{
    [Header("Parameters")]
    Vector3 ropePoint;
    Vector3 ropeNormal;
    public float horizontalOffset = 0.3f;
    public float verticalOffset = 0.6f;
    public float RopeHangCooldown = 0.5f;
    public float radius = 1f;
    Vector3 sphereCastOrigin;

    RaycastHit hit;
    
    public RopeHangState(PlayerStateManager manager, RaycastHit hit) : base(manager)
    {
        this.hit = hit;
    }
    public override void EnterState()
    {
        Debug.Log("enter");
        HingeJointSetUp();

        manager.inState = true;
        manager.rotatePlayerToCamera = false;

        Rigidbody ropeRb = hit.collider.GetComponent<Rigidbody>();
        manager.hinge.connectedBody = ropeRb;
        hit.collider.GetComponent<CapsuleCollider>().enabled = false;

        manager.transform.forward = -hit.normal;
        Vector3 hangPosition = hit.point - manager.transform.forward * horizontalOffset;
        manager.rb.position = hangPosition;

        // reset velocity
        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;
    }
    public override void UpdateState()
    {
        manager.Look();
        RopeMovement();
        RopeDrop();
        manager.ResetPlayerVelocity();
    }
    public override void FixedUpdateState()
    {
        
    }
    private void RopeMovement()
    {
        Debug.Log("hi");
        Debug.DrawRay(manager.transform.position, -ropeNormal * 2f, Color.yellow);
        Rigidbody ropeRb = manager.hinge.connectedBody;

        // Input
        float h = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxis("Vertical");   // W/S or Up/Down

        // Optional: Get swing direction based on camera or player orientation
        Vector3 swingDirection = (manager.cameraHolder.right * h + manager.cameraHolder.forward * v).normalized;

        // Remove upward component to avoid climbing
        swingDirection = Vector3.ProjectOnPlane(swingDirection, Vector3.up);

        // Apply force to rope segment
        float swingForce = 30f;
        ropeRb.AddForce(swingDirection * swingForce, ForceMode.Force);
    }
    private void RopeDrop()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            manager.SwitchState(new AirState(manager));
        }
    }

    private void HingeJointSetUp()
    {
        manager.hinge = manager.gameObject.AddComponent<HingeJoint>();
    }
    public override void ExitState()
    {
        manager.inState = false;
        manager.rotatePlayerToCamera = true;

        hit.collider.GetComponent<CapsuleCollider>().enabled = true;

        manager.StartTimer("RopeHangCooldown", RopeHangCooldown);

        manager.DestroyHinge();
        Debug.Log("exit");
    }
}
