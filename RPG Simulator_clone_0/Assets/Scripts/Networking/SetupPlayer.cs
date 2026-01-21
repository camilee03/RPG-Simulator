using Unity.Netcode;
using UnityEngine;
public class SetupPlayer : MonoBehaviour
{
    public string playerName { get; private set; }
    FirstPersonMovement playerController;

    private async void Start()
    {
        Debug.Log("[SetupPlayer] Setting up...");

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

    public void SaveName()
    {
        PlayerPrefs.SetString("PlayerName", playerName);
    }

    public void ChangeName(string newName)
    {
        playerName = newName;
        SaveName();
    }

    public void OnDestroy()
    {
        LobbyManager.Instance.LeaveLobby();
    }
}
