using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Game-Over / Victory screen. Shows final wave, per-player scores, and navigation buttons.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Summary")]
    [SerializeField] private TMP_Text  _finalWaveText;
    [SerializeField] private Transform _scoreboardParent;
    [SerializeField] private TMP_Text  _scoreRowPrefab;

    [Header("Buttons")]
    [SerializeField] private Button    _playAgainButton;
    [SerializeField] private Button    _mainMenuButton;

    private WaveManager _waveManager;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        WaveManager.OnGameStateChanged += HandleGameState;
    }

    private void OnDisable()
    {
        WaveManager.OnGameStateChanged -= HandleGameState;
    }

    private void Start()
    {
        _waveManager = FindFirstObjectByType<WaveManager>();
        _playAgainButton?.onClick.AddListener(OnPlayAgain);
        _mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    private void HandleGameState(GameState state)
    {
        if (state != GameState.GameOver)
            return;

        gameObject.SetActive(true);
        PopulateScoreboard();
    }

    private void PopulateScoreboard()
    {
        if (_waveManager == null || _scoreboardParent == null || _scoreRowPrefab == null)
            return;

        if (_finalWaveText != null)
            _finalWaveText.text = $"You survived to Wave {_waveManager.CurrentWave}!";

        // Clear old rows
        foreach (Transform child in _scoreboardParent)
            Destroy(child.gameObject);

        // Add a row per player
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
            return;

        foreach (PlayerRef p in runner.ActivePlayers)
        {
            int score = _waveManager.GetScore(p);
            TMP_Text row = Instantiate(_scoreRowPrefab, _scoreboardParent);
            row.text = $"Player {p.PlayerId}: {score} pts";
        }
    }

    private void OnPlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
