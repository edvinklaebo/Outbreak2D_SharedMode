using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI controller for zombies. Only executes movement/attack logic on the Zombie Master client
/// (State Authority). Other clients interpolate positions via NetworkRigidbody2D.
/// 
/// States: Idle → Chase → Attack → Dead
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : NetworkBehaviour
{
    // ── Serialized ────────────────────────────────────────────────────────────
    [SerializeField] private ZombieData _data;
    [SerializeField] private Animator   _animator;

    // ── Networked state ───────────────────────────────────────────────────────
    [Networked] private PlayerRef     _targetPlayerRef  { get; set; }
    [Networked] private ZombieState   _state            { get; set; }

    // ── Private fields ────────────────────────────────────────────────────────
    private NavMeshAgent  _agent;
    private ZombieHealth  _health;
    private float         _attackCooldown;
    private int           _retargetTick;
    private const int     RetargetEveryTicks = 30;  // ~0.5 s at 60 tick rate
    private const float   StoppingDistanceFactor = 0.9f; // stop slightly before melee range

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private enum ZombieState { Idle, Chase, Attack, Dead }

    // ── Fusion callbacks ──────────────────────────────────────────────────────

    public override void Spawned()
    {
        _health = GetComponent<ZombieHealth>();
        _health.Init(_data);

        _agent = GetComponent<NavMeshAgent>();
        // Configure NavMeshAgent for 2D top-down (XY plane)
        _agent.updateRotation  = false;
        _agent.updateUpAxis    = false;
        _agent.speed           = _data != null ? _data.MoveSpeed : 2.5f;
        _agent.stoppingDistance = _data != null ? _data.AttackRange * StoppingDistanceFactor : 0.7f;
    }

    public override void FixedUpdateNetwork()
    {
        // Only the Zombie Master runs AI logic
        if (!HasStateAuthority)
            return;

        if (_health.IsDead)
        {
            _state = ZombieState.Dead;
            _agent.isStopped = true;
            return;
        }

        // Periodically re-select the nearest living player
        if (Runner.Tick - _retargetTick >= RetargetEveryTicks)
        {
            _retargetTick = Runner.Tick;
            _targetPlayerRef = FindNearestLivingPlayer();
        }

        GameObject target = GetTargetObject();

        if (target == null)
        {
            _state = ZombieState.Idle;
            _agent.isStopped = true;
            return;
        }

        float dist = Vector2.Distance(transform.position, target.transform.position);
        float attackRange = _data != null ? _data.AttackRange : 0.8f;

        if (dist <= attackRange)
        {
            _state = ZombieState.Attack;
            _agent.isStopped = true;
            TryMeleeAttack(target);
        }
        else
        {
            _state = ZombieState.Chase;
            _agent.isStopped = false;
            _agent.SetDestination(target.transform.position);
        }

        // Ranged attack
        if (_data != null && _data.RangedAttackRange > 0f && dist <= _data.RangedAttackRange)
            TryRangedAttack(target);
    }

    public override void Render()
    {
        if (_animator == null)
            return;

        float speed = _agent != null ? _agent.velocity.magnitude : 0f;
        _animator.SetFloat(SpeedHash, speed);
    }

    // ── AI helpers ────────────────────────────────────────────────────────────

    private PlayerRef FindNearestLivingPlayer()
    {
        PlayerRef best     = default;
        float     bestDist = float.MaxValue;

        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            NetworkObject obj = Runner.GetPlayerObject(p);
            if (obj == null)
                continue;

            PlayerHealth ph = obj.GetComponent<PlayerHealth>();
            if (ph == null || ph.IsDead)
                continue;

            float d = Vector2.Distance(transform.position, obj.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best     = p;
            }
        }

        return best;
    }

    private GameObject GetTargetObject()
    {
        if (_targetPlayerRef == default)
            return null;

        NetworkObject obj = Runner.GetPlayerObject(_targetPlayerRef);
        return obj != null ? obj.gameObject : null;
    }

    private void TryMeleeAttack(GameObject target)
    {
        if (_data == null)
            return;

        _attackCooldown -= Runner.DeltaTime;
        if (_attackCooldown > 0f)
            return;

        _attackCooldown = 1f / Mathf.Max(0.01f, _data.AttackRate);

        PlayerHealth ph = target.GetComponent<PlayerHealth>();
        ph?.RPC_TakeDamage(_data.MeleeDamage);

        if (_data.AttackSFX != null)
            AudioManager.Instance?.PlaySFX(_data.AttackSFX, transform.position);
    }

    private void TryRangedAttack(GameObject target)
    {
        // Placeholder – implement projectile spawning for Spitter variant
    }
}
