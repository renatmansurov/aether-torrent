using UnityEngine;

public class FallingState : CharacterState
{
    public FallingState(PlayerController player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        Debug.Log("Falling");
        Player.gravity *= Player.fallGravityMult;
        // Optionally, trigger falling animation.
        // For example: Player.animator.SetBool("isFalling", true);
    }

    public override void HandleInput()
    {
        // Allow a double jump if a jump input is buffered and the jump count is below max.
        if (Player.jumpBufferCounter > 0 && JumpState.JumpCount < Player.maxJumps)
        {
            // Clear the buffer so that we only trigger one jump.
            Player.jumpBufferCounter = 0;
            // Transition to JumpState to perform the double jump.
            StateMachine.ChangeState(new JumpState(Player, StateMachine));
            return;
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
        }
    }

    public override void FixedUpdate()
    {
        Player.ApplyGravity();
    }
}