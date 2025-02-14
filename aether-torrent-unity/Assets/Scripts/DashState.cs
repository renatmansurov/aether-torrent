// Assets/Scripts/DashState.cs
using UnityEngine;
using System.Collections;

public class DashState : CharacterState
{
    private Vector3 dashDirection;

    public DashState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        dashDirection = Player.inputMovement.sqrMagnitude > 0.1f
            ? new Vector3(Player.inputMovement.x, 0f, Player.inputMovement.y).normalized
            : Player.transform.forward;

        Player.currentDashStart = Player.transform.position;
        Player.dashCount++;
        Player.dashCooldownTimer = Player.dashCooldown;
        Player.isDashing = true; // Ensure regular movement is paused

        // Start the dash coroutine
        Player.StartCoroutine(PerformDash(dashDirection, Player.dashDuration));
    }

    public override void HandleInput()
    {
        // No input handling while dashing
    }

    public override void Update()
    {
        // Optionally update dash visuals, etc.
    }

    public override void FixedUpdate()
    {
        // Apply gravity even during dash
        Player.ApplyGravity();
    }

    /// <summary>
    /// Coroutine that moves the CharacterController in the given direction until:
    /// - The dash time expires.
    /// - An obstacle (non-ground) is encountered.
    /// - If there is no safe ground ahead, the dash will continue only until the player reaches
    ///   the edge (a distance equal to dashThreshold from the dash start), then stops.
    /// </summary>
    private IEnumerator PerformDash(Vector3 moveDirection, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 1. Check for obstacles ahead
            if (IsObstacleInPath(moveDirection))
            {
                Debug.Log("Dash stopped: Obstacle detected.");
                break;
            }

            // 2. Check for safe ground ahead
            if (!IsGroundAhead(moveDirection))
            {
                // Instead of stopping immediately, allow dash to continue until reaching the edge.
                float distanceTraveled = Vector3.Distance(Player.currentDashStart, Player.transform.position);
                if (distanceTraveled < Player.dashThreshold)
                {
                    // Move the remaining distance this frame, clamped by dashSpeed.
                    float remaining = Player.dashThreshold - distanceTraveled;
                    float moveStep = Mathf.Min(remaining, Player.dashSpeed * Time.deltaTime);
                    Vector3 movement = moveDirection * moveStep;
                    Player.characterController.Move(movement);
                    elapsed += Time.deltaTime;
                    yield return null;
                    continue;
                }
                else
                {
                    Debug.Log("Reached dash edge.");
                    break;
                }
            }

            // 3. Normal dash movement when safe ground is detected
            Vector3 normalMovement = moveDirection * (Player.dashSpeed * Time.deltaTime);
            Player.characterController.Move(normalMovement);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Player.isDashing = false; // Reset the dashing flag

        // Transition back to movement state
        StateMachine.ChangeState(new MovementState(Player, StateMachine));
    }

    /// <summary>
    /// Checks for an obstacle in the dash direction using a raycast.
    /// Assumes obstacles are on Player.obstacleLayer.
    /// </summary>
    private bool IsObstacleInPath(Vector3 direction)
    {
        Vector3 origin = Player.characterController.bounds.center;
        float checkDistance = 0.5f; // Adjust as needed based on character size

        if (Physics.Raycast(origin, direction, out RaycastHit hit, checkDistance, Player.obstacleLayer))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if there is safe ground ahead within Player.maxFallLimit.
    /// Casts a ray downward from a point offset by dashThreshold.
    /// </summary>
    private bool IsGroundAhead(Vector3 direction)
    {
        Vector3 basePosition = Player.transform.position;
        Vector3 checkPosition = basePosition + direction * Player.dashThreshold;
        float rayLength = Player.maxFallLimit;

        // If a ray cast downward finds ground within maxFallLimit, consider it safe.
        if (Physics.Raycast(checkPosition, Vector3.down, out RaycastHit hit, rayLength, Player.groundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}