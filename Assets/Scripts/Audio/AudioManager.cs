using UnityEngine;

/// <summary>
/// Singleton audio manager. Handles music, positional SFX, and pooled AudioSources.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip   _lobbyMusic;
    [SerializeField] private AudioClip   _gameMusic;
    [SerializeField] private AudioClip   _intenseMusic;
    [SerializeField] private int         _intenseFromWave = 5;

    [Header("SFX")]
    [SerializeField] private AudioSource _uiSFXSource;
    [SerializeField] private AudioClip   _waveStartSFX;
    [SerializeField] private AudioClip   _gameOverSFX;
    [SerializeField] private AudioClip   _playerDeathSFX;

    [Header("Positional SFX Pool")]
    [SerializeField] private int _poolSize = 10;
    [SerializeField] private AudioSource _positionalSourcePrefab;

    private AudioSource[] _pool;
    private int           _poolIndex;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPool();
    }

    private void OnEnable()
    {
        WaveManager.OnWaveChanged      += OnWaveChanged;
        WaveManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveChanged      -= OnWaveChanged;
        WaveManager.OnGameStateChanged -= OnGameStateChanged;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlaySFX(AudioClip clip, Vector3 worldPos)
    {
        if (clip == null)
            return;

        AudioSource src = GetPooledSource();
        src.transform.position = worldPos;
        src.clip               = clip;
        src.Play();
    }

    public void PlayWaveStartSFX()  => _uiSFXSource?.PlayOneShot(_waveStartSFX);
    public void PlayGameOverSFX()   => _uiSFXSource?.PlayOneShot(_gameOverSFX);
    public void PlayPlayerDeathSFX() => _uiSFXSource?.PlayOneShot(_playerDeathSFX);

    public void SetMusicVolume(float volume)
    {
        if (_musicSource != null)
            _musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        foreach (AudioSource src in _pool)
            src.volume = volume;

        if (_uiSFXSource != null)
            _uiSFXSource.volume = volume;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void BuildPool()
    {
        _pool = new AudioSource[_poolSize];
        for (int i = 0; i < _poolSize; i++)
        {
            AudioSource src = _positionalSourcePrefab != null
                ? Instantiate(_positionalSourcePrefab, transform)
                : gameObject.AddComponent<AudioSource>();

            // spatialBlend = 0 keeps audio fully 2D (orthographic camera).
            // Set to 1 if you add 3D spatialization via an Audio Listener on the camera.
            src.spatialBlend = 0f;
            src.playOnAwake  = false;
            _pool[i] = src;
        }
    }

    private AudioSource GetPooledSource()
    {
        // Round-robin – stops oldest playing source if all are busy
        AudioSource src = _pool[_poolIndex % _poolSize];
        if (src.isPlaying)
            src.Stop();

        _poolIndex = (_poolIndex + 1) % _poolSize;
        return src;
    }

    private void OnWaveChanged(int wave, int total)
    {
        if (_musicSource == null)
            return;

        AudioClip desired = wave >= _intenseFromWave ? _intenseMusic : _gameMusic;
        if (desired != null && _musicSource.clip != desired)
        {
            _musicSource.clip = desired;
            _musicSource.Play();
        }
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
            PlayGameOverSFX();
    }
}
