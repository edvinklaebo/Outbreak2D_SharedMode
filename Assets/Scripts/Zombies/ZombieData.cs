using UnityEngine;

/// <summary>
/// ScriptableObject that defines a zombie variant's stats and behaviour parameters.
/// Create via Assets → Create → Outbreak2D → Zombie Data.
/// </summary>
[CreateAssetMenu(menuName = "Outbreak2D/Zombie Data", fileName = "ZombieData")]
public class ZombieData : ScriptableObject
{
    [Header("Identity")]
    public string ZombieName = "Walker";

    [Header("Stats")]
    public int   MaxHP        = 60;
    public float MoveSpeed    = 2.5f;
    public int   MeleeDamage  = 10;
    public float AttackRate   = 1f;    // attacks per second
    public float AttackRange  = 0.8f;

    [Header("Scoring")]
    public int ScoreValue = 10;

    [Header("Ranged (optional – leave 0 to disable)")]
    public float RangedAttackRange  = 0f;
    public int   RangedDamage       = 0;
    public float RangedAttackRate   = 0f;
    public GameObject ProjectilePrefab;

    [Header("Audio")]
    public AudioClip GrowlSFX;
    public AudioClip AttackSFX;
    public AudioClip DeathSFX;
}
