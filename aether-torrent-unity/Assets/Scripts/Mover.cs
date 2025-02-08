using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Mover : MonoBehaviour
{
    public Transform target;
    public NavMeshAgent player;
    private Ray _lastRay;
    public Camera _mainCamera;
    public Animator animator;
    private static readonly int ChrSpeed = Animator.StringToHash("chrSpeed");

    private void Start()
    {
        player = GetComponent<NavMeshAgent>();
        //_mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            MoveToCursor();
        }
        UpdateAnimator();
    }

    private void MoveToCursor()
    {
        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        var hasHit = Physics.Raycast(ray, out var hit);
        if (hasHit)
        {
            player.destination = hit.point;
        }
    }

    private void UpdateAnimator()
    {
        var velocity = player.velocity;
        var localVelocity = transform.InverseTransformDirection(velocity);
        var speed = localVelocity.z;
        animator.SetFloat(ChrSpeed, speed);
    }
}