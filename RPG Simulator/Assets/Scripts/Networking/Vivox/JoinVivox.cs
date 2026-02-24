using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoinVivox : NetworkBehaviour
{
    [Header("Vivox Setup")]
    GameObject vivoxHead;
    public static bool init = false;

    [Header("Debug Settings")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Vivox;
    private bool shouldLog;

    private void Start()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel); 
    }

    public override async void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            if (shouldLog) Debug.Log("[JoinVivox] Joining...");

            NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out NetworkClient client);
            vivoxHead = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject;

            VivoxManager.Instance.SetPlayerHeadPos(vivoxHead);
            if (shouldLog) Debug.Log("[JoinVivox] Test Relay Code:  " + TestRelay.Instance.gameCode);
            await VivoxManager.Instance.JoinPositionalChannelAsync(TestRelay.Instance.gameCode + "Global");
            await VivoxManager.Instance.JoinGlobalTextChannelAsync(TestRelay.Instance.gameCode + "GlobalText");
            await VivoxManager.Instance.UnmuteChannelAsync(TestRelay.Instance.gameCode + "Global");

            // activate all network-necessary components. They should be inactive until vivox is completely set up, ideally in a loading screen.
            if (shouldLog) Debug.Log("[JoinVivox] Initial setup is finished, initializing player controls.");
            init = true;
            VivoxManager.Instance.SendMessageAsync("joined the game.");
        }
    }

}
