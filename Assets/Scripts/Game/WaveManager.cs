using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages wave spawning, game state, and difficulty progression.
/// Only the Zombie Master (first player / lowest PlayerId) runs authoritative logic.
/// All state is networked so every client shows correct HUD values.
/// </summary>
public class WaveManager : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Prefabs")]
    [SerializeField] private NetworkObject   _walkerPrefab;
    [SerializeField] private NetworkObject   _runnerPrefab;
    [SerializeField] private NetworkObject   _tankPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[]     _zombieSpawnPoints;

    [Header("Difficulty")]
    [SerializeField] private int   _baseZombieCount   = 6;
    [SerializeField] private float _spawnInterval     = 0.5f;
    [SerializeField] private float _betweenWaveDelay  = 10f;
    [SerializeField] private int   _runnersFromWave   = 5;
    [SerializeField] private int   _tanksFromWave     = 8;

    // ── Networked state ───────────────────────────────────────────────────────
    [Networked(OnChanged = nameof(OnStateChanged))]
    public GameState State { get; private set; } = GameState.WaitingForPlayers;

    [Networked] public int CurrentWave       { get; private set; }
    [Networked] public int ZombiesRemaining  { get; private set; }

    [Networked]
    private NetworkDictionary<PlayerRef, int> _scores => default;

    // ── Events (local) ────────────────────────────────────────────────────────
    public static event Action<GameState>            OnGameStateChanged;
    public static event Action<int, int>             OnWaveChanged;        // (wave, zombiesTotal)
    public static event Action<PlayerRef, int, int>  OnScoreChanged;       // (player, score, delta)

    // ── Private ───────────────────────────────────────────────────────────────
    private bool _isMaster => HasStateAuthority;

    // ── Fusion lifecycle ──────────────────────────────────────────────────────

    public override void Spawned()
    {
        if (!_isMaster)
            return;

        // Wait for at least one player before starting countdown
        State = GameState.WaitingForPlayers;
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isMaster)
            return;

        if (State == GameState.WaitingForPlayers)
        {
            // ActivePlayers is IEnumerable; LINQ Count() is the correct API here
            if (AreAllPlayersDead() == false && Runner.ActivePlayers.Count() > 0)
                BeginCountdown();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by ZombieHealth when a zombie dies.</summary>
    public void OnZombieDied(PlayerRef killer, int scoreValue)
    {
        if (!_isMaster)
            return;

        ZombiesRemaining = Mathf.Max(0, ZombiesRemaining - 1);

        // Award score
        int prev = _scores.ContainsKey(killer) ? _scores[killer] : 0;
        _scores.Set(killer, prev + scoreValue);
        OnScoreChanged?.Invoke(killer, prev + scoreValue, scoreValue);

        if (ZombiesRemaining <= 0)
            StartCoroutine(BetweenWaves());
    }

    public int GetScore(PlayerRef player) => _scores.ContainsKey(player) ? _scores[player] : 0;

    // ── Internal state machine ────────────────────────────────────────────────

    private void BeginCountdown()
    {
        State = GameState.Countdown;
        StartCoroutine(CountdownThenStart());
    }

    private IEnumerator CountdownThenStart()
    {
        yield return new WaitForSeconds(3f);
        StartNextWave();
    }

    private void StartNextWave()
    {
        CurrentWave++;
        int count = _baseZombieCount + CurrentWave * 3;
        ZombiesRemaining = count;
        State = GameState.InWave;
        OnWaveChanged?.Invoke(CurrentWave, count);
        StartCoroutine(SpawnWave(count));
    }

    private IEnumerator SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!Object.IsValid)
                yield break;

            SpawnZombie();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    private void SpawnZombie()
    {
        if (_zombieSpawnPoints == null || _zombieSpawnPoints.Length == 0)
            return;

        Transform spawnPt = _zombieSpawnPoints[UnityEngine.Random.Range(0, _zombieSpawnPoints.Length)];

        NetworkObject prefab = PickZombiePrefab();
        if (prefab == null)
            return;

        Runner.Spawn(prefab, spawnPt.position, Quaternion.identity);
    }

    private NetworkObject PickZombiePrefab()
    {
        if (CurrentWave >= _tanksFromWave && _tankPrefab != null && UnityEngine.Random.value < 0.1f)
            return _tankPrefab;

        if (CurrentWave >= _runnersFromWave && _runnerPrefab != null && UnityEngine.Random.value < 0.25f)
            return _runnerPrefab;

        return _walkerPrefab;
    }

    private IEnumerator BetweenWaves()
    {
        State = GameState.BetweenWaves;
        yield return new WaitForSeconds(_betweenWaveDelay);

        if (AreAllPlayersDead())
        {
            State = GameState.GameOver;
            return;
        }

        StartNextWave();
    }

    private bool AreAllPlayersDead()
    {
        foreach (PlayerRef p in Runner.ActivePlayers)
        {
            NetworkObject obj = Runner.GetPlayerObject(p);
            if (obj == null)
                continue;

            PlayerHealth ph = obj.GetComponent<PlayerHealth>();
            if (ph != null && !ph.IsDead)
                return false;
        }
        return true;
    }

    // ── Networked property callbacks ──────────────────────────────────────────

    private static void OnStateChanged(Changed<WaveManager> changed)
    {
        OnGameStateChanged?.Invoke(changed.Behaviour.State);
    }
}

public enum GameState
{
    WaitingForPlayers,
    Countdown,
    InWave,
    BetweenWaves,
    GameOver
}
