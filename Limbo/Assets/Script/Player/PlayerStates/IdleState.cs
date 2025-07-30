using UnityEngine;

public class IdleState : PlayerStateBase
{
    public IdleState(PlayerStateManager manager) : base(manager) {}

    public override void EnterState()
    { 
        manager.rotatePlayerToCamera = true;
    }

    public override void UpdateState()
    {

        manager.HandleMovementInput();
        //manager.CustomGravity();
        manager.MovePlayer();
        manager.Look();
    }
    public override void FixedUpdateState()
    {
        
    }
    public override void ExitState() {}
}
