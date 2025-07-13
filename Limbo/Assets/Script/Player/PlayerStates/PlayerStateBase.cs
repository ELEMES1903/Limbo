using UnityEngine;

public abstract class PlayerStateBase
{
    protected PlayerStateManager manager;
    protected CharacterController controller;
    protected Transform cameraHolder;

    public PlayerStateBase(PlayerStateManager manager)
    {
        this.manager = manager;
        this.controller = manager.controller;
        this.cameraHolder = manager.cameraHolder;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}
