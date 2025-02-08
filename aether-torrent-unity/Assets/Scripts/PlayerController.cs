using System;
using System.Collections;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")] public CharacterController characterController;
    public Animator animator;

    [Header("Jump Settings")] [Tooltip("Maximum jump height when jump is fully held.")]
    public float maxJumpHeight = 5f;

    [Tooltip("Low jump height when jump is tapped.")]
    public float lowJumpHeight = 2f;

    [Tooltip("Maximum jump height for a double jump.")]
    public float doubleJumpHeight = 3f;

    [Tooltip("Gravity (should be a negative value).")]
    public float gravity = -9.81f;

    [Tooltip("Time allowed after leaving the ground to still jump (coyote time).")]
    public float coyoteTime = 0.2f;

    [Tooltip("Time window to buffer a jump input before landing.")]
    public float jumpBufferTime = 0.2f;

    [Tooltip("Total jumps allowed (2 for double jump).")] [SerializeField]
    private int maxJumps = 2;

    [Header("Jump Sustain Settings")] [Tooltip("Additional upward force applied while the jump button is held on the first jump.")] [SerializeField]
    private float jumpSustainForce = 20f; // For first jump

    [Tooltip("Maximum duration (in seconds) for the jump sustain phase on the first jump.")] [SerializeField]
    private float maxJumpSustainTime = 0.2f;

    [Tooltip("Additional upward force applied while the jump button is held on the double jump.")] [SerializeField]
    private float doubleJumpSustainForce = 10f; // For second jump

    [Tooltip("Maximum duration (in seconds) for the jump sustain phase on the double jump.")] [SerializeField]
    private float maxDoubleJumpSustainTime = 0.1f;

    [Header("Movement Settings")] public float maxSpeed;
    [SerializeField] private float movementLerpSpeed;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float gravityMult = 3f;
    [SerializeField] private float jumpGravityMult = 3f;
    [SerializeField] private float fallTime;

    [Header("Dash Settings")] public float dashDistance = 5f; // Total distance to cover during dash
    public float dashTime = 0.2f; // Total time the dash lasts
    public float dashCooldown = 1f; // Cooldown period before dash can be used again

    private bool isDashing = false; // Whether the player is currently dashing
    private float dashTimer = 0f; // Timer for the duration of the current dash
    private Vector3 dashDirection; // The normalized direction in which to dash
    private float dashCooldownTimer = 0f; // Internal timer for dash cooldown
    private int dashCount = 0; // Counts dashes used in the current jump (max 1)
    private float currentDashSpeed = 0f; // Computed speed = dashDistance / dashTime

    // Internal state variables
    private float lastGroundedTime = -999f;
    private float jumpBufferCounter = 0f;
    private int jumpCount = 0;
    private float jumpSustainTimer = 0f;

    private Vector2 inputMovement;
    public Vector2 inputLook;
    public Vector3 direction;
    public float verticalVelocity;

    private bool holdJump = false;
    private bool jumping = false;
    private bool isFalling = false;
    private bool startFall = false;
    private float startFallTime;
    private float turnSmoothVelocity;

    //Debug Data
    float currentHeight;
    float lastHeight;
    public float lastJumpHeight;

    // Animator parameter hashes
    private static readonly int ChrSpeedID = Animator.StringToHash("chrSpeed");
    private static readonly int JumpID = Animator.StringToHash("jump");
    private static readonly int IsFallingID = Animator.StringToHash("isFalling");
    private static readonly int LandID = Animator.StringToHash("land");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Existing update tasks...
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        // Decrement dash cooldown timer if active.
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        ApplyRotation();
        UpdateAnimator();

        if (!IsGrounded() || startFall || isFalling)
            CheckFall();

        UpdateJump();
        MeasureHeight();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            // Apply standard gravity to update vertical velocity during dash.
            ApplyStandardGravity();

            // Compute dash movement vector:
            // Horizontal movement uses the computed currentDashSpeed.
            var dashMove = dashDirection * currentDashSpeed;
            dashMove.y = verticalVelocity; // Preserve vertical motion.

            // Move the character.
            characterController.Move(dashMove * Time.fixedDeltaTime);

            // Decrement the dash timer and end dash when time is up.
            if ((dashTimer -= Time.fixedDeltaTime) <= 0f)
                isDashing = false;
        }
        else
        {
            ApplyMovement();
            ApplyGravity();
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 lastJumpHeightPos = new Vector3(characterController.transform.position.x, lastJumpHeight - characterController.height / 2, characterController.transform.position.z);
        Handles.DrawWireDisc(lastJumpHeightPos, Vector3.up, 1);
        GUIStyle style = new GUIStyle
        {
            normal =
            {
                textColor = Color.red
            },
            fontSize = 20
        };
        Handles.Label(lastJumpHeightPos, "Last Jump:" + (lastJumpHeight - characterController.height / 2).ToString(CultureInfo.InvariantCulture), style);
    }

    private void MeasureHeight()
    {
        currentHeight = characterController.transform.position.y;
        if (currentHeight > lastHeight)
        {
            lastJumpHeight = currentHeight;
        }

        lastHeight = currentHeight;
    }


    private bool IsGrounded() => characterController.isGrounded;

    private void ApplyGravity()
    {
        if (IsGrounded() && verticalVelocity <= 0)
        {
            verticalVelocity = verticalVelocity < 0 ? -1f : verticalVelocity; // Keeps you "stuck" to the ground.
            lastGroundedTime = Time.time;
            jumpCount = dashCount = 0; // Reset jump and dash count on landing
            jumping = false;
            jumpSustainTimer = 0f;
        }
        else
        {
            // Always apply standard gravity.
            ApplyStandardGravity();

            // Determine the current sustain parameters.
            var currentSustainForce = (jumpCount == 1) ? jumpSustainForce : doubleJumpSustainForce;
            var currentMaxSustainTime = (jumpCount == 1) ? maxJumpSustainTime : maxDoubleJumpSustainTime;

            if (jumping && holdJump && jumpSustainTimer < currentMaxSustainTime)
            {
                verticalVelocity += currentSustainForce * Time.fixedDeltaTime;
                jumpSustainTimer += Time.fixedDeltaTime;
            }
            else if (!holdJump)
            {
                jumping = false;
            }
        }

        direction.y = verticalVelocity;
    }

    // Standard gravity application (with multipliers) for when sustain is not applied.
    private void ApplyStandardGravity()
    {
        var currentGravityMult = verticalVelocity < 0 ? jumpGravityMult + gravityMult : gravityMult;
        verticalVelocity += gravity * currentGravityMult * Time.fixedDeltaTime;
    }

    private void UpdateJump()
    {
        // Allow jump if on the ground (or within coyote time) or if additional jumps are available.
        var canJump = IsGrounded() ||
                      (Time.time - lastGroundedTime <= coyoteTime) ||
                      (jumpCount < maxJumps);

        if (jumpBufferCounter > 0f && canJump)
        {
            Jump();
            jumpBufferCounter = 0f; // Clear the jump buffer after executing jump.
        }
    }

    private void Jump()
    {
        // Optionally check slopes; here we log and allow the jump.
        if (!CheckSlope() || (!IsGrounded() && jumpCount >= maxJumps))
            return;

        jumpCount++;

        // Calculate the target impulse based on the jump count.
        var targetImpulse = Mathf.Sqrt(2f * -gravity * (jumpCount == 1 ? maxJumpHeight : doubleJumpHeight));

        // If already moving upward, take the maximum of the current velocity or the target impulse.
        verticalVelocity = verticalVelocity > 0 ? Mathf.Max(verticalVelocity, targetImpulse) : targetImpulse;

        // Begin the jump sustain phase by resetting the timer.
        jumping = true;
        jumpSustainTimer = 0f;

        // Trigger jump animation.
        animator.SetTrigger(JumpID);
    }

    // Basic slope check; extend this if you want to restrict jumping on steep slopes.
    private bool CheckSlope()
    {
        if (Physics.Raycast(new Ray(transform.position, Vector3.down), out var hitInfo, 0.2f))
        {
            Debug.Log("Slope normal: " + hitInfo.normal);
            // You could, for example, disallow jumps on slopes beyond a certain angle.
        }

        return true;
    }

    private void CheckFall()
    {
        if (IsGrounded())
        {
            startFall = isFalling = false;
            animator.SetBool(IsFallingID, false);
            animator.SetTrigger(LandID);
        }
        else if (!isFalling)
        {
            if (!startFall)
            {
                startFall = true;
                startFallTime = Time.time;
            }
            else if (Time.time >= startFallTime + fallTime)
            {
                isFalling = true;
                animator.SetBool(IsFallingID, true);
            }
        }
    }

    // Input callback for jump events.
    public void JumpPressed(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Buffer the jump input and start holding the jump.
            jumpBufferCounter = jumpBufferTime;
            holdJump = true;
        }
        else if (context.canceled)
        {
            holdJump = false;
            // If released early while still ascending, clamp the upward velocity for a shorter jump.
            if (verticalVelocity > Mathf.Sqrt(2f * -gravity * lowJumpHeight))
            {
                verticalVelocity = Mathf.Sqrt(2f * -gravity * lowJumpHeight);
            }
        }
    }

    public void DashPressed(InputAction.CallbackContext context)
    {
        if (context.started && dashCooldownTimer <= 0f && dashCount < 1)
        {
            // Determine dash direction:
            // - If movement input exists, use that.
            // - Otherwise, use the character's current facing direction.
            dashDirection = inputMovement.sqrMagnitude > 0.1f
                ? new Vector3(inputMovement.x, 0f, inputMovement.y).normalized
                : transform.forward;

            // Initiate the dash.
            isDashing = true;
            dashTimer = dashTime; // Set dash duration
            currentDashSpeed = dashDistance / dashTime; // Compute dash speed from distance and time
            dashCount++; // Mark that a dash has been used for this jump
            dashCooldownTimer = dashCooldown; // Start the cooldown timer

            // (Optional) Trigger dash animation or visual effects here.
        }
    }

    // Input callback for movement.
    public void InputMovement(InputAction.CallbackContext context)
    {
        inputMovement = context.ReadValue<Vector2>();
    }

    private void ApplyMovement()
    {
        var desiredVelocity = new Vector3(inputMovement.x, 0, inputMovement.y) * maxSpeed;
        var currentVelocity = characterController.velocity;
        var maxSpeedChange = movementLerpSpeed * Time.deltaTime;

        // Smoothly interpolate the horizontal velocity.
        direction.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity.x, maxSpeedChange);
        direction.z = Mathf.MoveTowards(currentVelocity.z, desiredVelocity.z, maxSpeedChange);

        // Move the character.
        characterController.Move(direction * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        var charVelocity = characterController.velocity;
        var localVelocity = transform.InverseTransformDirection(charVelocity);
        var speed = localVelocity.z;
        animator.SetFloat(ChrSpeedID, speed);
    }

    private void ApplyRotation()
    {
        if (inputMovement.sqrMagnitude == 0)
            return;

        var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }
}