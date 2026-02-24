using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class VivoxPlayer : NetworkBehaviour
{
    PlayerInfo playerInfo;
    bool isInitialized = false;

    [Header("UI References")]
    [SerializeField] Image playerIcon;
    [SerializeField] Toggle muteToggle;
    [SerializeField] Toggle hearToggle;
    [SerializeField] TMP_Text playerNameText;

    [Header("Object References")]
    [SerializeField] NetworkObject playerObject;

    private void OnEnable() { if (!DebugSettings.Instance.OfflineTesting) Ticker.OnSlowTick += UpdateUI; }
    private void OnDisable() { if (!DebugSettings.Instance.OfflineTesting) Ticker.OnSlowTick -= UpdateUI; }
    public void UpdateUI()
    {
        if (!isInitialized)
        {
            Debug.Log("[VivoxPlayer] Player info not initialized for player: " + playerObject.OwnerClientId);
            RequestPlayerInfoForOwnerServerRpc(playerObject.OwnerClientId);
        }

        if (IsOwner && isInitialized && playerInfo.playerIcon != null) playerIcon.sprite = playerInfo.playerIcon.sprite;
        if (IsOwner || !isInitialized) return;
        Debug.Log("[VivoxPlayer] Updating UI for player: " + playerObject.OwnerClientId);

        playerNameText.text = playerInfo.playerName;
        muteToggle.isOn = false;
        hearToggle.isOn = true;

        if (playerInfo.playerIcon != null) playerIcon.sprite = playerInfo.playerIcon.sprite;
    }


    #region Transfer Player Info
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        Debug.Log("[VivoxPlayer] Updating player info for player: " + playerObject.OwnerClientId);

        PlayerInfo newInfo = LobbyManager.Instance.GetPlayerInfo();
        SetLocalPlayerInfo(newInfo);
        InitializeServerRPC(newInfo);
    }

    [ClientRpc]
    private void InitializeClientRPC(PlayerInfo newInfo)
    {
        SetLocalPlayerInfo(newInfo);
    }

    [ServerRpc]
    private void InitializeServerRPC(PlayerInfo newInfo)
    {
        SetLocalPlayerInfo(newInfo);
        InitializeClientRPC(newInfo);
    }


    // Any client -> Server: request that the server ask the owner client to re-send its info.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlayerInfoForOwnerServerRpc(ulong requestedOwnerClientId)
    {
        // Target the owner client with a ClientRpc asking them to re-send their info.
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { requestedOwnerClientId }
            }
        };

        AskOwnerToResendClientRpc(clientRpcParams);
    }

    // Server -> Owner client (targeted): asks the owner to re-send their info via SendPlayerInfoServerRpc
    [ClientRpc]
    private void AskOwnerToResendClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // This runs only on the owner client (because server targeted it).
        if (!IsOwner) return;

        var local = LobbyManager.Instance.GetPlayerInfo();
        // Send up to server which will rebroadcast to all.
        InitializeServerRPC(local);
    }

    private void SetLocalPlayerInfo(PlayerInfo newInfo)
    {
        playerInfo = newInfo;
        isInitialized = true;
    }

    #endregion


    #region Player Actions

    /// <summary>
    /// Turns on and off the local player's ability to hear the client. (Mute/unmute)
    /// </summary>
    public void TogglePlayerSpeaker()
    {
        VivoxManager.Instance.TogglePlayerMute(playerInfo.playerID);
        muteToggle.isOn = !muteToggle.isOn;
    }

    // Can't be changed by local player, only by audio (Defean/Undeafen)
    public void TogglePlayerAudio()
    {
        VivoxManager.Instance.TogglePlayerDeafen(playerInfo.playerID);
        hearToggle.isOn = !hearToggle.isOn;
    }

    public void RunToPlayer()
    {

    }

    public void MessagePlayer()
    {

    }

    #endregion
}

public class PlayerInfo : IEquatable<PlayerInfo>, INetworkSerializable
{
    public string playerName;
    public string playerID; // vivox ID
    public ulong clientID; // netcode client ID
    public Image playerIcon;

    bool IEquatable<PlayerInfo>.Equals(PlayerInfo other)
    {
        return other != null &&
            other.clientID == clientID && 
            other.playerName == playerName &&
            other.playerIcon == playerIcon;
    }

    public override string ToString()
    {
        return $"{playerName}:{playerID}:{clientID}";
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(playerName) && clientID == 0 && playerIcon == null;
    }

    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        // serialize primitive fields
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerID);
        serializer.SerializeValue(ref clientID);


        // -- Custom serialization for Image asset via GUID -- //

        string iconID = string.Empty;

        // When writing, produce the GUID from the Image asset path
        if (serializer.IsWriter)
        {
            //if (playerIcon != null) SerializationExtensions.WriteValueSafe(serializer.GetFastBufferWriter(), in playerIcon);
           // else iconID = string.Empty;
        }

        // serialize the GUID string (will write or populate when reading)
        serializer.SerializeValue(ref iconID);

        // When reading, resolve the GUID back to an asset path and load the Image
        if (serializer.IsReader)
        {
            //if (!string.IsNullOrEmpty(iconID)) SerializationExtensions.ReadValueSafe(serializer.GetFastBufferReader(), out playerIcon);
           // else playerIcon = null;
        }
    }
}

/*
public static class SerializationExtensions
{
    public static void ReadValueSafe(this FastBufferReader reader, out Image image)
    {
        reader.ReadValueSafe(out string val);
        //image = UnityEditor.AssetDatabase.LoadAssetAtPath<Image>(val);
    }

    public static void WriteValueSafe(this FastBufferWriter writer, in Image image)
    {
        // Source - https://stackoverflow.com/a
        // Posted by Steveboy001
        // Retrieved 2026-01-27, License - CC BY-SA 4.0
        //string id = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(image));

        writer.WriteValueSafe(id);
    }
}
*/