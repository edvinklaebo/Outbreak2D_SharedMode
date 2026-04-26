using Fusion;
using UnityEngine;

/// <summary>
/// Provides a read-only view into per-player scores stored in WaveManager.
/// Use ScoreManager.GetScore(playerRef) from UI or other systems.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private WaveManager _waveManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _waveManager = FindFirstObjectByType<WaveManager>();
    }

    public int GetScore(PlayerRef player)
    {
        if (_waveManager == null)
            _waveManager = FindFirstObjectByType<WaveManager>();

        return _waveManager != null ? _waveManager.GetScore(player) : 0;
    }
}
