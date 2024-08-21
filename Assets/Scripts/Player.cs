using System;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float minProjectileSpeed = 5f;  // Minimum projectile speed
    [SerializeField] private float maxProjectileSpeed = 5f;  // Maximum projectile speed
    private float lastShotTime; 
    public float cooldownTime = 0.1f;

    private NetworkVariable<Vector2> moveInput = new();
    private NetworkVariable<Color> playerColor = new(Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
      
        if (IsLocalPlayer && inputReader != null)
        {
            inputReader.MoveEvent += OnMove;
            inputReader.ShootEvent += OnShoot;
        }

        // Apply the initial color
        ApplyColor(playerColor.Value);

        // Subscribe to changes in the color variable
        playerColor.OnValueChanged += (oldColor, newColor) =>
        {
            ApplyColor(newColor);
        };
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"Player {OwnerClientId} spawned.");

        if (IsServer)
        {
            AssignUniqueColor();
        }

        if (IsLocalPlayer)
        {
            // Show the health bar after the player has spawned
            GetComponent<Health>().ShowHealthBar();
        }
    }

    private void OnMove(Vector2 input)
    {
        Debug.Log($"Player {OwnerClientId} is moving.");
        MoveServerRpc(input);
    }

    private void OnShoot()
    {
        // Check if enough time has passed since the last shot
        if (Time.time - lastShotTime < cooldownTime)
        {
            Debug.Log($"Player {OwnerClientId} is cooling down, cannot shoot yet.");
            return; // Do nothing if the cooldown has not yet expired
        }

        // Update the time of the last shot
        lastShotTime = Time.time;

        // Calculate the direction towards the mouse position
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 shootDirection = (mousePosition - transform.position).normalized;
        float projectileSpeed = Mathf.Clamp(maxProjectileSpeed, minProjectileSpeed, maxProjectileSpeed);

        Debug.Log($"Player {OwnerClientId} is shooting in direction {shootDirection} with speed {projectileSpeed}.");

        // Sync the projectile across the network
        ShootServerRpc(shootDirection, projectileSpeed);
    }

    private void Update()
    {
        if (IsServer)
        {
            transform.position += (Vector3)moveInput.Value * Time.deltaTime;
        }
        if (IsLocalPlayer && Input.GetMouseButtonDown(0))
        {
            OnShoot();
        }
    }

    private void AssignUniqueColor()
    {
        Color color = ColorManager.GetUniqueColor();
        playerColor.Value = color; // This will automatically propagate to all clients
        Debug.Log($"Server assigned color {color} to player {OwnerClientId}");
    }

    private void ApplyColor(Color color)
    {
        spriteRenderer.color = color;
        Debug.Log($"Applied color {color} to player {OwnerClientId}");
    }

    [ServerRpc]
    private void ShootServerRpc(Vector2 direction, float speed, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Attempting to spawn projectile for player {OwnerClientId} on the server.");

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, speed, OwnerClientId);
            Debug.Log($"Server: Initialized projectile from {OwnerClientId}");
        }
        else
        {
            Debug.LogError("Server: Failed to initialize projectile script.");
        }

        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();  // Ensure the projectile is spawned across the network
            Debug.Log($"Server: Projectile spawned for {OwnerClientId}");
        }
        else
        {
            Debug.LogError("Server: Failed to find NetworkObject on the projectile.");
        }
    }

    [ClientRpc]
    private void ShootClientRpc(Vector2 direction, float speed, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) // Prevent the local owner from shooting twice
        {
            Debug.Log($"Client: Spawning projectile for owner {OwnerClientId}");
            ShootProjectileLocally(direction, speed);
        }
    }

    private void ShootProjectileLocally(Vector2 direction, float speed)
    {
        Debug.Log($"Client: Attempting to locally spawn projectile for player {OwnerClientId}.");

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, speed, OwnerClientId);  // Pass the owner ID
            Debug.Log($"Client: Projectile initialized locally for player {OwnerClientId}");
        }
        else
        {
            Debug.LogError("Client: Failed to initialize projectile script locally.");
        }

        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        if (networkObject != null && IsServer)
        {
            networkObject.Spawn(); // Sync projectile across the network only if it's the server
            Debug.Log($"Client: Projectile spawned locally on server for player {OwnerClientId}");
        }
        else if (networkObject == null)
        {
            Debug.LogError("Client: Failed to find NetworkObject on the projectile locally.");
        }
    }

    [ServerRpc]
    private void MoveServerRpc(Vector2 data)
    {
        moveInput.Value = data;
    }
}
