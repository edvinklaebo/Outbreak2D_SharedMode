using Fusion;
using UnityEngine;

/// <summary>
/// Abstract base for all weapons. Handles ammo tracking, fire-rate cooldown, and reload logic
/// via Photon Fusion networked state (Shared Mode).
/// </summary>
public abstract class WeaponBase : NetworkBehaviour
{
    [SerializeField] protected WeaponData _data;
    [SerializeField] private   Transform  _muzzlePoint;
    [SerializeField] private   GameObject _muzzleFlashPrefab;

    [Networked] public int          CurrentAmmo   { get; protected set; }
    [Networked] public int          ReserveAmmo   { get; protected set; }
    [Networked] public TickTimer    FireCooldown  { get; private set; }
    [Networked] public NetworkBool  IsReloading   { get; private set; }
    [Networked] public TickTimer    ReloadTimer   { get; private set; }

    public WeaponData Data => _data;
    public Transform  MuzzlePoint => _muzzlePoint;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentAmmo  = _data.MagazineSize;
            ReserveAmmo  = _data.MaxReserveAmmo;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Finish reload when timer expires
        if (IsReloading && ReloadTimer.Expired(Runner))
        {
            int needed  = _data.MagazineSize - CurrentAmmo;
            int refill  = Mathf.Min(needed, ReserveAmmo);
            CurrentAmmo += refill;
            ReserveAmmo -= refill;
            IsReloading  = false;
        }
    }

    /// <summary>
    /// Attempt to fire in the given world-space direction.
    /// Returns true if a shot was fired.
    /// </summary>
    public bool TryFire(Vector2 direction)
    {
        if (!HasStateAuthority)
            return false;

        if (IsReloading)
            return false;

        if (!FireCooldown.ExpiredOrNotRunning(Runner))
            return false;

        if (CurrentAmmo <= 0)
        {
            PlayEmptySFX();
            return false;
        }

        CurrentAmmo--;
        float interval = 1f / Mathf.Max(0.01f, _data.FireRate);
        FireCooldown = TickTimer.CreateFromSeconds(Runner, interval);

        ExecuteFire(direction);
        SpawnMuzzleFlash();
        PlayShootSFX();
        return true;
    }

    /// <summary>
    /// Subclasses implement the actual damage/ray logic here.
    /// </summary>
    protected abstract void ExecuteFire(Vector2 direction);

    public void TryReload()
    {
        if (!HasStateAuthority)
            return;

        if (IsReloading || CurrentAmmo == _data.MagazineSize || ReserveAmmo <= 0)
            return;

        IsReloading = true;
        ReloadTimer = TickTimer.CreateFromSeconds(Runner, _data.ReloadTime);
        PlayReloadSFX();
    }

    // ── VFX / Audio helpers ──────────────────────────────────────────────────

    private void SpawnMuzzleFlash()
    {
        if (_muzzleFlashPrefab == null || _muzzlePoint == null)
            return;

        // Muzzle flash is purely local — no need to network it
        GameObject fx = ObjectPool.Instance.Get(_muzzleFlashPrefab);
        fx.transform.SetPositionAndRotation(_muzzlePoint.position, _muzzlePoint.rotation);
    }

    private void PlayShootSFX()
    {
        if (_data.ShootSFX != null)
            AudioManager.Instance?.PlaySFX(_data.ShootSFX, _muzzlePoint ? _muzzlePoint.position : transform.position);
    }

    private void PlayEmptySFX()
    {
        if (_data.EmptySFX != null)
            AudioManager.Instance?.PlaySFX(_data.EmptySFX, transform.position);
    }

    private void PlayReloadSFX()
    {
        if (_data.ReloadSFX != null)
            AudioManager.Instance?.PlaySFX(_data.ReloadSFX, transform.position);
    }
}
