using UnityEngine;

public class FallingState : CharacterState
{
    public FallingState(PlayerController player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        // Optionally, trigger falling animation.
        // For example: Player.animator.SetBool("isFalling", true);
    }

    public override void HandleInput()
    {
        // Allow a double jump if a jump input is buffered and the jump count is below max.
        if (Player.jumpBufferCounter > 0 && JumpState.jumpCount < Player.maxJumps)
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
        // If the player has landed, exit FallingState.
        if (Player.IsGrounded())
        {
            // Optionally, reset falling animation parameters.
            // Player.animator.SetBool("isFalling", false);
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
            return;
        }
    }

    public override void FixedUpdate()
    {
        // Apply general gravity and horizontal movement.
        Player.ApplyGravity();
        Player.ApplyMovement();
    }
}