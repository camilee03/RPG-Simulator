using System.Collections;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Load : MonoBehaviour
{
    bool vivoxSubscribed = false;

    void Start()
    {
        // If VivoxService is already available, subscribe immediately and trigger if already logged in.
        if (VivoxService.Instance != null)
        {
            if (VivoxService.Instance.IsLoggedIn) LoadLobbyScene();
            else
            {
                VivoxService.Instance.LoggedIn += LoadLobbyScene;
                vivoxSubscribed = true;
            }
        }
        else StartCoroutine(WaitForVivoxAndSubscribe(10f));
    }

    IEnumerator WaitForVivoxAndSubscribe(float timeoutSeconds)
    {
        var start = Time.time;
        while (VivoxService.Instance == null && Time.time - start < timeoutSeconds)
        {
            yield return null;
        }

        if (VivoxService.Instance != null)
        {
            if (VivoxService.Instance.IsLoggedIn) LoadLobbyScene();
            else
            {
                VivoxService.Instance.LoggedIn += LoadLobbyScene;
                vivoxSubscribed = true;
            }
        }
        else
        {
            Debug.LogWarning("[Load] VivoxService not available within timeout; skipping automatic lobby load.");
        }
    }

    void LoadLobbyScene()
    {
        LevelManager.Instance.ReturnToLobby();
    }

    private void OnDestroy()
    {
        if (vivoxSubscribed && VivoxService.Instance != null) VivoxService.Instance.LoggedIn -= LoadLobbyScene;
    }
}
