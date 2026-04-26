using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game HUD. Subscribes to game events and updates UI elements every frame.
/// Only displays information relevant to the local player.
/// </summary>
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private Slider    _healthBar;
    [SerializeField] private TMP_Text  _healthText;

    [Header("Ammo")]
    [SerializeField] private TMP_Text  _ammoText;
    [SerializeField] private Slider    _reloadBar;

    [Header("Wave")]
    [SerializeField] private TMP_Text  _waveText;
    [SerializeField] private TMP_Text  _zombieCountText;

    [Header("Scoreboard")]
    [SerializeField] private Transform _scoreboardParent;
    [SerializeField] private TMP_Text  _scoreRowPrefab;

    [Header("Panels")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _hudPanel;

    // Cached references set after local player spawns
    private PlayerHealth _localHealth;
    private WeaponBase   _localWeapon;
    private WaveManager  _waveManager;

    private readonly Dictionary<PlayerRef, TMP_Text> _scoreRows = new();

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDied      += OnPlayerDied;
        PlayerHealth.OnPlayerRespawned += OnPlayerRespawned;
        WaveManager.OnWaveChanged      += OnWaveChanged;
        WaveManager.OnScoreChanged     += OnScoreChanged;
        WaveManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDied      -= OnPlayerDied;
        PlayerHealth.OnPlayerRespawned -= OnPlayerRespawned;
        WaveManager.OnWaveChanged      -= OnWaveChanged;
        WaveManager.OnScoreChanged     -= OnScoreChanged;
        WaveManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        _waveManager = FindFirstObjectByType<WaveManager>();
        _gameOverPanel?.SetActive(false);
        _reloadBar?.gameObject.SetActive(false);
    }

    private void Update()
    {
        RefreshHealthBar();
        RefreshAmmoDisplay();
        RefreshZombieCount();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called once the local player's NetworkObject is available.</summary>
    public void SetLocalPlayer(PlayerHealth health, WeaponBase weapon)
    {
        _localHealth = health;
        _localWeapon = weapon;
    }

    public void ShowGameOver()
    {
        _hudPanel?.SetActive(false);
        _gameOverPanel?.SetActive(true);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RefreshHealthBar()
    {
        if (_localHealth == null)
            return;

        float ratio = (float)_localHealth.HP / _localHealth.MaxHP;
        if (_healthBar  != null) _healthBar.value  = ratio;
        if (_healthText != null) _healthText.text   = $"{_localHealth.HP}/{_localHealth.MaxHP}";
    }

    private void RefreshAmmoDisplay()
    {
        if (_localWeapon == null || _ammoText == null)
            return;

        _ammoText.text = $"{_localWeapon.CurrentAmmo} / {_localWeapon.ReserveAmmo}";

        bool reloading = _localWeapon.IsReloading;
        _reloadBar?.gameObject.SetActive(reloading);

        if (reloading && _reloadBar != null)
        {
            float elapsed  = _localWeapon.Data.ReloadTime - _localWeapon.ReloadTimer.RemainingTime(FindFirstObjectByType<NetworkRunner>()).GetValueOrDefault();
            _reloadBar.value = Mathf.Clamp01(elapsed / _localWeapon.Data.ReloadTime);
        }
    }

    private void RefreshZombieCount()
    {
        if (_zombieCountText == null || _waveManager == null)
            return;

        _zombieCountText.text = $"Zombies: {_waveManager.ZombiesRemaining}";
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnPlayerDied(PlayerHealth ph)
    {
        // Flash red or show "YOU DIED" overlay (extend as needed)
    }

    private void OnPlayerRespawned(PlayerHealth ph) { }

    private void OnWaveChanged(int wave, int total)
    {
        if (_waveText != null)
            _waveText.text = $"Wave {wave}";
    }

    private void OnScoreChanged(PlayerRef player, int score, int delta)
    {
        if (_scoreboardParent == null || _scoreRowPrefab == null)
            return;

        if (!_scoreRows.TryGetValue(player, out TMP_Text row))
        {
            row = Instantiate(_scoreRowPrefab, _scoreboardParent);
            _scoreRows[player] = row;
        }

        row.text = $"P{player.PlayerId}: {score}";
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
            ShowGameOver();
    }
}
