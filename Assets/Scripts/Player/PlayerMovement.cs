using Fusion;
using UnityEngine;

/// <summary>
/// Top-down 2D player movement driven by Photon Fusion Shared Mode input polling.
/// Requires a Rigidbody2D on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 5f;

    [Header("Aim")]
    [SerializeField] private Transform _weaponPivot;

    [Networked] private Vector2 _networkedAimDirection { get; set; } = Vector2.right;

    private Rigidbody2D _rb;

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out PlayerInput input))
            return;

        // Movement
        Vector2 velocity = input.MoveDirection.normalized * _speed;
        _rb.linearVelocity = velocity;

        // Aim direction – only update when the player actually provides aim input
        if (input.AimDirection.sqrMagnitude > 0.01f)
            _networkedAimDirection = input.AimDirection.normalized;
    }

    public override void Render()
    {
        // Rotate the weapon pivot toward the networked aim direction on all clients
        if (_weaponPivot != null && _networkedAimDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(_networkedAimDirection.y, _networkedAimDirection.x) * Mathf.Rad2Deg;
            _weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>
    /// Returns the current networked aim direction (used by weapons to determine fire direction).
    /// </summary>
    public Vector2 AimDirection => _networkedAimDirection;
}
