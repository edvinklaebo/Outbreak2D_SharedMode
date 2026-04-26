using Fusion;
using UnityEngine;

/// <summary>
/// Bridges the local player's active weapon with the shooting input.
/// Attach to the same GameObject as the PlayerMovement / PlayerHealth.
/// Called from FixedUpdateNetwork after input is polled.
/// </summary>
public class PlayerShooter : NetworkBehaviour
{
    [SerializeField] private WeaponBase _equippedWeapon;

    public WeaponBase EquippedWeapon => _equippedWeapon;

    public override void FixedUpdateNetwork()
    {
        if (_equippedWeapon == null)
            return;

        if (!GetInput(out PlayerInput input))
            return;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        Vector2 aimDir = movement != null ? movement.AimDirection : Vector2.right;

        if (input.Shoot)
            _equippedWeapon.TryFire(aimDir);

        if (input.Reload)
            _equippedWeapon.TryReload();
    }

    /// <summary>Equip a new weapon at runtime (e.g. after picking up a drop).</summary>
    public void Equip(WeaponBase weapon)
    {
        _equippedWeapon = weapon;
    }
}
