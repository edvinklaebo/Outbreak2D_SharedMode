using Fusion;
using UnityEngine;

/// <summary>
/// Drives the Animator on the PlayerChar based on movement velocity and aim direction.
/// Uses the networked Rigidbody2D velocity so all clients animate correctly.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimator : NetworkBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    [SerializeField] private Animator _animator;

    private Rigidbody2D _rb;

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void Render()
    {
        if (_animator == null)
            return;

        Vector2 vel = _rb.linearVelocity;
        float speed = vel.magnitude;

        _animator.SetFloat(SpeedHash, speed);

        if (speed > 0.1f)
        {
            _animator.SetFloat(MoveXHash, vel.x);
            _animator.SetFloat(MoveYHash, vel.y);
        }
    }

    public void SetDead(bool isDead)
    {
        if (_animator != null)
            _animator.SetBool(IsDeadHash, isDead);
    }
}
