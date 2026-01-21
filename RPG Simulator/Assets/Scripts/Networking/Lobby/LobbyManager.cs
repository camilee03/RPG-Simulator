using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using NUnit.Framework.Interfaces;
using Unity.Netcode;
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Lobby Keys")]
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_START_GAME = "Start";


    // -- Events -- //
    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyEnded;
    public event EventHandler<EventArgs> OnGameStarted;

    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    // -- Lobby State -- //
    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby, gameLobby;
    bool inLobby;
    private string playerName;

    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Networking;

    private void Awake()
    {
        if (this != Instance && Instance != null) Destroy(this);
        else Instance = this;
    }

    private void Update()
    {
        HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
        inLobby = joinedLobby != null;
    }



    #region Join & Authenticate

    public async Task Authenticate(string playerName)
    {
        this.playerName = playerName;
        InitializationOptions initializationOptions = new();

        var shortSuffix = Guid.NewGuid().ToString("N")[..8]; // 8 hex characters
        var profileForThisInstance = $"{playerName}{shortSuffix}";
        initializationOptions.SetProfile(profileForThisInstance);


        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += async () => {
            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

            RefreshLobbyList();

            if (DebugSettings.Instance.DoVivox) await VivoxManager.Instance.Authenticate();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }
    public Lobby GetGameLobby()
    {
        return gameLobby;
    }

    /// <summary> Creates a lobby with the given name, max players, privacy setting, and game mode. </summary>
    /// <param name="lobbyName"></param>
    /// <param name="maxPlayers"></param>
    /// <param name="isPrivate"></param>
    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
    {
        try
        {
            Player player = GetPlayer();

            CreateLobbyOptions options = new()
            {
                Player = player,
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject> {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Created Lobby " + lobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary> Joins a lobby using a lobby code. </summary>
    /// <param name="lobbyCode"></param>
    public async Task JoinLobbyByCode(string lobbyCode)
    {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
        {
            Player = player
        });

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    /// <summary> Joins a lobby using a lobby ID. </summary>
    /// <param name="lobby"></param>
    public async Task JoinLobby(Lobby lobby)
    {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
        {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Joined Lobby " + lobby.Name);
    }

    public async Task QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    #endregion


    #region Refresh Lobby

    private void HandleRefreshLobbyList()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f)
            {
                float refreshLobbyListTimerMax = 5f;
                refreshLobbyListTimer = refreshLobbyListTimerMax;

                RefreshLobbyList();

                if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Refreshing Lobby List");
            }
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Heartbeat");
                if (gameLobby != null)
                    await LobbyService.Instance.SendHeartbeatPingAsync(gameLobby.Id);
                else await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (joinedLobby != null)
        {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f)
            {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                if (!IsPlayerInLobby())
                {
                    // Player was kicked out of this lobby
                    if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;
                }
                if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    if (DebugSettings.Instance.ShouldLog(logLevel)) {
                        Debug.Log("Server has started game!");
                        Debug.Log($"[LobbyManager] joinedLobby.HostId={joinedLobby.HostId} localPlayerId={AuthenticationService.Instance.PlayerId} IsLobbyHost={IsLobbyHost()}");
                    }

                    if (AuthenticationService.Instance.IsSignedIn && !IsLobbyHost())
                    {
                        // Safety: guard against null or empty value
                        string relayCode = joinedLobby.Data.ContainsKey(KEY_START_GAME) ? joinedLobby.Data[KEY_START_GAME].Value : null;
                        if (!string.IsNullOrEmpty(relayCode)) await TestRelay.Instance.JoinRelay(relayCode);
                        else Debug.LogWarning("[LobbyManager] Start game key was empty when trying to join relay.");
                    }
                    gameLobby = joinedLobby;
                    joinedLobby = null;

                    OnGameStarted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        else if (gameLobby != null)
        {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f)
            {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                gameLobby = await LobbyService.Instance.GetLobbyAsync(gameLobby.Id);
            }
        }
    }


    /// <summary> Refreshes the lobby list by querying open lobbies from the Lobby Service. </summary>
    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new (
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new (
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await LobbyService.Instance.QueryLobbiesAsync();


            if (DebugSettings.Instance.ShouldLog(DebugSettings.LogLevel.Networking)) Debug.Log("Found " + lobbyListQueryResponse.Results.Count + " lobbies");
            OnLobbyListChanged?.Invoke(this, 
                new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    #endregion


    #region Identify Player & Host

    /// <summary> Figures out if the local player is the host of the joined lobby </summary>
    public bool IsLobbyHost()
    {
        // Be defensive: ensure we are signed in and joinedLobby/gameLobby present
        if (!AuthenticationService.Instance.IsSignedIn) return false;

        return (joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId) 
            || (gameLobby != null && gameLobby.HostId == AuthenticationService.Instance.PlayerId);
    }

    /// <summary> Figures out if the given player is the host of the joined lobby </summary>
    public bool IsLobbyHost(Player player)
    {
        // Be defensive: ensure we are signed in and joinedLobby/gameLobby present
        if (!AuthenticationService.Instance.IsSignedIn) return false;

        return (joinedLobby != null && joinedLobby.HostId == player.Id)
            || (gameLobby != null && gameLobby.HostId == player.Id);
    }

    /// <summary> Determines whether the currently authenticated player is a member of the joined lobby. </summary>
    /// <returns><see langword="true"/> if the authenticated player is present in the joined lobby; otherwise, <see
    /// langword="false"/>.</returns>
    private bool IsPlayerInLobby()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary> Returns a Player object representing the currently authenticated player. </summary>
    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
        });
    }

    /// <summary>
    /// Returns all players in the currently joined lobby.
    /// </summary>
    /// <returns></returns>
    public Player[] GetAllPlayers()
    {
        if (joinedLobby != null)
        {
            return joinedLobby.Players.ToArray();
        }
        return Array.Empty<Player>();
    }


    /// <summary> Changes the player's name in the currently joined lobby. </summary>
    /// <param name="playerName"></param>
    public async void UpdatePlayerName(string playerName)
    {
        this.playerName = playerName;

        if (joinedLobby != null)
        {
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }


    #endregion


    #region Lobby Actions

    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                if (joinedLobby.Players.Count == 0)
                {
                    // If the lobby is empty after leaving, delete it
                    OnLobbyEnded?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                }

                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void StartGame()
    {
        if (IsLobbyHost())
        {
            try
            {
                if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("Starting Game");

                string relayCode = await TestRelay.Instance.CreateRelay();

                LevelManager.Instance.StartGame();

                //set the lobby to private so late joiners can still join, but must have the code.
                Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    IsPrivate = true,
                    Data = new Dictionary<string, DataObject>
                    {
                        {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                joinedLobby = lobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    #endregion
}
