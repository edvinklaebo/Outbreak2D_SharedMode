using Fusion;
using UnityEngine;

/// <summary>
/// Manages zombie hit points in Fusion Shared Mode.
/// Only the Zombie Master (State Authority of this object) can reduce HP.
/// All other clients observe the [Networked] HP via an RPC from the Zombie Master.
/// </summary>
public class ZombieHealth : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnHPChanged))]
    public int HP { get; private set; }

    [Networked]
    public NetworkBool IsDead { get; private set; }

    private ZombieData _data;
    private Animator   _animator;

    private static readonly int DeathHash = Animator.StringToHash("Death");

    public void Init(ZombieData data)
    {
        _data = data;
    }

    public override void Spawned()
    {
        _animator = GetComponent<Animator>();

        if (HasStateAuthority && _data != null)
            HP = _data.MaxHP;
    }

    /// <summary>
    /// RPC called by a shooting player. The Zombie Master (State Authority) applies the damage.
    /// The scorer's PlayerRef is forwarded so WaveManager can attribute the kill.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int amount, PlayerRef scorer)
    {
        if (IsDead)
            return;

        HP = Mathf.Max(0, HP - amount);

        if (HP <= 0)
            Die(scorer);
    }

    private void Die(PlayerRef scorer)
    {
        IsDead = true;

        // Notify WaveManager so it can decrement zombie count and award score
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        waveManager?.OnZombieDied(scorer, _data != null ? _data.ScoreValue : 10);

        // Let the animation play then despawn
        StartCoroutine(DespawnAfterAnimation());
    }

    private System.Collections.IEnumerator DespawnAfterAnimation()
    {
        _animator?.SetTrigger(DeathHash);

        // Wait for death animation to finish (approximated by clip length or a fixed delay)
        yield return new WaitForSeconds(1.5f);

        if (HasStateAuthority && Object.IsValid)
            Runner.Despawn(Object);
    }

    // ── Networked property callbacks ─────────────────────────────────────────

    private static void OnHPChanged(Changed<ZombieHealth> changed)
    {
        // Optional: trigger hit VFX on all clients
        if (!changed.Behaviour.IsDead)
            AudioManager.Instance?.PlaySFX(changed.Behaviour._data?.GrowlSFX, changed.Behaviour.transform.position);
    }
}
