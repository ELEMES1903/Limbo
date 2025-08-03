using Unity.VisualScripting;
using UnityEngine;
using Dreamteck.Splines;
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
    
    public RopeHangState(PlayerStateManager manager) : base(manager){ }
    public override void EnterState()
    {
        manager.inState = true;
        manager.rotatePlayerToCamera = false;

        manager.splineFollower.direction = GetLandingDirection(manager.ropeGrabPoint.transform.position, manager.ropeGrabPoint.transform.forward, manager.splineFollower);
        manager.configJoint.connectedBody = manager.ropeGrabPoint.transform.GetComponent<Rigidbody>();
        
        manager.transform.position = manager.ropeGrabPoint.transform.position;

        // reset velocity
        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;
    }
    public override void UpdateState()
    {
        manager.Look();
        manager.CustomGravity();
        //manager.ResetPlayerVelocity();
    }
    public override void FixedUpdateState()
    {
        
    }

    Spline.Direction GetLandingDirection(Vector3 position, Vector3 forward, SplineFollower follower) 
    {
        SplineSample result = new();
        follower.Project(position, ref result);
        float dot = Vector3.Dot(result.forward, forward);
        manager.splineFollower.SetPercent(result.percent);
        return (Spline.Direction)Mathf.Sign(dot);
    }

    public override void ExitState()
    {
        manager.ropeDetected = false;
        manager.StartTimer("LedgeHangCooldown", RopeHangCooldown);
    }
}
