using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lobby / Main Menu UI. Drives session creation through GameLauncher.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameLauncher _launcher;

    [Header("Input")]
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private TMP_InputField _playerNameInput;

    [Header("Buttons")]
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _quitButton;

    [Header("Status")]
    [SerializeField] private TMP_Text _statusText;

    private void Start()
    {
        _joinButton?.onClick.AddListener(OnJoinClicked);
        _quitButton?.onClick.AddListener(Application.Quit);
        SetStatus("Enter a room name and press Join.");
    }

    private void OnJoinClicked()
    {
        string room = _roomNameInput != null && !string.IsNullOrWhiteSpace(_roomNameInput.text)
            ? _roomNameInput.text.Trim()
            : "OutbreakRoom";

        SetStatus($"Connecting to room: {room}…");
        _joinButton.interactable = false;
        _launcher.LaunchShared(room);
    }

    private void SetStatus(string msg)
    {
        if (_statusText != null)
            _statusText.text = msg;
    }
}
