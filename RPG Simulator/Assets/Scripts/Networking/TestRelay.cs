using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using UnityEngine;

public class TestRelay : MonoBehaviour
{
    public static TestRelay Instance;
    public string gameCode = null;
    NetworkManager networkManager;

    // Debug
    readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Networking;

    private void Awake()
    {
        if (this != Instance && Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            networkManager = NetworkManager.Singleton;

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(LobbyManager.Instance.GetJoinedLobby().MaxPlayers);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            gameCode = joinCode;
            //VivoxRoom.serverID = gameCode;

            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log(joinCode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            networkManager.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            networkManager.StartHost();
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async Task JoinRelay(string joinCode)
    {
        try
        {
            if (networkManager == null) networkManager = NetworkManager.Singleton;

            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Joining Relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            gameCode = joinCode;
            //VivoxRoom.serverID = gameCode;

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            networkManager.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            networkManager.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
