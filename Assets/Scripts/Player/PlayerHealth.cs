using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages player hit points, damage application, and death/respawn in Fusion Shared Mode.
/// The local player is always the State Authority for their own PlayerHealth.
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private float _respawnDelay = 5f;

    [Networked(OnChanged = nameof(OnHPChanged))]
    public int HP { get; private set; }

    [Networked]
    public NetworkBool IsDead { get; private set; }

    public int MaxHP => _maxHP;

    // Raised on every client so UI / VFX can react
    public static event System.Action<PlayerHealth> OnPlayerDied;
    public static event System.Action<PlayerHealth> OnPlayerRespawned;
    public static event System.Action<PlayerHealth, int> OnPlayerDamaged;

    private PlayerAnimator _playerAnimator;
    private PlayerMovement _playerMovement;

    public override void Spawned()
    {
        _playerAnimator = GetComponent<PlayerAnimator>();
        _playerMovement = GetComponent<PlayerMovement>();

        if (HasStateAuthority)
            HP = _maxHP;
    }

    /// <summary>
    /// Apply damage. Only the State Authority (local player) executes the actual HP reduction.
    /// Other systems (e.g. ZombieController RPC) should call this via RPC on the player's authority.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!HasStateAuthority || IsDead)
            return;

        HP = Mathf.Max(0, HP - amount);

        if (HP == 0)
            Die();
    }

    /// <summary>RPC so the Zombie Master can trigger damage on the owning player's client.</summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int amount)
    {
        TakeDamage(amount);
    }

    private void Die()
    {
        IsDead = true;
        OnPlayerDied?.Invoke(this);
        _playerAnimator?.SetDead(true);

        if (HasStateAuthority)
            StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(_respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        HP = _maxHP;
        IsDead = false;
        _playerAnimator?.SetDead(false);
        OnPlayerRespawned?.Invoke(this);
    }

    /// <summary>
    /// Heal the player. Only the State Authority may heal.
    /// </summary>
    public void Heal(int amount)
    {
        if (!HasStateAuthority || IsDead)
            return;

        HP = Mathf.Min(_maxHP, HP + amount);
    }

    // ── Networked property callbacks ─────────────────────────────────────────

    private static void OnHPChanged(Changed<PlayerHealth> changed)
    {
        int previous = changed.GetPrevious().HP;
        int current  = changed.Behaviour.HP;
        int delta    = previous - current;

        if (delta > 0)
            OnPlayerDamaged?.Invoke(changed.Behaviour, delta);
    }
}
