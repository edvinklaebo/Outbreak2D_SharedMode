using UnityEngine;
using Fusion;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private CharacterController _controller;
    
    public float PlayerSpeed = 2f;
    
    public override void FixedUpdateNetwork()
    {
        var move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * Runner.DeltaTime * PlayerSpeed;

        _controller.Move(move);
    }
}
