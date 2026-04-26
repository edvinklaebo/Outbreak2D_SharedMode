using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles session startup (Shared Mode), input polling, and connects the top-level
/// Fusion callbacks to the rest of the game. Attach to a persistent GameObject in the
/// LobbyScene or as a standalone launcher prefab.
/// </summary>
public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefabs / References")]
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private NetworkSceneManagerDefault _sceneManager;

    [Header("Session")]
    [SerializeField] private string _defaultRoomName = "OutbreakRoom";
    [SerializeField] private string _gameSceneName   = "GameScene";

    private NetworkRunner _runner;

    // ── Public API ────────────────────────────────────────────────────────────

    public async void LaunchShared(string roomName = null)
    {
        if (_runner != null)
            return;

        _runner = Instantiate(_runnerPrefab);
        _runner.AddCallbacks(this);

        var startArgs = new StartGameArgs
        {
            GameMode      = GameMode.Shared,
            SessionName   = string.IsNullOrEmpty(roomName) ? _defaultRoomName : roomName,
            SceneManager  = _sceneManager ?? _runner.GetComponent<INetworkSceneManager>(),
        };

        var result = await _runner.StartGame(startArgs);
        if (!result.Ok)
        {
            Debug.LogError($"[GameLauncher] StartGame failed: {result.ShutdownReason}");
            return;
        }

        // Load the game scene for everyone
        if (Runner.IsSharedModeMasterClient)
            _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(_gameSceneName)));
    }

    // Convenience property exposed to UI
    public NetworkRunner Runner => _runner;

    // ── INetworkRunnerCallbacks ───────────────────────────────────────────────

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var pi = new PlayerInput();

        pi.MoveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Aim toward mouse cursor in world space, relative to the local player's position
        if (Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            NetworkObject playerObj = runner.GetPlayerObject(runner.LocalPlayer);
            Vector2 origin = playerObj != null ? (Vector2)playerObj.transform.position : Vector2.zero;
            pi.AimDirection = ((Vector2)mouseWorld - origin).normalized;
        }

        pi.Shoot  = Input.GetButton("Fire1");
        pi.Reload = Input.GetKeyDown(KeyCode.R);

        input.Set(pi);
    }

    // Unused callbacks – must be implemented to satisfy the interface
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessage message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
