using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class VivoxTextChat : NetworkBehaviour
{
    enum SendMode { Channel, Whisper }

    [Header("UI References")]
    [SerializeField] private TMP_Text messageHistory;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private ScrollRect vivoxChat;
    [SerializeField] private TMP_Dropdown receiverDropdown;

    [Header("Input Reference")]
    [SerializeField] private PlayerInputManager playerInputManager;

    private string clientID;
    private List<string> clientIDs = new();

    [Header("Debug Settings")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Vivox;
    private bool shouldLog;

    public void OnEnable()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);

        clientID = AuthenticationService.Instance.PlayerId;
        VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
        NetworkManager.Singleton.OnConnectionEvent += ChangeClientList;
    }

    private void OnDisable()
    {
        VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnConnectionEvent -= ChangeClientList;
    }

    private void Update()
    {
        if (playerInputManager.chatOpen && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            playerInputManager.OnEscape();
        }
    }

    public void OnChannelMessageReceived(VivoxMessage message)
    {
        if (message == null) return;

        string messageText = message.MessageText?.Trim();
        string senderId = message.SenderPlayerId ?? string.Empty;
        string senderName = VivoxManager.Instance.GetPlayerNameFromID(senderId);

        if (string.IsNullOrEmpty(messageText)) return;
        if (senderId == clientID) return; // We don't want to double send the same message

        // Private message format: "/pm:{message}"
        const string pmPrefix = "/pm:";

        if (messageText.StartsWith(pmPrefix, StringComparison.Ordinal))
        {
            // Split into 2 parts max: prefix, rest of message
            string[] parts = messageText.Split(new[] { ':' }, 2);
            if (parts.Length < 2) return; // Invalid PM format, ignore

            string pmBody = parts[1];

            AddMessageToHistory(SendMode.Whisper, senderName, pmBody);

            if (shouldLog)
            {
                Debug.Log($"[PM] {senderName} -> You: {pmBody}");
            }

            // If not intended for this client, do not log
            return;
        }

        // Otherwise it's a channel message (not a PM) — log it.
        AddMessageToHistory(SendMode.Channel, senderName, messageText);
        if (shouldLog)
        {
            Debug.Log($"[Channel] {senderName}: {messageText}");
        }
    }

    public async void SendTextMessage(string message)
    {
        string messageText = message.Trim();
        if (string.IsNullOrEmpty(messageText)) return;

        if (receiverDropdown.value == 0)
        {
            VivoxManager.Instance.SendMessageAsync(messageText);
            AddMessageToHistory(SendMode.Channel, "You", messageText);
        }
        else
        {
            string id = clientIDs[receiverDropdown.value-1];
            await VivoxManager.Instance.SendMessageToClientAsync(id, messageText);
            AddMessageToHistory(SendMode.Whisper, "You", messageText);
        }
        messageInput.text = "";
    }

    private void AddMessageToHistory(SendMode mode, string sender, string message)
    {
        string msgText = messageHistory.text;
        if (msgText != "") msgText += "\n";

        switch (mode)
        {
            case SendMode.Channel:
                msgText += $"<color=\"black\">{sender}: {message}";
                break;
            case SendMode.Whisper:
                msgText += $"<color=\"red\">{sender}: {message}";
                break;
        }

        messageHistory.text = msgText;

        // force scroll to top
        Canvas.ForceUpdateCanvases();
        vivoxChat.verticalNormalizedPosition = 0f;
    }

    private void ChangeClientList(NetworkManager manager, ConnectionEventData data)
    {
        Player[] players = LobbyManager.Instance.GetAllPlayers();
        clientIDs = new();
        receiverDropdown.options = new();
        TMP_Dropdown.OptionData optionData = new()
        {
            text = "All",
        };
        receiverDropdown.options.Add(optionData);

        foreach (Player player in players)
        {
            if (player.Id == clientID) return;

            string playerInfoStr = player?.Data != null && player.Data.ContainsKey(LobbyManager.KEY_PLAYER_INFO) ? player.Data[LobbyManager.KEY_PLAYER_INFO].Value : player?.Id;
            optionData = new()
            {
                text = playerInfoStr.Split(':')[0],
            };
            //receiverDropdown.options.Add(optionData);
            //clientIDs.Add(player.Id);

            if (shouldLog) Debug.Log($"Adding Player {playerInfoStr.Split(':')[0]} to list");
        }

        receiverDropdown.value = 0;
    }
}
