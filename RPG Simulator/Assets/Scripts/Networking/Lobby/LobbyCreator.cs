using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreator : MonoBehaviour
{
    [SerializeField] TMP_Text lobbyNameText;
    [SerializeField] TMP_Text maxPlayersText;
    [SerializeField] Toggle isPrivate;
    int maxPlayers = 4;

    private void Start()
    {
        SetText();
    }

    public void CreateLobby()
    {
        LobbyManager.Instance.CreateLobby(lobbyNameText.text, maxPlayers, isPrivate.isOn);
    }

    public void IncreasePlayerSize() { maxPlayers++; SetText(); }
    public void DecreasePlayerSize() { maxPlayers--; SetText(); }
    private void SetText() { maxPlayersText.text = $"{maxPlayers}"; }
}
