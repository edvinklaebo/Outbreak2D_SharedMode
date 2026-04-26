using Fusion;
using UnityEngine;

/// <summary>
/// Network input struct – collected each tick by the local player and sent to the server.
/// </summary>
public struct PlayerInput : INetworkInput
{
    public Vector2 MoveDirection;
    public Vector2 AimDirection;
    public NetworkBool Shoot;
    public NetworkBool Reload;
}
