using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyJoiner : MonoBehaviour
{
    public static LobbyJoiner Instance { get; private set; }


    [SerializeField] TMP_Text lobbyCode;

    [Header("Joined Lobby Info")]
    [SerializeField] TMP_Text lobbyName;
    [SerializeField] GameObject joinedLobbyPanel;
    [SerializeField] GameObject mainLobbyPanel;
    [SerializeField] TMP_Text playerList;
    [SerializeField] Button startButton;
    [SerializeField] TMP_Text waitText;


    private void Awake()
    {
        if (this != Instance && Instance != null) Destroy(this);
        else Instance = this;
    }

    public async void JoinLobby()
    {
        await LobbyManager.Instance.JoinLobbyByCode(lobbyCode.text);
        ConnectedToLobby(false);
    }

    public void ConnectedToLobby(bool isHost)
    {
        joinedLobbyPanel.SetActive(true);
        mainLobbyPanel.SetActive(false);

        if (isHost)
        {
            startButton.gameObject.SetActive(true);
            waitText.gameObject.SetActive(false);
        }
        else
        {
            startButton.gameObject.SetActive(false);
            waitText.gameObject.SetActive(true);
        }

        Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
        if (lobby == null) return;
        lobbyName.text = "Lobby: " + lobby.Name;
        string players = "";
        foreach (var player in lobby.Players)
        {
            string displayName = player?.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_NAME) ? player.Data[LobbyManager.KEY_PLAYER_NAME].Value : player?.Id;
            players += $"{displayName}, ";
        }

        playerList.text = $"Players ({lobby.Players.Count}/{lobby.MaxPlayers}):\n {players}";
    }

    public void StartGame()
    {
        LobbyManager.Instance.StartGame();
    }
}
