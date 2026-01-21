using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class StreamVideo : NetworkBehaviour
{
    WebCamTexture webCam;
    Renderer rend;
    [SerializeField] Material playerColor;
    bool isInitialized = false;

    [Header("Capture Settings")]

    /// The interval in seconds between frame captures. Reducing this lowers bandwidth usage but also reduces frame rate.
    [SerializeField] float captureInterval = 0.2f;
    /// Size of the requested webcam texture (width and height). Lowering this reduces bandwidth usage but also reduces image quality.
    [SerializeField] int requestedSize = 64;

    /// JPG quality for encoding frames (10-90). Lowering this reduces bandwidth usage but also reduces image quality.
    [SerializeField, Range(10, 90)] int jpgQuality = 10;

    [Header("Delta Frame Detection Settings")]
    /// <summary> The percentage of pixels that must change to send a new frame. Lowering this increases bandwidth usage but also increases responsiveness.</summary>
    [SerializeField, Range(0f, 100f)] float minChangePercent = 2f;
    /// The per-channel difference threshold to consider a pixel changed. Lowering this increases bandwidth usage but also increases responsiveness.
    [SerializeField, Range(0, 255)] int pixelDiffThreshold = 15;
    /// Maximum number of frames to skip before forcing a keyframe send. Lowering this increases bandwidth usage but also increases responsiveness.
    [SerializeField] int maxFramesBetweenKeyframes = 30;

    Coroutine captureCoroutine;
    Texture2D receivedTexture;

    // Reused to avoid allocations every frame
    Texture2D captureTexture;
    Color32[] pixelBuffer;
    Color32[] prevPixelBuffer;
    int framesSinceLastSend = 0;
    bool firstFrameSent = false;

    // Debug
    readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Video;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        Application.targetFrameRate = 100;
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(WaitToConnect());
    }

    public override void OnNetworkDespawn()
    {
        StopCapture();
        if (webCam != null && webCam.isPlaying) webCam.Stop();
        if (receivedTexture != null) Destroy(receivedTexture);
        if (captureTexture != null) Destroy(captureTexture);

        // release buffers
        pixelBuffer = null;
        prevPixelBuffer = null;
    }

    private void Update()
    {
        if (isInitialized && webCam != null)
        {
            if (DebugSettings.Instance.ShouldLog(logLevel)) { Debug.Log($"[StreamVideo] [Update] Is webCam playing? {webCam.isPlaying}"); }
        }
    }

    public void UpdatePlayerColor(Color color)
    {
        playerColor.SetColor("Color", color);
    }

    void UpdateMaterials()
    {
        if (!IsOwner) return;

        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [UpdateMaterials] Starting...");

        // Properly set the renderer material for the body to the playerColor material
        if (rend.materials.Length > 0 && playerColor != null)
        {
            rend.materials[0] = playerColor;
        }

        // Set webcam
        webCam = new WebCamTexture()
        {
            wrapMode = TextureWrapMode.Clamp,
            requestedWidth = requestedSize,
            requestedHeight = requestedSize,
        };

        if (rend.materials[1] != null)
        {
            rend.materials[1].color = Color.white;
            rend.materials[1].mainTexture = webCam;
        }

        webCam.Play();
    }

    IEnumerator WaitToConnect()
    {
        yield return new WaitForSeconds(1f);

        // Initialize placeholders for non-owners
        if (!IsOwner)
        {
            if (rend.materials[1] != null) rend.materials[1].color = Color.gray;
        }

        if (IsOwner)
        {
            if (DebugSettings.Instance.ShouldLog(logLevel)) { Debug.Log($"[StreamVideo] [WaitToConnect] Initializing Owner. IsHost: {IsHost}"); }

            UpdateMaterials();

            // Start capture coroutine to send frames to server
            captureCoroutine = StartCoroutine(CaptureAndSendCoroutine());
            isInitialized = true;
        }
    }

    IEnumerator CaptureAndSendCoroutine()
    {
        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [CaptureAndSend] Starting...");

        // Wait until webcam is reporting a reasonable size
        float timeout = 5f;
        float start = Time.time;
        while ((webCam == null || webCam.width < 100) && Time.time - start < timeout)
        {
            yield return null;
        }

        if (webCam == null) yield break;

        // Allocate reusable buffers once
        int width = webCam.width > 0 ? webCam.width : requestedSize;
        int height = webCam.height > 0 ? webCam.height : requestedSize; 
        int totalPixels = width * height;
        captureTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        pixelBuffer = new Color32[totalPixels];
        prevPixelBuffer = new Color32[totalPixels];
        framesSinceLastSend = maxFramesBetweenKeyframes; // force keyframe on first capture
        firstFrameSent = false;

        while (true)
        {
            if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [CaptureAndSend] Capturing frame");

            // Grab pixels into reusable buffer
            try
            {
                webCam.GetPixels32(pixelBuffer);

                // Delta-frame detection
                int changed = 0;
                if (!firstFrameSent)
                {
                    // ensure first frame always sends
                    changed = totalPixels;
                }
                else
                {
                    // count pixels where channel diffs exceed threshold
                    for (int i = 0; i < totalPixels; i++)
                    {
                        Color32 a = pixelBuffer[i];
                        Color32 b = prevPixelBuffer[i];

                        int dr = a.r - b.r;
                        if (dr < 0) dr = -dr;
                        if (dr > pixelDiffThreshold)
                        {
                            changed++;
                            continue;
                        }

                        int dg = a.g - b.g;
                        if (dg < 0) dg = -dg;
                        if (dg > pixelDiffThreshold)
                        {
                            changed++;
                            continue;
                        }

                        int db = a.b - b.b;
                        if (db < 0) db = -db;
                        if (db > pixelDiffThreshold)
                        {
                            changed++;
                            continue;
                        }
                    }
                }

                
                float changePercent = (changed * 100f) / totalPixels;
                bool shouldForceKeyframe = framesSinceLastSend >= maxFramesBetweenKeyframes;
                bool shouldSend = firstFrameSent == false || shouldForceKeyframe || changePercent >= minChangePercent;

                if (DebugSettings.Instance.ShouldLog(logLevel))
                {
                    Debug.Log($"[StreamVideo] [Delta] changed={changed} ({changePercent:F2}%), shouldSend={shouldSend}, framesSinceLastSend={framesSinceLastSend}");
                }

                if (shouldSend)
                {
                    // Update the texture only when sending (avoid work when skipping)
                    captureTexture.SetPixels32(pixelBuffer);
                    captureTexture.Apply(false);

                    byte[] jpg = captureTexture.EncodeToJPG(jpgQuality);

                    // Send to server (server will relay to clients)
                    SendFrameServerRpc(jpg);

                    // update prev buffer snapshot
                    Array.Copy(pixelBuffer, prevPixelBuffer, totalPixels);

                    framesSinceLastSend = 0;
                    firstFrameSent = true;
                }
                else
                {
                    framesSinceLastSend++;
                }
            }
            catch (Exception ex)
            {
                if (DebugSettings.Instance.ShouldLog(logLevel)) { Debug.LogWarning($"[StreamVideo] [CaptureAndSend] Exception: {ex.Message}"); }
            }

            yield return new WaitForSeconds(captureInterval);
        }
    }

    void StopCapture()
    {
        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [StopCapture] Stopping web cam play");

        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }
        if (webCam != null && webCam.isPlaying)
        {
            webCam.Stop();
        }
    }

    [ServerRpc]
    void SendFrameServerRpc(byte[] jpgBytes, ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [SendFrameServerRpc] Received frame from sender: {senderId}");

        // Build list of clients to forward to (everyone except the original sender).
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;

        // ConnectedClientsIds returns all client ids (including host's client id)
        var targetIds = networkManager.ConnectedClientsIds.Where(id => id != senderId).ToArray();
        if (targetIds.Length == 0) return;

        // Relay to targeted clients (including host if host is a client)
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targetIds
            }
        };

        RelayFrameClientRpc(jpgBytes, senderId, clientRpcParams);
    }

    [ClientRpc]
    void RelayFrameClientRpc(byte[] jpgBytes, ulong senderId, ClientRpcParams clientRpcParams = default)
    {
        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [RelayFrame] Calling Client Rpc - sender: {senderId}");
        // Each client receives frames; apply them only to the StreamVideo instance
        // whose OwnerClientId matches senderId and which is not the local owner (remote view)
        if (OwnerClientId != senderId) return;
        if (IsOwner) return; // owner already shows its own webcam locally


        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [RelayFrame] Applying remote textures");
        ApplyRemoteTexture(jpgBytes);
    }

    void ApplyRemoteTexture(byte[] jpgBytes)
    {
        if (jpgBytes == null || jpgBytes.Length == 0) return;

        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log($"[StreamVideo] [ApplyRemoteTexture] Applying texture");

        // Destroy previous received texture to avoid leaks
        if (receivedTexture != null)
        {
            Destroy(receivedTexture);
            receivedTexture = null;
        }

        receivedTexture = new Texture2D(2, 2);

        if (rend == null) rend = GetComponent<Renderer>(); // safety check

        if (receivedTexture.LoadImage(jpgBytes))
        {
            if (rend.materials[1] != null)
            {
                rend.materials[1].mainTexture = receivedTexture;
                rend.materials[1].color = Color.white;
            }
        }
        else
        {
            Destroy(receivedTexture);
            receivedTexture = null;
        }
    }

    public override void OnDestroy()
    {
        StopCapture();
        if (webCam != null && webCam.isPlaying) webCam.Stop();
        if (receivedTexture != null) Destroy(receivedTexture);
        if (captureTexture != null) Destroy(captureTexture);
    }
}