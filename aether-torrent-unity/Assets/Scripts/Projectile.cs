using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Tooltip("Time in seconds before the projectile self-destructs")]
    [SerializeField] private float lifetime = 1f;

    private void Start()
    {
        // Destroy the projectile after lifetime seconds
        Destroy(gameObject, lifetime);
    }
} 