using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyList : MonoBehaviour
{
    private Dictionary<string, LobbyListItem> lobbies;
    [SerializeField] GameObject lobbyListItemPrefab;
    readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Networking;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lobbies = new();

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyListChanged += HandleLobbyListChanged;
            LobbyManager.Instance.OnLobbyEnded += HandleLobbyEnded;
        }
    }


    private void HandleLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
    {
        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Lobby List Changed. Number of lobbies: " + e.lobbyList.Count);

        if (LobbyManager.Instance.GetJoinedLobby() == null)
        {
            foreach (Lobby lobby in e.lobbyList)
            {
                if (!lobbies.ContainsKey(lobby.Name))
                {
                    GameObject newToggle = Instantiate(lobbyListItemPrefab, this.transform);
                    LobbyListItem lobbyItem = newToggle.GetComponent<LobbyListItem>();
                    lobbyItem.lobby = lobby;
                    lobbies[lobby.Name] = lobbyItem;
                }
                else if (!lobbies[lobby.Name].IsSameLobby(lobby))
                {
                    lobbies[lobby.Name].lobby = lobby;
                    if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"Lobby {lobby.Name} updated");
                }
            }
        }

        else if (LobbyManager.Instance != null && e.lobbyList.Count > 0) 
            LobbyJoiner.Instance.ConnectedToLobby(LobbyManager.Instance.IsLobbyHost());

    }

    private void HandleLobbyEnded(object sender, LobbyManager.LobbyEventArgs e)
    {
        if (lobbies.ContainsKey(e.lobby.Name))
        {
            Destroy(lobbies[e.lobby.Name].gameObject);
            lobbies.Remove(e.lobby.Name);
            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"Lobby {e.lobby.Name} ended and removed from list");
        }
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyListChanged -= HandleLobbyListChanged;
            LobbyManager.Instance.OnLobbyEnded -= HandleLobbyEnded;
        }
    }
}
