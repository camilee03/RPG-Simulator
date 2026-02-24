using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Whiteboard : NetworkBehaviour
{
    public bool isActive;
    CinemachineCamera whiteboardCamera;
    [SerializeField] GameObject linePrefab;
    [SerializeField] GameObject lineParent;

    [SerializeField] Image backgroundImage;
    WhiteboardMarker activeLine;
    Color currentColor = Color.black;
    float currentPenWidth = 0.1f;
    Tool currentTool = Tool.Pen;


    bool isTransitioning = false;

    [Header("Debug Settings")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Whiteboard;
    private bool shouldLog;

    public enum Tool
    {
        Pen,
        Fill,
        Eraser,
        None
    }

    private void Start()
    {
        whiteboardCamera = GetComponentInChildren<CinemachineCamera>();
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);
    }


    // Notes for improvement: 
    // 1. Add icons to follow the mouse to showcase which tool is being used
    // 2. Different pen types?
    // 3. Have eraser only erase parts of lines rather than whole lines
    // 4. Fill tool to fill enclosed areas (How??)
    // 5. Undo/Redo functionality
    // 6. Save whiteboard drawings between sessions
    // 7. Restructure RPCs into one

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetMouseButtonDown(0)) StartDraw();
        if (Input.GetMouseButtonUp(0)) StopDraw();

        if (IsStillDrawing()) ContinueDraw();
    }

    private void StartDraw()
    {
        if (shouldLog)
        {
            Debug.Log($"[Whiteboard] Drawing with {currentTool}");
            Debug.Log($"[Whiteboard] Current color: {currentColor}, Current width: {currentPenWidth}");
        }

            switch (currentTool)
        {
            case Tool.Pen:
                if (IsHost)
                {
                    GameObject newLine = Instantiate(linePrefab, lineParent.transform, false);
                    NetworkObject networkObject = newLine.GetComponent<NetworkObject>();
                    networkObject.Spawn();
                    networkObject.TrySetParent(lineParent);
                    
                    // Initialize the spawned object's visual properties on all clients.
                    if (newLine.TryGetComponent<WhiteboardMarker>(out activeLine))
                    {
                        activeLine.ChangeColorClientRpc(currentColor);
                        activeLine.ChangeColor(currentColor);

                        activeLine.ChangeWidthClientRpc(currentPenWidth);
                        activeLine.ChangeWidth(currentPenWidth);
                    }
                }
                else if (IsClient)
                {
                    SpawnLineServerRpc(currentColor, currentPenWidth);
                }
                else if (DebugSettings.Instance.OfflineTesting)
                {
                    GameObject newLine = Instantiate(linePrefab, lineParent.transform, false);
                    activeLine = newLine.GetComponent<WhiteboardMarker>();

                    activeLine.ChangeColor(currentColor);
                    activeLine.ChangeWidth(currentPenWidth);

                }
                break;
            case Tool.Fill:
                // Fill logic to be implemented
                return;
            case Tool.Eraser:
                // Eraser logic to be implemented
                return;
            case Tool.None: // do nothing
                return;
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnLineServerRpc(Color color, float width)
    {
        GameObject newLine = Instantiate(linePrefab, lineParent.transform, false);
        NetworkObject networkObject = newLine.GetComponent<NetworkObject>();
        networkObject.Spawn();

        // Initialize the spawned object's visual properties on all clients.
        if (newLine.TryGetComponent<WhiteboardMarker>(out WhiteboardMarker marker))
        {
            marker.ChangeColorClientRpc(color);
            marker.ChangeColor(color);

            marker.ChangeWidthClientRpc(width);
            marker.ChangeWidth(width);
        }

        // Send the NetworkObjectReference back to clients so they can grab a local reference.
        GetObjectReferenceClientRpc(networkObject);
    }

    [ClientRpc]
    private void GetObjectReferenceClientRpc(NetworkObjectReference reference)
    {
        if (reference.TryGet(out NetworkObject networkObject))
        {
            activeLine = networkObject.gameObject.GetComponent<WhiteboardMarker>();
        }
    }

    [Rpc(SendTo.Server)]
    private void DespawnAllLinesServerRpc()
    {
        DespawnAllLines();
    }

    private void DespawnAllLines()
    {
        if (!DebugSettings.Instance.OfflineTesting)
        {
            int numChildren = lineParent.transform.childCount;

            for (int i = 0; i < numChildren; i++)
            {
                lineParent.transform.GetChild(i).GetComponent<NetworkObject>().Despawn(true);
            }
        }
        else { 
            Destroy(lineParent);
            lineParent = new("LineParent");
            lineParent.transform.parent = transform;
        }
    }

    private void ContinueDraw()
    {
        switch (currentTool)
        {
            case Tool.Pen:
                Vector3 zMousePosition = Input.mousePosition;
                zMousePosition.z = 4f;
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(zMousePosition);

                if (IsHost) activeLine.UpdateLineClientRpc(mousePos);
                else if (IsClient) activeLine.UpdateLineServerRpc(mousePos);
                else if (DebugSettings.Instance.OfflineTesting) activeLine.UpdateLine(mousePos);
                break;
            case Tool.Fill:
                // Doesn't matter: baseline logic already done
                return;
            case Tool.Eraser:
                // Eraser logic to be implemented
                return;
            case Tool.None: // do nothing
                return;
        }
    }

    private bool IsStillDrawing()
    {
        return Input.GetMouseButton(0) && activeLine != null;
    }

    private void StopDraw()
    {
        activeLine = null;
    }


    public bool StartDrawing(CinemachineCamera defaultCamera)
    {
        if (isTransitioning) return false;
        
        StartCoroutine(WaitToActivate());
        Cursor.lockState = CursorLockMode.None;

        whiteboardCamera.Priority = 1;
        defaultCamera.Priority = 0;
        return true;
    }

    public bool StopDrawing(CinemachineCamera defaultCamera)
    {
        if (isTransitioning) return false;

        isActive = false;
        Cursor.lockState = CursorLockMode.Locked;

        whiteboardCamera.Priority = 0;
        defaultCamera.Priority = 1;

        return true;
    }


    #region Background Image Generation

    public async void SetNewBackgroundImage()
    {
        Sprite sprite = await FileBrowserUpdate.Instance.GetSpriteFromImageFileBrowser();

        if (sprite == null)
        {
            if (shouldLog) Debug.Log("[Whiteboard] No sprite selected.");
            return;
        }

        // Apply locally
        backgroundImage.sprite = sprite;

        // Try to convert the sprite to PNG bytes
        byte[] png = Converter.SpriteToPng(sprite, out int width, out int height);
        if (png == null || png.Length == 0)
        {
            if (shouldLog) Debug.LogWarning("[Whiteboard] Could not serialize sprite for network sync. Background updated locally only.");
            return;
        }

        // If host, directly tell clients to update. If client, send to server to propagate.
        if (IsHost)
        {
            UpdateBackgroundImageClientRpc(png, width, height);
        }
        else if (IsClient)
        {
            UpdateBackgroundImageServerRpc(png, width, height);
        }
    }

    // Server-bound RPC (client -> server). Uses project custom attribute pattern.
    [Rpc(SendTo.Server)]
    private void UpdateBackgroundImageServerRpc(byte[] imageData, int width, int height)
    {
        try
        {
            if (imageData != null && imageData.Length > 0)
            {
                // Apply on server/host as well
                Sprite serverSprite = Converter.PngBytesToSprite(imageData, width, height);
                if (serverSprite != null)
                {
                    backgroundImage.sprite = serverSprite;
                }
            }
        }
        catch (Exception e)
        {
            if (shouldLog) Debug.LogError($"[Whiteboard] Error applying background on server: {e}");
        }

        // Propagate to all clients
        UpdateBackgroundImageClientRpc(imageData, width, height);
    }

    // Client-bound RPC (server/host -> clients)
    [ClientRpc]
    private void UpdateBackgroundImageClientRpc(byte[] imageData, int width, int height)
    {
        if (imageData == null || imageData.Length == 0)
        {
            if (shouldLog) Debug.LogWarning("[Whiteboard] Received empty background image payload.");
            return;
        }

        try
        {
            Sprite newSprite = Converter.PngBytesToSprite(imageData, width, height);
            if (newSprite != null)
            {
                backgroundImage.sprite = newSprite;
            }
            else if (shouldLog)
            {
                Debug.LogWarning("[Whiteboard] Failed to create sprite from received bytes.");
            }
        }
        catch (Exception e)
        {
            if (shouldLog) Debug.LogError($"[Whiteboard] Error applying received background image: {e}");
        }
    }

    #endregion

    #region Whiteboard Tools

    public void ChangeColor(Color newColor)
    {
        currentColor = newColor;
    }

    public void ChangePenWidth(float newWidth)
    {
        currentPenWidth = newWidth;
    }

    public void ChangeTool(Tool newTool)
    {
        currentTool = newTool;
    }

    public void ClearBoard()
    {
        if (IsHost) DespawnAllLines();
        else if (IsClient) DespawnAllLinesServerRpc();
        else if (DebugSettings.Instance.OfflineTesting) DespawnAllLines();
    }

    #endregion

    IEnumerator WaitToActivate()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(2f);
        isActive = true;
        isTransitioning = false;
    }
}
