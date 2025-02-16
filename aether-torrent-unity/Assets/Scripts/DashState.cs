// Assets/Scripts/DashState.cs

using Unity.VisualScripting;
using UnityEngine;

public class DashState : CharacterState
{
    private float dashTimer;
    private Vector3 dashDirection;
    private bool dashIsSafe;
    private float dashNormalizedTimer;

    public DashState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        // Initialize the dash timer with the dash duration
        dashTimer = Player.dashDuration;

        // Set dash cooldown timer so the dash can't be reused immediately
        Player.dashCooldownTimer = Player.dashCooldown;

        // Increment dash count (if you wish to restrict number of dashes)
        Player.dashCount++;

        // Set the dashing flag
        Player.isDashing = true;

        // Determine dash direction based on input; if no input, dash forward
        dashDirection = Player.inputMovement.sqrMagnitude > 0.1f
            ? new Vector3(Player.inputMovement.x, 0f, Player.inputMovement.y).normalized
            : Player.transform.forward;

        // Record dash start and target positions for debugging (optional)
        Player.animator.SetTrigger("dash");
    }

    public override void HandleInput()
    {
        // Input handling during dash can be ignored or limited
    }


    public override void Update()
    {
        // Decrease the dash timer
        dashTimer -= Time.deltaTime;
        dashNormalizedTimer = 1 - dashTimer / Player.dashDuration;
        // End dash state once the dash duration is complete
        if (dashTimer <= 0)
        {
            Player.isDashing = false;
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
        }
    }

    public override void DrawGizmo()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
    }

    public override void FixedUpdate()
    {
        Vector3 dashMovement = dashDirection * (Player.dashSpeed * Time.fixedDeltaTime);
            Player.characterController.Move(dashMovement);
    }
}