using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float lifetime = 5f;  // Time after which the projectile is destroyed if it doesn't hit anything
    private Rigidbody2D rb;
    private ulong ownerId;  // Store the ID of the player who fired the projectile
    private Collider2D collider2D; // Reference to the Collider2D component

    private void Awake()
    {
        // Ensure Rigidbody2D and Collider2D are initialized as early as possible
        rb = GetComponent<Rigidbody2D>();
        collider2D = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError("No Rigidbody2D found on the projectile.");
        }

        if (collider2D == null)
        {
            Debug.LogError("No Collider2D found on the projectile.");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(SelfDestruct());
    }

    public void Initialize(Vector2 direction, float speed, ulong ownerId)
    {
        // Set the projectile's velocity based on the direction and speed
        rb.velocity = direction.normalized * speed;

        // Store the owner's ID
        this.ownerId = ownerId;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer)
        {
            // Check if the other object has a Health component
            Health health = other.GetComponent<Health>();
            if (health != null && health.OwnerId != ownerId)
            {
                // Inflict damage only if the projectile didn't come from the player itself
                health.TakeDamage(10);  // Example damage value
                Debug.Log($"Projectile hit {other.gameObject.name}, owned by {health.OwnerId}. Damage applied.");

                // Destroy the projectile
                Destroy(gameObject);
            }
        }
    }


    private void DestroyProjectile()
    {
        if (gameObject != null)
        {
            Debug.Log("Destroying projectile");
            Destroy(gameObject);
        }
    }

    private IEnumerator SelfDestruct()
    {
        yield return new WaitForSeconds(lifetime);
        DestroyProjectile();
    }
}
