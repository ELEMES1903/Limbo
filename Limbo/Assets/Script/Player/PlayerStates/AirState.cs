using UnityEngine;

public class AirState : PlayerStateBase
{
    public AirState(PlayerStateManager manager) : base(manager)
    {
    }

    public override void EnterState()
    {

    }

    public override void UpdateState()
    {
        //manager.HandleMovementInput();
        //manager.MovePlayer(); 
        manager.CustomGravity();
        manager.Look();
    }
    public override void FixedUpdateState()
    {
        
    }

    public override void ExitState()
    { 
        
    }
}
