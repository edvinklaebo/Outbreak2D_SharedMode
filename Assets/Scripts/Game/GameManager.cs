using UnityEngine;

/// <summary>
/// Central coordinator that wires game-wide events together.
/// Add this to a persistent scene GameObject (or the same GO as WaveManager).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        WaveManager.OnGameStateChanged += HandleGameStateChanged;
        PlayerHealth.OnPlayerDied      += HandlePlayerDied;
        PlayerHealth.OnPlayerRespawned += HandlePlayerRespawned;
    }

    private void OnDisable()
    {
        WaveManager.OnGameStateChanged -= HandleGameStateChanged;
        PlayerHealth.OnPlayerDied      -= HandlePlayerDied;
        PlayerHealth.OnPlayerRespawned -= HandlePlayerRespawned;
    }

    private void HandleGameStateChanged(GameState state)
    {
        Debug.Log($"[GameManager] Game state → {state}");

        if (state == GameState.GameOver)
            HUDController.Instance?.ShowGameOver();

        if (state == GameState.Countdown)
            AudioManager.Instance?.PlayWaveStartSFX();
    }

    private void HandlePlayerDied(PlayerHealth ph)
    {
        Debug.Log($"[GameManager] Player died: {ph.name}");
        AudioManager.Instance?.PlayPlayerDeathSFX();
    }

    private void HandlePlayerRespawned(PlayerHealth ph)
    {
        Debug.Log($"[GameManager] Player respawned: {ph.name}");
    }
}
