using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoinVivox : NetworkBehaviour
{
    public GameObject vivoxHead;
    public static bool init = false;

    // Debug
    readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Vivox;

    public override async void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            Debug.Log("[JoinVivox] Joining...");

            VivoxManager.Instance.SetPlayerHeadPos(vivoxHead);
            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Test Relay Code:  " + TestRelay.Instance.gameCode);
            await VivoxManager.Instance.JoinPositionalChannelAsync(TestRelay.Instance.gameCode + "Global");
            await VivoxManager.Instance.JoinGlobalTextChannelAsync(TestRelay.Instance.gameCode + "GlobalText");
            await VivoxManager.Instance.UnmuteChannelAsync(TestRelay.Instance.gameCode + "Global");

            // activate all network-necessary components. They should be inactive until vivox is completely set up, ideally in a loading screen.
            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Vivox: Initial setup is finished, initializing player controls.");
            init = true;
            VivoxManager.Instance.SendMessageAsync("joined the game.");

            // Debug test Prefabs
            foreach (var p in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
            {
                Debug.Log($"{p.Prefab.name} → {p.SourcePrefabGlobalObjectIdHash}");
            }
        }
    }

    private void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.J))
        {
            VivoxManager.Instance.JoinEchoChannelAsync("ChannelName");
        }
        */
    }
}
