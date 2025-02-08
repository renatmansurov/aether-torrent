using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;
    public Animator animator;

    [Header("Jump Settings")]
    [Tooltip("Maximum jump height when jump is fully held.")]
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

    [Tooltip("Total jumps allowed (2 for double jump).")]
    [SerializeField] private int maxJumps = 2;

    [Header("Jump Sustain Settings")]
    [Tooltip("Additional upward force applied while the jump button is held on the first jump.")]
    [SerializeField] private float jumpSustainForce = 20f; // For first jump

    [Tooltip("Maximum duration (in seconds) for the jump sustain phase on the first jump.")]
    [SerializeField] private float maxJumpSustainTime = 0.2f;

    [Tooltip("Additional upward force applied while the jump button is held on the double jump.")]
    [SerializeField] private float doubleJumpSustainForce = 10f; // For second jump

    [Tooltip("Maximum duration (in seconds) for the jump sustain phase on the double jump.")]
    [SerializeField] private float maxDoubleJumpSustainTime = 0.1f;

    [Header("Movement Settings")]
    public float maxSpeed;
    [SerializeField] private float movementLerpSpeed;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float gravityMult = 3f;
    [SerializeField] private float jumpGravityMult = 3f;
    [SerializeField] private float fallTime;

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
        // Decrement the jump buffer timer each frame.
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        ApplyRotation();
        UpdateAnimator();

        // Check falling state for animations, etc.
        if (!IsGrounded() || startFall || isFalling)
            CheckFall();

        // Process jump if buffered and conditions allow.
        UpdateJump();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyGravity();
    }

    private bool IsGrounded() => characterController.isGrounded;

    private void ApplyGravity()
    {
        if (IsGrounded() && verticalVelocity <= 0)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -1f; // Keeps you "stuck" to the ground.

            lastGroundedTime = Time.time;
            jumpCount = 0;
            jumping = false;
            jumpSustainTimer = 0f;
        }
        else
        {
            // Always apply standard gravity.
            ApplyStandardGravity();

            // Determine the current sustain parameters.
            float currentSustainForce = (jumpCount == 1) ? jumpSustainForce : doubleJumpSustainForce;
            float currentMaxSustainTime = (jumpCount == 1) ? maxJumpSustainTime : maxDoubleJumpSustainTime;

            // If we're in the sustain phase, add the additional upward force.
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
        float currentGravityMult = verticalVelocity < 0 ? jumpGravityMult + gravityMult : gravityMult;
        verticalVelocity += gravity * currentGravityMult * Time.fixedDeltaTime;
    }

    private void UpdateJump()
    {
        // Allow jump if on the ground (or within coyote time) or if additional jumps are available.
        bool canJump = IsGrounded() ||
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

        float targetImpulse = (jumpCount == 1)
            ? Mathf.Sqrt(2f * -gravity * maxJumpHeight)
            : Mathf.Sqrt(2f * -gravity * doubleJumpHeight);

        // If already moving upward, take the maximum of the current velocity or the target impulse.
        if (verticalVelocity > 0)
            verticalVelocity = Mathf.Max(verticalVelocity, targetImpulse);
        else
            verticalVelocity = targetImpulse;

        // Begin the jump sustain phase by resetting the timer.
        jumping = true;
        jumpSustainTimer = 0f;

        // Trigger jump animation.
        animator.SetTrigger(JumpID);
    }

    // Basic slope check; extend this if you want to restrict jumping on steep slopes.
    private bool CheckSlope()
    {
        if (Physics.Raycast(new Ray(transform.position, Vector3.down), out RaycastHit hitInfo, 0.2f))
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
            startFall = false;
            isFalling = false;
            animator.SetBool(IsFallingID, false);
            animator.SetTrigger(LandID);
            return;
        }

        if (isFalling)
            return;

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
            if (verticalVelocity > 0)
            {
                float lowJumpVelocity = Mathf.Sqrt(2f * -gravity * lowJumpHeight);
                if (verticalVelocity > lowJumpVelocity)
                    verticalVelocity = lowJumpVelocity;
            }
        }
    }

    // Input callback for movement.
    public void InputMovement(InputAction.CallbackContext context)
    {
        inputMovement = context.ReadValue<Vector2>();
    }

    private void ApplyMovement()
    {
        Vector3 desiredVelocity = new Vector3(inputMovement.x, 0, inputMovement.y) * maxSpeed;
        Vector3 currentVelocity = characterController.velocity;
        float maxSpeedChange = movementLerpSpeed * Time.deltaTime;

        // Smoothly interpolate the horizontal velocity.
        direction.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity.x, maxSpeedChange);
        direction.z = Mathf.MoveTowards(currentVelocity.z, desiredVelocity.z, maxSpeedChange);

        // Move the character.
        characterController.Move(direction * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        Vector3 charVelocity = characterController.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(charVelocity);
        float speed = localVelocity.z;
        animator.SetFloat(ChrSpeedID, speed);
    }

    private void ApplyRotation()
    {
        if (inputMovement.sqrMagnitude == 0)
            return;

        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }
}