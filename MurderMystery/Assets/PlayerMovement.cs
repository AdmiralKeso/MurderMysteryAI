using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // Only the owning client simulates physics for its own player;
        // remote instances just play back the NetworkTransform-replicated pose.
        rb.isKinematic = !IsOwner;
    }

    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
        float vertical = Input.GetAxisRaw("Vertical");     // W/S

        moveInput = new Vector3(horizontal, 0f, vertical).normalized;
    }

    void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        Vector3 movement = Time.fixedDeltaTime * moveSpeed * moveInput;
        rb.MovePosition(rb.position + movement);
    }
}
