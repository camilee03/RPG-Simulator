using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class StreamVideo : MonoBehaviour
{
    [Header("WebSocket Client Settings")]
    private WebSocket webSocket;
    private readonly string serverIP = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address;
    private readonly int serverPort = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port;

    [Header("Video Settings")]
    private string clientId = NetworkManager.Singleton.LocalClientId.ToString();
    [SerializeField] private Camera cameraStream;
    [SerializeField] private RawImage sourceImage;
    [SerializeField] private List<RawImage> receivedImages = new();

    private VideoStreamTrack videoStreamTrack;
    private int receiveImageCounter = 0;


    [Header("Connections")]
    private Dictionary<string, RTCPeerConnection> peerConnections = new();

    private bool hasReceievedOffer = false;
    private SessionDescription receivedOfferSessDescr;
    private string receivedOfferChannelId;


    [Header("Debug")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Video;
    private bool shouldLog;



    private void Start()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);
        InitClient(serverIP, serverPort);
    }



    private void Update()
    {
        if (hasReceievedOffer)
        {
            hasReceievedOffer = !hasReceievedOffer;
            StartCoroutine(CreateAnswer(peerConnections[receivedOfferChannelId], receivedOfferChannelId));
        }
    }

    private void InitClient(string serverIP, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;

        webSocket = new WebSocket($"ws://{serverIP}:{port}/{nameof(VideoMediaWebSocket)}");
        webSocket.OnMessage += (sender, e) => {
            SignallingMessage signallingMessage = new(e.Data);

            switch (signallingMessage.Type)
            {
                case SignallingMessageType.OFFER:

                    if (clientId == signallingMessage.ChannelID.Substring(1, 1))
                    {
                        if (shouldLog) Debug.Log($"{clientId} received OFFER from {signallingMessage.ChannelID} from Maximus {signallingMessage.Message}");

                        receivedOfferChannelId = signallingMessage.ChannelID;
                        receivedOfferSessDescr = SessionDescription.FromJson(signallingMessage.Message);
                        hasReceievedOffer = true;
                    }
                    break;
                case SignallingMessageType.ANSWER:
                    if (clientId == signallingMessage.ChannelID.Substring(0, 1))
                    {
                        if (shouldLog) Debug.Log($"{clientId} received ANSWER from {signallingMessage.ChannelID} from Maximus {signallingMessage.Message}");

                        var answerSessionDesc = SessionDescription.FromJson(signallingMessage.Message);
                        RTCSessionDescription answerDesc = new RTCSessionDescription
                        {
                            type = RTCSdpType.Answer,
                            sdp = answerSessionDesc.sdp
                        };

                        peerConnections[signallingMessage.ChannelID].SetRemoteDescription(ref answerDesc);
                    }
                    break;
                case SignallingMessageType.CANDIDATE:
                    if (clientId == signallingMessage.ChannelID.Substring(1, 1))
                    {
                        if (shouldLog) Debug.Log($"{clientId} received CANDIDATE from {signallingMessage.ChannelID} from Maximus {signallingMessage.Message}");

                        var candidateInit = CandidateInit.FromJson(signallingMessage.Message);
                        RTCIceCandidateInit init = new()
                        {
                            candidate = candidateInit.candidate,
                            sdpMid = candidateInit.sdpMid,
                            sdpMLineIndex = candidateInit.sdpMLineIndex
                        };
                        RTCIceCandidate candidate = new(init);

                        peerConnections[signallingMessage.ChannelID].AddIceCandidate(candidate);
                    }
                    break;
                default:
                    if (e.Data.Contains("|"))
                    {
                        var connectionIds = e.Data.Split('|');
                        foreach (var connectionId in connectionIds)
                        {
                            if (connectionId.Contains(clientId))
                            {
                                if (shouldLog) Debug.Log($"{clientId} creating peer connection for {connectionId}");
                                peerConnections.Add(connectionId, CreatePeerConnection(connectionId));
                            }
                        }
                    }
                    else
                    {
                        clientId = e.Data;
                    }
                    break;

            }

        };
        webSocket.Connect();

        // NOTE: Change to web cam texture instead
        videoStreamTrack = cameraStream.CaptureStreamTrack(1280, 720);
        sourceImage.texture = cameraStream.targetTexture;

        StartCoroutine(WebRTC.Update());
    }

    private RTCPeerConnection CreatePeerConnection(string id)
    {
        RTCPeerConnection peerConnection = new();
        peerConnection.OnIceCandidate = candidate =>
        {
            CandidateInit candidateInit = new()
            {
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex.GetValueOrDefault()
            };
            string message = $"{SignallingMessageType.CANDIDATE}!{id}!{candidateInit.ToJson()}";
            webSocket.Send(message);
        };

        peerConnection.OnIceConnectionChange = state =>
        {
            if (shouldLog) Debug.Log($"ICE connection state changed to {state}");
        };

        peerConnection.OnNegotiationNeeded = () =>
        {
            StartCoroutine(CreateOffer(peerConnection, id));
        };

        peerConnection.OnTrack = e =>
        {
            if (e.Track is VideoStreamTrack videoTrack)
            {
                videoTrack.OnVideoReceived += (texture) =>
                {
                    if (receiveImageCounter < receivedImages.Count)
                    {
                        receivedImages[receiveImageCounter].texture = texture;
                        receiveImageCounter++;
                    }
                };
            }
        };

        return peerConnection;
    }

    private IEnumerator CreateOffer(RTCPeerConnection peerConnection, string id)
    {
        RTCSessionDescriptionAsyncOperation offer = peerConnection.CreateOffer();
        yield return offer;

        RTCSessionDescription desc = offer.Desc;
        RTCSetSessionDescriptionAsyncOperation setLocalOp = peerConnection.SetLocalDescription(ref desc);
        yield return setLocalOp;

        SessionDescription offerSessionDesc = new()
        {
            type = desc.type.ToString().ToLower(),
            sdp = desc.sdp
        };

        string message = $"{SignallingMessageType.OFFER}!{id}!{offerSessionDesc.ToJson()}";
        webSocket.Send(message);
    }

    private IEnumerator CreateAnswer(RTCPeerConnection peerConnection, string id)
    {
        RTCSessionDescription offerSessionDesc = new()
        {
            type = RTCSdpType.Offer,
            sdp = receivedOfferSessDescr.sdp
        };

        RTCSetSessionDescriptionAsyncOperation remoteDescOp = peerConnection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = peerConnection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var setLocalOp = peerConnection.SetLocalDescription(ref answerDesc);
        yield return setLocalOp;

        var answerSessionDesc = new SessionDescription
        {
            type = answerDesc.type.ToString(),
            sdp = answerDesc.sdp
        };

        string message = $"{SignallingMessageType.ANSWER}!{id}!{answerSessionDesc.ToJson()}";
        webSocket.Send(message);
    }


    private void WebSocket_OnMessage(object sender, MessageEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OnDestroy()
    {
        videoStreamTrack.Stop();

        foreach (var connection in peerConnections)
        {
            connection.Value.Close();
        }

        webSocket.Close();
    }
}