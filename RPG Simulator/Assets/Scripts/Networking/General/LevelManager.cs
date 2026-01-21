using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    NetworkManager networkManager;

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
            SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }

        /*
        else
        {
            try
            {
                networkManager.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene: {e.Message}");
            }
        }
        */
    }
}
