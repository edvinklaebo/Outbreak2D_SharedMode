using UnityEngine;

/// <summary>
/// ScriptableObject that holds all configuration data for a weapon type.
/// Create via Assets → Create → Outbreak2D → Weapon Data.
/// </summary>
[CreateAssetMenu(menuName = "Outbreak2D/Weapon Data", fileName = "WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string WeaponName = "Pistol";

    [Header("Damage")]
    public int Damage = 25;

    [Header("Ammo")]
    public int MagazineSize    = 12;
    public int MaxReserveAmmo  = 60;

    [Header("Timing")]
    [Tooltip("Shots per second")]
    public float FireRate   = 4f;
    public float ReloadTime = 1.5f;

    [Header("Spread (for multi-pellet weapons)")]
    [Tooltip("Number of raycasts per shot (1 = hitscan, >1 = shotgun)")]
    public int   PelletCount  = 1;
    [Tooltip("Half-angle spread in degrees")]
    public float SpreadAngle  = 0f;

    [Header("Range")]
    public float Range = 30f;

    [Header("Audio")]
    public AudioClip ShootSFX;
    public AudioClip EmptySFX;
    public AudioClip ReloadSFX;
}
