using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;


public class PlayersManager : NetworkBehaviour
{
    List<PlayerInfo> currentPlayers;
    [SerializeField] GameObject namePrefab;
    NetworkManager networkManager;

    private void OnConnectedToServer()
    {
        Debug.Log("[PlayersManager] Connected to server");
        networkManager = NetworkManager.Singleton;
        //InitializePlayerList();
    }

    void InitializePlayerList()
    {
        IReadOnlyDictionary<ulong, NetworkClient> players = networkManager.ConnectedClients;

        foreach (var kvp in players)
        {
            if (networkManager.LocalClientId == kvp.Key) continue;

            NetworkObject playerObject = kvp.Value.PlayerObject;

            PlayerInfo newPlayer = new()
            {
                playername = playerObject.GetComponent<SetupPlayer>().playerName,
                realname = "",
                clientID = kvp.Key,
                playerObject = playerObject,
            };

            AddPlayer(newPlayer);
            currentPlayers.Add(newPlayer);
        }
    }

    public void AddPlayer(PlayerInfo newPlayer)
    {
        currentPlayers.Add(newPlayer);
        GameObject newName = Instantiate(namePrefab, this.transform);
        ControlPlayerInteraction interaction = newName.GetComponent<ControlPlayerInteraction>();
        interaction.InitializeUI(newPlayer);
    }
}

public class PlayerInfo
{
    public string playername;
    public string realname;
    public ulong clientID;
    public NetworkObject playerObject;
}
