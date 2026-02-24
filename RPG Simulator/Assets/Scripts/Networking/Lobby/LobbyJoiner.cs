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
    [SerializeField] TMP_Text playerName;


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

    public void LeaveLobby()
    {
        LobbyManager.Instance.LeaveLobby();
        joinedLobbyPanel.SetActive(false);
        mainLobbyPanel.SetActive(true);
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

        // Update player names
        playerName.text = LobbyManager.Instance.GetPlayerName();

        string players = "";
        foreach (var player in lobby.Players)
        {
            string playerInfoStr = player?.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_INFO) ? player.Data[LobbyManager.KEY_PLAYER_INFO].Value : player?.Id;
            string displayName = playerInfoStr.Split(':')[0];
            players += $"{displayName}, ";
        }

        playerList.text = $"Players ({lobby.Players.Count}/{lobby.MaxPlayers}):\n {players}";
        UpdateName();
    }

    public void ChangeName(string newName)
    {
        LobbyManager.Instance.ChangeName(newName);
    }

    private void UpdateName()
    {
       playerName.text = LobbyManager.Instance.GetPlayerName();
    }

    public void StartGame()
    {
        LobbyManager.Instance.StartGame();
    }
}
