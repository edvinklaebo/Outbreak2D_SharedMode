using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Spawns the local player when they join the session, and respawns them on death.
/// Uses pre-placed SpawnPoint objects in the scene; falls back to world origin if none exist.
/// </summary>
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private Transform[]   _spawnPoints;

    // Track spawned players so we can despawn on leave
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    public void PlayerJoined(PlayerRef player)
    {
        if (player != Runner.LocalPlayer)
            return;

        SpawnPlayer(player);
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
            return;

        Runner.Despawn(obj);
        _spawnedPlayers.Remove(player);
    }

    public void SpawnPlayer(PlayerRef player)
    {
        Vector3 spawnPos = PickSpawnPoint();
        NetworkObject playerObj = Runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
        _spawnedPlayers[player] = playerObj;

        // Give the runner a reference to this player's object (used by other systems)
        Runner.SetPlayerObject(player, playerObj);

        Debug.Log($"[PlayerSpawner] Spawned player {player.PlayerId} at {spawnPos}");
    }

    private Vector3 PickSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return Vector3.zero;

        return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
    }
}
