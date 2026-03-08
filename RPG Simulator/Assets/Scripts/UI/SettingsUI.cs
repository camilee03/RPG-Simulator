using TMPro;
using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] TMP_InputField gameCodeText;
    private void UpdateGameCode()
    {
        gameCodeText.text = "Lobby Code: " + LobbyManager.Instance.GetJoinedLobby().LobbyCode;
    }
}
