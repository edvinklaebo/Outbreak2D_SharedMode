using Fusion;
using UnityEngine;

/// <summary>
/// Pistol – hitscan weapon using Fusion lag-compensated raycasting.
/// One pellet, straight shot, moderate damage.
/// </summary>
public class Pistol : WeaponBase
{
    [Header("Impact VFX")]
    [SerializeField] private GameObject _impactPrefab;

    protected override void ExecuteFire(Vector2 direction)
    {
        FirePellet(direction);
    }

    private void FirePellet(Vector2 direction)
    {
        if (Runner == null)
            return;

        Vector2 origin = MuzzlePoint != null ? (Vector2)MuzzlePoint.position : (Vector2)transform.position;

        // Lag-compensated raycast so we hit objects as they were when the player pressed fire
        var hitOptions = HitOptions.IncludePhysX | HitOptions.SubtickAccuracy;
        if (Runner.LagCompensation.Raycast(origin, direction, Data.Range, Object.InputAuthority, out LagCompensatedHit hit, options: hitOptions))
        {
            HandleHit(hit, origin);
        }
    }

    private void HandleHit(LagCompensatedHit hit, Vector2 origin)
    {
        // Spawn impact VFX at hit point (local – no networking needed)
        if (_impactPrefab != null)
        {
            GameObject fx = ObjectPool.Instance.Get(_impactPrefab);
            fx.transform.position = hit.Point;
            fx.transform.up = hit.Normal;
        }

        // Apply damage if we hit a zombie
        if (hit.Hitbox != null)
        {
            ZombieHealth zombie = hit.Hitbox.Root.GetComponent<ZombieHealth>();
            if (zombie != null)
            {
                zombie.RPC_TakeDamage(Data.Damage, Object.InputAuthority);
                return;
            }
        }

        // Apply damage if we hit a PhysX collider (e.g. player colliders in some setups)
        if (hit.Collider != null)
        {
            PlayerHealth ph = hit.Collider.GetComponent<PlayerHealth>();
            if (ph != null && ph.Object.InputAuthority != Object.InputAuthority)
                ph.RPC_TakeDamage(Data.Damage);
        }
    }
}
