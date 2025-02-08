using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private const float Gravity = -9.81f;

    private static readonly int ChrSpeedID = Animator.StringToHash("chrSpeed");
    private static readonly int JumpID = Animator.StringToHash("jump");
    private static readonly int IsFallingID = Animator.StringToHash("isFalling");
    private static readonly int LandID = Animator.StringToHash("land");
    public CharacterController characterController;
    public Animator animator;
    public Vector2 inputMovement;
    public Vector2 inputLook;

    public float maxSpeed;

    [SerializeField] private float movementLerpSpeed;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float gravityMult = 3;
    [SerializeField] private float jumpPower = 3f;
    [SerializeField] private float jumpGravityMult = 3;
    [SerializeField] private int maxNumberOfJumps = 2;
    [SerializeField] private float fallTime;

    public Vector3 direction;
    public float velocity;

    public GameObject projectile;

    private Vector3 _dashDirection;
    private bool _dashOnCooldown;
    private bool _dashPressed;
    private bool _dashReloaded;
    private bool _holdJump;
    private bool _isFalling;

    private bool _jumping;

    private float _mFireCooldown;
    private bool _mFiring;
    private int _numberOfJumps;
    private bool _startFall;
    private float _startFallTime;
    private float _turnSmoothVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void Update()
    {
        if (!_dashReloaded && !_dashOnCooldown)
        {
            StartCoroutine(DashCooldown());
        }

        ApplyRotation();
        UpdateAnimator();
        if (!IsGrounded() || _startFall || _isFalling) CheckFall();
    }

    public void FixedUpdate()
    {
        ApplyMovement();
        ApplyGravity();
    }

    private bool IsGrounded() => characterController.isGrounded;

private void ApplyGravity()
{
    if (IsGrounded() && !_jumping)
    {
        velocity = -1.0f;
    }
    else
    {
        _jumping = false;
        float currentGravityMult = velocity < 0 ? jumpGravityMult + gravityMult : gravityMult;
        velocity += Gravity * currentGravityMult * Time.fixedDeltaTime;
    }

    direction.y = velocity;
}

    private void CheckFall()
    {
        if (IsGrounded())
        {
            _startFall = _isFalling = false;
            animator.SetBool(IsFallingID, false);
            animator.SetTrigger(LandID);
            return;
        }

        if (_isFalling) return;

        if (!_startFall)
        {
            _startFall = true;
            _startFallTime = Time.time;
        }
        else if (Time.time >= _startFallTime + fallTime)
        {
            _isFalling = true;
            animator.SetBool(IsFallingID, true);
        }
    }


    private IEnumerator DashCooldown()
    {
        _dashOnCooldown = true;
        yield return new WaitForSeconds(3);
        _dashReloaded = true;
        _dashOnCooldown = false;
    }

    public void JumpPressed(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            _holdJump = false;
        }

        //if (!context.started) return;
        if (context.performed)
        {
            _holdJump = true;
            _jumping = true;
            Jump();
        }
    }

private void Jump()
{
    if (!CheckSlope() || (!IsGrounded() && _numberOfJumps >= maxNumberOfJumps))
    {
        return;
    }
    if (_numberOfJumps == 0) StartCoroutine(WaitForLanding());
    _numberOfJumps++;
    velocity = jumpPower;
    animator.SetTrigger(JumpID);
}

private bool CheckSlope()
{
    if (Physics.Raycast(new Ray(transform.position, Vector3.down), out var hitInfo, 0.2f))
    {
        Debug.Log(hitInfo.normal);
    }
    return true;
}

    private IEnumerator WaitForLanding()
    {
        yield return new WaitUntil(() => !IsGrounded());
        yield return new WaitUntil(IsGrounded);
        _numberOfJumps = 0;
    }

    public void InputMovement(InputAction.CallbackContext context)
    {
        inputMovement = context.ReadValue<Vector2>();
    }

    public void InputLook(InputAction.CallbackContext context)
    {
        inputMovement = context.ReadValue<Vector2>();
    }


    private void ApplyMovement()
    {
        var desiredVelocity = new Vector3(inputMovement.x, 0, inputMovement.y) * maxSpeed;
        var currentVelocity = characterController.velocity;
        var maxSpeedChange = movementLerpSpeed * Time.deltaTime;

        direction.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity.x, maxSpeedChange);
        direction.z = Mathf.MoveTowards(currentVelocity.z, desiredVelocity.z, maxSpeedChange);

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
        {
            return;
        }

        var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }

    public void Fire()
    {
        var newProjectile = Instantiate(projectile);
        var transform1 = transform;
        newProjectile.transform.position = transform1.position + transform1.forward * 0.6f + Vector3.up;
        newProjectile.transform.rotation = transform1.rotation;
        const int size = 1;
        newProjectile.transform.localScale *= size;
        newProjectile.GetComponent<Rigidbody>().mass = Mathf.Pow(size, 3);
        newProjectile.GetComponent<Rigidbody>().AddForce(transform.forward * 20f, ForceMode.Impulse);
        newProjectile.GetComponent<MeshRenderer>().material.color =
            new Color(Random.value, Random.value, Random.value, 1.0f);
    }
}