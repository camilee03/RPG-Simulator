using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    NetworkManager networkManager;

    // Debug
    DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Networking;

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

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
    }

    public void StartGame()
    {
        try 
        { 
            networkManager.SceneManager.LoadScene("Main", LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
        }
    }

    public void ReturnToLobby()
    {
        if (networkManager == null || !networkManager.IsListening)
        {
            // Loading from load -> lobby
            SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        else
        {
            // Loading from main -> lobby (CHANGE, STILL SOME ISSUES)
            try
            {
                if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("[LevelManager] Exiting Game...");

                if (NetworkManager.Singleton.IsHost)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("Load", LoadSceneMode.Single);
                    NetworkManager.Singleton.Shutdown();
                }
                else
                {
                    NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
                    SceneManager.LoadScene("Load", LoadSceneMode.Single);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

        }
    }
}
