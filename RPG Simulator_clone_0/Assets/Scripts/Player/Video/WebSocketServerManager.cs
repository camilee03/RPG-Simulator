using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WebSocketSharp.Server;

public class WebSocketServerManager : MonoBehaviour
{
    [Header("WebSocket Server Settings")]
    private WebSocketServer webSocketServer;
    private string serverIp4Address;
    private readonly string serverIP = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address;
    private readonly int serverPort = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port;

    [Header("Debug")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Video;
    private bool shouldLog;

    private void Start()
    {
        CreateWebSocket();
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);
    }
    private void CreateWebSocket()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                serverIp4Address = ip.ToString();
                break;
            }
        }

        webSocketServer = new WebSocketServer($"ws://{serverIp4Address}:{serverPort}/");
        webSocketServer.AddWebSocketService<VideoMediaWebSocket>($"/{nameof(VideoMediaWebSocket)}");
        webSocketServer.Start();
    }

    private void OnDestroy()
    {
        webSocketServer.Stop();
    }

}
