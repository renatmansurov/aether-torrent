using UnityEngine;

public class JumpState : CharacterState
{
    // Static jumpCount tracks the total number of jumps performed in the air.
    public static int jumpCount;
    public float jumpSustainTimer;
    public bool jumping;

    public JumpState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        // Check if a jump is allowed:
        // - If the slope is too steep, or
        // - If in air and we've already reached max jumps, then transition back.
        if (!Player.CheckSlope() || (!Player.IsGrounded() && jumpCount >= Player.maxJumps))
        {
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
            return;
        }

        // Increment jump count and calculate the jump impulse.
        jumpCount++;
        float jumpHeight = (jumpCount == 1) ? Player.maxJumpHeight : Player.doubleJumpHeight;
        float targetImpulse = Mathf.Sqrt(2f * -Player.gravity * jumpHeight);

        // Apply the jump impulse (if already moving upward, choose the higher velocity).
        Player.verticalVelocity = (Player.verticalVelocity > 0) ?
                                  Mathf.Max(Player.verticalVelocity, targetImpulse) :
                                  targetImpulse;

        // Set jump-related flags.
        jumping = true;
        jumpSustainTimer = 0f;

        // (Optional) Trigger jump animation.
        // Player.animator.SetTrigger(Player.JumpID);
    }

    public override void HandleInput()
    {
        // **New Double Jump Check:**
        // If a jump input is buffered (set in PlayerController) and we haven't reached max jumps,
        // trigger a new jump (i.e. a double jump) by transitioning to a new JumpState.
        if (Player.jumpBufferCounter > 0 && jumpCount < Player.maxJumps)
        {
            // Clear the jump buffer so that we don't trigger multiple jumps.
            Player.jumpBufferCounter = 0;
            // Transition to a new JumpState, which will execute the new jump.
            StateMachine.ChangeState(new JumpState(Player, StateMachine));
            return;
        }

        // If the player has landed (and we're not sustaining a jump), transition to MovementState.
        if (Player.IsGrounded() && !jumping)
        {
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
        }
    }

    public override void Update()
    {
        // 1. Check if the upward motion has ceased.
        if (Player.verticalVelocity <= 0)
        {
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
            return;
        }

        // 2. Handle jump sustain logic when airborne.
        if (!Player.IsGrounded())
        {
            // Choose the sustain force and maximum sustain time based on whether it's the first jump or a double jump.
            float currentSustainForce = (jumpCount == 1) ? Player.jumpSustainForce : Player.doubleJumpSustainForce;
            float currentMaxSustainTime = (jumpCount == 1) ? Player.maxJumpSustainTime : Player.maxDoubleJumpSustainTime;

            // If the jump is active, the jump button is held, and we haven't exceeded sustain time:
            if (jumping && Player.holdJump && jumpSustainTimer < currentMaxSustainTime)
            {
                Player.verticalVelocity += currentSustainForce * Time.fixedDeltaTime;
                jumpSustainTimer += Time.fixedDeltaTime;
            }
            // Otherwise, if the jump button has been released, perform early termination:
            else if (!Player.holdJump)
            {
                float lowJumpVelocity = Mathf.Sqrt(2f * -Player.gravity * Player.lowJumpHeight);
                if (Player.verticalVelocity > lowJumpVelocity)
                {
                    Player.verticalVelocity = lowJumpVelocity;
                }
                jumping = false;
            }
        }
    }

    public override void FixedUpdate()
    {
        // Apply general gravity and movement.
        Player.ApplyGravity();
        Player.ApplyMovement();
    }
}