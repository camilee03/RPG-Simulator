using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Internal; 
using Unity.Services.Lobbies.Models;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance;

    private GameObject _localPlayerHead;
    private Vector3 lastPlayerHeadPos;

    //private string gameChannelName = "3DLobby";
    private bool isIn3DChannel = false;
    Channel3DProperties player3DProperties = new(256, 32, 4, AudioFadeModel.InverseByDistance);

    string joinedChannel = null;
    string joined3DChannel = null;
    string joinedChatChannel = null;

    [Header("Client Info")]
    private string clientID;
    [SerializeField] private int newVolumeMinusPlus50 = 0;
    public TMP_InputField messageField;

    [Header("Debug Settings")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Vivox;
    private bool shouldLog;

    private void Awake()
    {
        if (this != Instance && Instance != null) Destroy(this);
        else Instance = this;
    }

    private void OnEnable()
    {
        Ticker.OnSlowTick += VivoxTick;
    }
    private void OnDisable()
    {
        Ticker.OnSlowTick -= VivoxTick;
    }

    private void VivoxTick()
    {
        if (joined3DChannel != null && isIn3DChannel)
        {
            UpdatePlayer3DPos();
        }

        if (joinedChannel != null && LobbyManager.Instance.GetJoinedLobby() == null && TestRelay.Instance == null)
        {
            LeaveChannelAsync();
        }
    }

    /// <summary> Initialize Vivox and log in the user. </summary>
    /// <returns></returns>
    public async Task Authenticate()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);

        // Check if the service is actually registered in the internal registry
        if (UnityServices.State == ServicesInitializationState.Initialized && shouldLog)
        {
            Debug.Log("[VivoxManager] UGS Initialized. Checking Vivox...");
        }

        if (shouldLog) Debug.Log("[VivoxManager] Trying to find instance...");

        var registry = CoreRegistry.Instance;
        if (registry == null)
        {
            Debug.LogError("[VivoxManager] Core Registry is null. Something is very wrong with UGS.");
        }
        else
        {
            // This is a way to see if Vivox is even "known" to the system
            try
            {
                var service = CoreRegistry.Instance.GetService<IVivoxService>();
                if (shouldLog) Debug.Log($"Registry search result: " +
                    $"{(service != null ? "[VivoxManager] Found Vivox" : "Vivox NOT in registry. Check your packages. \n If that fails, try deleting the Library and obj folders")}");
            }
            catch
            {
                Debug.Log("[VivoxManager] Vivox service not even registered.");
            }
        }

            int tries = 0;
        while (VivoxService.Instance == null && tries < 50)
        {
            tries++; 
            await Task.Delay(100);
        }

        if (VivoxService.Instance == null)
        {
            if (shouldLog) Debug.Log("[VivoxManager] Could not find Vivox instance!");
            return;
        }

        if (shouldLog) Debug.Log("[VivoxManager] Initializing...");
        await VivoxService.Instance.InitializeAsync();

        VivoxService.Instance.LoggedIn += OnLoggedIn;
        VivoxService.Instance.LoggedOut += OnLoggedOut;
        messageField.onSubmit.AddListener(SendMessageAsync);
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;

        await LoginToVivoxAsync();
    }
    private async Task LoginToVivoxAsync()
    {
        //clientID = (int)NetworkManager.Singleton.LocalClientId;
        clientID = AuthenticationService.Instance.PlayerId;
        LoginOptions options = new()
        {
            DisplayName = "Client" + clientID, //you can probably rename this in GUI by doing it backwards
            EnableTTS = true
        };
        await VivoxService.Instance.LoginAsync(options);

        //join3DChannelAsync();
    }


    #region Vivox Callbacks
    private void OnLoggedIn()
    {
        if (VivoxService.Instance.IsLoggedIn)
        {
            if (shouldLog) Debug.Log("[VivoxManager] Client" + clientID + " Login successful");
        }
        else
        {
            if (shouldLog) Debug.Log("[VivoxManager] Something has gone horribly wrong I am so sorry");
        }
    }
    private void OnLoggedOut()
    {
        isIn3DChannel = false;
        joinedChannel = null;
        VivoxService.Instance.LeaveAllChannelsAsync();
        if (shouldLog) Debug.Log("[VivoxManager] Left from all Channels.");
        VivoxService.Instance.LogoutAsync();
        if (shouldLog) Debug.Log("[VivoxManager] Logged out of Vivox");
    }
    async void LobbyManager_OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        if (shouldLog) Debug.Log("[VivoxManager] Joining Vivox Channel for Lobby: " + e.lobby.LobbyCode + e.lobby.Name);
        await JoinGroupChannelAsync(LobbyManager.Instance.GetJoinedLobby().LobbyCode + LobbyManager.Instance.GetJoinedLobby().Name);
    }
    void LobbyManager_OnLeftLobby(object sender, EventArgs e)
    {
        LeaveChannelAsync();
    }

    #endregion


    #region Join Channels

    public async void JoinEchoChannelAsync(string channelName) //self echo
    {
        if (joinedChannel != null)
        {
            await VivoxService.Instance.LeaveChannelAsync(joinedChannel);
            if (shouldLog) Debug.Log("Vivox: Left " + joinedChannel);
            joinedChannel = null;
        }

        await VivoxService.Instance.JoinEchoChannelAsync(channelName, ChatCapability.TextAndAudio);
        joinedChannel = channelName;
        if (shouldLog) Debug.Log("Vivox: Successfully joined Echo Channel.");
    }

    public async Task JoinPositionalChannelAsync(string channelName)//proxy chat, should only ever be called once
    {
        if (!DebugSettings.Instance.DoVivox) return;

        if (joinedChannel != null)
        {
            await VivoxService.Instance.LeaveChannelAsync(joinedChannel);
            if (shouldLog) Debug.Log("Vivox: Left " + joinedChannel);
            joinedChannel = null;
        }

        await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, player3DProperties);
        joined3DChannel = channelName;
        isIn3DChannel = true;
        if (shouldLog) Debug.Log("Vivox: Successfully joined 3D Channel" + channelName + ".");
    }
    public async Task JoinGlobalTextChannelAsync(string channelName)//global text chat, should only ever be called once
    {
        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextOnly);
        if (shouldLog) Debug.Log("Vivox: Successfully joined global text Channel" + channelName + ".");
        joinedChatChannel = channelName;
    }
    public async Task JoinGroupChannelAsync(string channelName)//global chat
    {
        if (joinedChannel != null && VivoxService.Instance.ActiveChannels.ContainsKey(joinedChannel)) // && VivoxRoom.serverID == null)
        {
            await VivoxService.Instance.LeaveChannelAsync(joinedChannel);
            if (shouldLog) Debug.Log("Vivox: Left " + joinedChannel);
        }

        joinedChannel = null;

        if (shouldLog) Debug.Log("Vivox: Joining Group Channel: " + channelName);
        
        // Make sure the channel name doesn't have any weird characters
        Regex rgx = new("[^a-zA-Z0-9]");
        channelName = rgx.Replace(channelName, "");

        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
        joinedChannel = channelName;
        //isIn3DChannel = false;
        if (shouldLog) Debug.Log("Vivox: Successfully joined Group Channel: " + channelName + ".");
    }

    #endregion


    #region Leave Channels
    public async void LeaveChannelAsync(string channel) //leave specific channel
    {
        string leavingChannel = channel;
        joinedChannel = null;
        if (leavingChannel != null)
        {
            await VivoxService.Instance.LeaveChannelAsync(leavingChannel);
            if (shouldLog) Debug.Log("Vivox: Left " + leavingChannel);
        }
    }
    async void LeaveChannelAsync() //leave current channel
    {
        if (!DebugSettings.Instance.DoVivox) return;

        string leavingChannel = joinedChannel;
        joinedChannel = null;
        if (leavingChannel != null)
        {
            await VivoxService.Instance.LeaveChannelAsync(leavingChannel);
            if (shouldLog) Debug.Log("Vivox: Left " + leavingChannel);
        }
    }

    #endregion


    #region Update Volume and Mute
    public async void DeafenUndeafenChannelsAsync(string deafenedChannel, string undeafenedChannel)//mute and turn off audio from specific channel
    {
        await VivoxService.Instance.SetChannelVolumeAsync(deafenedChannel, -50);
        await VivoxService.Instance.SetChannelVolumeAsync(undeafenedChannel, newVolumeMinusPlus50);
        await UnmuteChannelAsync(undeafenedChannel);
        if (shouldLog) Debug.Log("Vivox: Deafened audio from " + deafenedChannel + ", and undeafened audio from " + undeafenedChannel);
    }
    public async void DeafenChannelAsync(string deafenedChannel)//mute and turn off audio from specific channel
    {
        await VivoxService.Instance.SetChannelVolumeAsync(deafenedChannel, -50);
        if (shouldLog) Debug.Log("[VivoxManager] Deafened audio from " + deafenedChannel);
    }
    public async Task UnmuteChannelAsync(string channel)
    {
        if (!DebugSettings.Instance.DoVivox) return;
        await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, channel); // only transmit mic audio to new channel
        if (shouldLog) Debug.Log("[VivoxManager] only allowing mic audio to feed into " + channel);
    }

    public void TogglePlayerMute(string playerID)
    {
        if (!DebugSettings.Instance.DoVivox) return;

        VivoxParticipant participant = GetVivoxParticipant(playerID);
        if (participant != null)
        {
            if (participant.IsMuted)
            {
                participant.UnmutePlayerLocally();
                if (shouldLog) Debug.Log("[VivoxManager] Unmuted player " + GetPlayerNameFromID(playerID));
            }
            else
            {
                participant.MutePlayerLocally();
                if (shouldLog) Debug.Log("[VivoxManager] Muted player " + GetPlayerNameFromID(playerID));
            }
        }
    }

    public void TogglePlayerDeafen(string playerID)
    {
        if (!DebugSettings.Instance.DoVivox) return;

        VivoxParticipant participant = GetVivoxParticipant(playerID);
        if (participant != null)
        {
            if (participant.LocalVolume == 0)
            {
                // set the volume back to default
                participant.SetLocalVolume(100); // NOTE: change to saved volume from participant's side
                if (shouldLog) Debug.Log("[VivoxManager] Undeafened player " + GetPlayerNameFromID(playerID));
            }
            else
            {
                participant.SetLocalVolume(0);
                if (shouldLog) Debug.Log("[VivoxManager] Deafened player " + GetPlayerNameFromID(playerID));
            }
        }
    }

    public double GetPlayerVoiceActivity(string playerID)
    {
        if (!DebugSettings.Instance.DoVivox) return 0f;

        VivoxParticipant participant = GetVivoxParticipant(playerID);
        if (participant != null)
        {
            return participant.AudioEnergy;
        }
        return 0f;
    }

    public VivoxParticipant GetVivoxParticipant(string playerID)
    {
        if (!DebugSettings.Instance.DoVivox) return null;

        VivoxParticipant participant = VivoxService.Instance.ActiveChannels[joined3DChannel].Where(p => p.PlayerId == playerID).FirstOrDefault();
        return participant;
    }

    public void UpdateVolume()
    {
        VivoxService.Instance.SetChannelVolumeAsync(joinedChannel, newVolumeMinusPlus50);
    }
    void SetLocalPlayerVolume(string id, int volume)
    {
        VivoxService.Instance.ActiveChannels[joined3DChannel].Where(participant => participant.PlayerId == id).First().SetLocalVolume(volume);
    }
    int GetLocalPlayerVolume(string id)
    {
        return VivoxService.Instance.ActiveChannels[joined3DChannel].Where(participant => participant.PlayerId == id).First().LocalVolume;
    }

    #endregion


    #region Chat & UI
    public async void SendMessageAsync(string message)
    {
        if (string.IsNullOrEmpty(message) || joinedChatChannel == null)
        {
            return;
        }

        await VivoxService.Instance.SendChannelTextMessageAsync(joinedChatChannel, message);
        messageField.text = string.Empty;
    }

    public string GetPlayerNameFromID(string id)
    {
        var players = LobbyManager.Instance.GetAllPlayers();

        //Debug.Log("Looking for Player with the id: " + id);
        if (players != null)
        {
            foreach (Player player in players)
            {
                if (player.Id == id)
                {
                    string playerInfoStr = player?.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_INFO) ? player.Data[LobbyManager.KEY_PLAYER_INFO].Value : player?.Id;
                    return playerInfoStr.Split(':')[0];
                }
            }
            if (shouldLog) Debug.Log("Didn't find a corresponding ID.");
            return "Someone";
        }

        if (shouldLog) Debug.Log("PlayerList was empty.");
        return "Someone";
    }

    public string GetPlayerNameFromID(ulong id)
    {
        var players = LobbyManager.Instance.GetAllPlayers();

        //Debug.Log("Looking for Player with the id: " + id);
        if (players != null)
        {
            foreach (Player player in players)
            {
                string playerInfoStr = player?.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_INFO) ? player.Data[LobbyManager.KEY_PLAYER_INFO].Value : player?.Id;
                ulong playerClientID = Convert.ToUInt64(playerInfoStr.Split(':')[2]);
                Debug.Log(playerClientID);
                if (playerClientID == id)
                {
                    return playerInfoStr.Split(':')[0];
                }
            }
            if (shouldLog) Debug.Log("Couldn't find a corresponding ID");
            return "Someone";
        }

        if (shouldLog) Debug.Log("PlayerList was empty.");
        return "Someone";
    }

    public string GetPlayerIDfromClientID(ulong clientID)
    {
        if (LobbyManager.Instance == null) return "";

        var players = LobbyManager.Instance.GetAllPlayers();

        //Debug.Log("Looking for Player with the id: " + id);
        if (players != null)
        {
            foreach (Player player in players)
            {
                string playerInfoStr = player?.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_INFO) ? player.Data[LobbyManager.KEY_PLAYER_INFO].Value : player?.Id;
                if (Convert.ToUInt64(playerInfoStr.Split(':')[2]) == clientID)
                {
                    return player.Id;
                }
            }
        }

        if (shouldLog) Debug.Log("Didn't find a corresponding player name or playerList was empty.");
        return "Someone";
    }

    #endregion

    public void UpdatePlayer3DPos()
    {
        if (_localPlayerHead == null || VivoxService.Instance == null) return;
        VivoxService.Instance.Set3DPosition(_localPlayerHead, joined3DChannel);
        if (_localPlayerHead.transform.position != lastPlayerHeadPos)
        {
            lastPlayerHeadPos = _localPlayerHead.transform.position;
        }
    }
    public void SetPlayerHeadPos(GameObject playerHead)
    {
        if (_localPlayerHead == null)
        {
            _localPlayerHead = playerHead;
            if (shouldLog) Debug.Log("Vivox: Successfully attached to PlayerHead");
        }
    }

    /// <summary>
    /// Send a message that is intended for a single client.
    /// The message is encoded and sent on the currently joined chat channel. Recipients must parse messages
    /// with the "/pm:{recipientId}:{message}" prefix to treat them as private.
    /// </summary>
    /// <param name="recipientId">Target client/player id.</param>
    /// <param name="message">Message text to send.</param>
    public async Task SendMessageToClientAsync(string recipientId, string message)
    {
        if (!DebugSettings.Instance.DoVivox) return;
        if (string.IsNullOrEmpty(recipientId) || string.IsNullOrEmpty(message) || joinedChatChannel == null)
        {
            return;
        }

        // Encode recipient into the payload so other clients can route/display appropriately.
        string payload = $"/pm:{message}";

        if (shouldLog) Debug.Log($"[VivoxManager] Sending private message to {recipientId}: {message}");

        await VivoxService.Instance.SendDirectTextMessageAsync(recipientId, payload);

        if (messageField != null)
        {
            messageField.text = string.Empty;
        }
    }
}
