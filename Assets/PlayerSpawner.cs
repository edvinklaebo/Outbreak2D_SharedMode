using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] private GameObject _player;
    
    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log("PlayerJoined");
        if (player != Runner.LocalPlayer)
            return;

        Debug.Log("Spawning Player: " + player.PlayerId);
        Runner.Spawn(_player);
    }
}
