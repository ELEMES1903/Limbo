using UnityEngine;
using UnityEngine.InputSystem;

public class IdleState : PlayerStateBase
{
    public IdleState(PlayerStateManager manager) : base(manager) {}

    public override void EnterState()
    {
        manager.rotatePlayerToCamera = true;

        manager.playerInputActions.OnGround.Enable();
        
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
        
    }
}
