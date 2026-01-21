using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListItem : MonoBehaviour
{
    public Lobby lobby;

    [SerializeField] private Text lobbyNameText;
    [SerializeField] private Text lobbyPlayersText;


    void Update()
    {
        lobbyNameText.text = lobby.Name;
        lobbyPlayersText.text = $"{lobby.Players.Count} / {lobby.AvailableSlots + lobby.Players.Count}";
    }

    public async void JoinLobby()
    {
        await LobbyManager.Instance.JoinLobby(lobby);

        var isHost = LobbyManager.Instance.IsLobbyHost();
        LobbyJoiner.Instance.ConnectedToLobby(isHost);
    }

    public bool IsSameLobby(Lobby otherLobby)
    {
        return lobby.Name == otherLobby.Name &&
            lobby.Players.Count == otherLobby.Players.Count &&
            lobby.MaxPlayers == otherLobby.MaxPlayers;
    }
}
