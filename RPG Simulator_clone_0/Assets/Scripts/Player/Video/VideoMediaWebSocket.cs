using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp.Server;


/// <summary>
/// Manages WebSocket connection functionalities for video media streams.
/// </summary>
public class VideoMediaWebSocket : WebSocketBehavior
{
    // first number is sender, second number is receiver
    // NOTE: This is just for testing with 3 clients, modify so that it works for any number of clients
    private static readonly List<string> connections = new()
    {
           "01", "02", "10", "12", "20", "21"
    };
    private static int connectionCount = 0;

    [Header("Debug Settings")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Video;
    private bool shouldLog;

    protected override void OnOpen()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);
        if (shouldLog) Debug.Log("WebSocket connection opened.");

        // send client ID
        Sessions.SendTo(connectionCount.ToString(), ID);
        connectionCount++;

        // send all connected users
        Sessions.SendTo(string.Join("|", connections), ID);
    }

    protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
    {
        // Forward signalling messages to all other clients except ourself
        foreach (var id in Sessions.ActiveIDs)
        {
            if (id != ID)
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }
}

