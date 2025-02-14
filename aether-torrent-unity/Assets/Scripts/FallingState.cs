using UnityEngine;

public class FallingState : CharacterState
{
    public FallingState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        Player.gravity *= Player.jumpFallGravityMult;
        // Optionally, trigger falling animation.
        // For example: Player.animator.SetBool("isFalling", true);
    }

    public override void HandleInput()
    {
        if (Player.jumpBufferCounter > 0 && JumpState.JumpCount < Player.maxJumps)
        {
            Player.jumpBufferCounter = 0;
            StateMachine.ChangeState(new JumpState(Player, StateMachine));
        }
    }

    public override void Update()
    {
        if (Player.IsGrounded())
        {
            // Optionally, reset falling animation parameters.
            // Player.animator.SetBool("isFalling", false);
            Player.gravity = PlayerController.BaseGravity;
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
            return;
        }

        if (Player.verticalVelocity < 0 && !Player.holdJump)
        {
            Player.gravity = Player.jumpGravity;
        }
        else
        {
            Player.gravity = Player.hoverGravity;
        }
    }

    public override void FixedUpdate()
    {
        Player.ApplyGravity();
    }
}