using Unity.Netcode;
using UnityEngine;
public class SetupPlayer : MonoBehaviour
{
    public string playerName { get; private set; }

    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Networking;
    private bool shouldLog;

    private async void Start()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);
        if (shouldLog) Debug.Log("[SetupPlayer] Setting up...");

        playerName = "Player";
        GetSavedName();

        await LobbyManager.Instance.Authenticate(playerName);
    }

    private void GetSavedName()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            playerName = PlayerPrefs.GetString("PlayerName");
        }
    }

    public void OnDestroy()
    {
        if (LobbyManager.Instance != null) LobbyManager.Instance.LeaveLobby();
    }
}
