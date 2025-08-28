using UnityEngine;

public class WalkingState : PlayerStateBase
{
    public WalkingState(PlayerStateManager manager) : base(manager) {}
    public override void EnterState()
    {
        manager.playerInputActions.OnGround.Enable();
        manager.playerInputActions.OnGround.Jump.performed += manager.DetectJumpInput;
    }

    public override void UpdateState()
    {
        manager.HandleMovementInput(); 
        manager.Look();
    }
    public override void FixedUpdateState()
    {
        manager.Move(); 
    }
    public override void ExitState()
    {
        manager.playerInputActions.OnGround.Jump.performed -= manager.DetectJumpInput; 
        //manager.jumpInputDetected = false; 
    }
}

