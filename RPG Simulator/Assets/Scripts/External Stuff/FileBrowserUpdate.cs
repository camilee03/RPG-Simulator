using AnotherFileBrowser;
#if UNITY_STANDALONE_WIN
using AnotherFileBrowser.Windows;
#endif

#if UNITY_STANDALONE_OSX
using AnotherFileBrowser.Mac;
#endif

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FileBrowserUpdate : MonoBehaviour
{
    public static FileBrowserUpdate Instance;
    Sprite tempSprite;

    private void Awake()
    {
        if (this != Instance && Instance != null) Destroy(this);
        else Instance = this;
    }

    public async Task<Sprite> GetSpriteFromImageFileBrowser()
    {
        // Create a new task for completion
        var tcs = new TaskCompletionSource<Sprite>(TaskCreationOptions.RunContinuationsAsynchronously);

        var bp = new BrowserProperties
        {
            filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png",
            filterIndex = 0
        };

        new FileBrowser().OpenFileBrowser(bp, async path =>
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    // User cancelled or no path selected
                    tcs.TrySetResult(null);
                    return;
                }

                // Load image from local path with UWR
                await LoadImage(path);

                // Return the loaded sprite (tempSprite) to awaiter
                tcs.TrySetResult(tempSprite);
            }
            catch (System.Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return await tcs.Task;
    }

    private async Task LoadImage(string path)
    {
        using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path);

        await uwr.SendWebRequest();

        if ((uwr.result == UnityWebRequest.Result.ConnectionError) || (uwr.result == UnityWebRequest.Result.ProtocolError))
        {
            Debug.Log(uwr.error);
            tempSprite = null;
        }
        else
        {
            // Get downloaded texture as sprite
            Texture2D uwrTexture = DownloadHandlerTexture.GetContent(uwr);
            tempSprite = Sprite.Create(uwrTexture, new Rect(0, 0, uwrTexture.width, uwrTexture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
