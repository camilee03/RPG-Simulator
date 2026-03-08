using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    private PlayerInfo[] playerInfos;

    [Header("Debug Settings")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Vivox;
    private bool shouldLog;

    public void OnEnable()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);

        clientID = AuthenticationService.Instance.PlayerId;
        VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
        VivoxService.Instance.DirectedMessageReceived += OnDirectMessageReceived;
        NetworkManager.Singleton.OnConnectionEvent += ChangeClientList;
    }

    private void OnDisable()
    {
        VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
        VivoxService.Instance.DirectedMessageReceived -= OnDirectMessageReceived;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnConnectionEvent -= ChangeClientList;
    }

    private void Update()
    {
        if (playerInputManager.chatOpen && Mouse.current.leftButton.isPressed && !EventSystem.current.IsPointerOverGameObject())
        {
            playerInputManager.OnEscape();
        }
    }

    public void OnDirectMessageReceived(VivoxMessage message)
    {
        if (message == null) return;
        if (string.IsNullOrEmpty(message.MessageText)) return;

        const string connectedSuffix = "joined the game.";
        if (message.MessageText.EndsWith(connectedSuffix, StringComparison.Ordinal))
        {
            ChangeClientList(NetworkManager.Singleton, default);
        }

        string messageText = message.MessageText?.Trim();
        string senderId = message.SenderPlayerId;
        string senderName = VivoxManager.Instance.GetPlayerNameFromID(senderId);

        if (senderId == clientID) return; // We don't want to double send the same message

        // Prefixes
        const string secretPrefix = "/secret~";

        // -- Secret PM: "/secret~{"sender"}~{message}"
        if (messageText.StartsWith(secretPrefix, StringComparison.Ordinal))
        {
            LogSecretPrivateMessage(messageText); return;
        }

        LogPrivateMessage(messageText, senderName); return;
    }

    public void OnChannelMessageReceived(VivoxMessage message)
    {
        if (message == null) return;
        if (string.IsNullOrEmpty(message.MessageText)) return;

        const string connectedSuffix = "joined the game.";
        if (message.MessageText.EndsWith(connectedSuffix, StringComparison.Ordinal))
        {
            ChangeClientList(NetworkManager.Singleton, default);
        }

        string messageText = message.MessageText?.Trim();
        string senderId = message.SenderPlayerId;
        string senderName = VivoxManager.Instance.GetPlayerNameFromID(senderId);

        if (senderId == clientID) return; // We don't want to double send the same message

        // Prefixes
        const string oraclePrefix = "/o~";

        // -- Private oracle message: "/o~{message}" - only send to DM
        if (messageText.StartsWith(oraclePrefix, StringComparison.Ordinal))
        {
            if (LobbyManager.Instance.GetPlayerInfo().playerType == PlayerType.DM)
                LogOracleMessage(messageText, senderName); 
            return;
        }

        else {
            // -- Channel message — log to everyone
            AddMessageToHistory(SendMode.Channel, senderName, messageText);
            if (shouldLog) Debug.Log($"[Channel] {senderName}: {messageText}");
        }
    }

    private void LogOracleMessage(string messageText, string senderName)
    {
        if (shouldLog) Debug.Log($"[Oracle] {senderName}: {messageText}");

        // Split into 2 parts max: prefix, rest of message
        string[] parts = messageText.Split(new[] { '~' }, 2);
        if (parts.Length < 2) return; // Invalid PM format, ignore
        string pmBody = "O: " + parts[1];

        AddMessageToHistory(SendMode.Whisper, senderName, pmBody);
    }

    private void LogPrivateMessage(string messageText, string senderName)
    {
        AddMessageToHistory(SendMode.Whisper, senderName, "[PM]" + messageText);

        if (shouldLog) Debug.Log($"[PM] {senderName} -> You: {messageText}");
    }

    private void LogSecretPrivateMessage(string messageText)
    {
        // Split into 3 parts max: prefix, secretSender, rest of message
        string[] parts = messageText.Split(new[] { '~' }, 3);
        if (parts.Length < 3) return; // Invalid PM format, ignore
        string secretSender = parts[1];
        string pmBody = parts[2];

        AddMessageToHistory(SendMode.Whisper, secretSender, pmBody);

        if (shouldLog) Debug.Log($"[SECRET PM] {secretSender} -> You: {pmBody}");
    }

    public async void SendTextMessage(string message)
    {
        string messageText = message.Trim();
        if (string.IsNullOrEmpty(messageText)) return;

        // Don't show in messages if this is an oracle question
        const string oraclePrefix = "/o~";
        if (message.StartsWith(oraclePrefix, StringComparison.Ordinal))
        {
            VivoxManager.Instance.SendMessageAsync(message); return;
        }

        // Otherwise, show in messages and send
        if (receiverDropdown.value == 0)
        {
            VivoxManager.Instance.SendMessageAsync(messageText);
            AddMessageToHistory(SendMode.Channel, "You -> ALL", messageText);
        }
        else
        {
            string id = playerInfos[receiverDropdown.value-1].playerID;
            await VivoxManager.Instance.SendMessageToClientAsync(id, messageText);
            AddMessageToHistory(SendMode.Whisper, $"You -> {playerInfos[receiverDropdown.value-1].playerName}", messageText);
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
                msgText += $"<color=\"green\">{sender}: {message}";
                break;
        }

        messageHistory.text = msgText;

        // force scroll to top
        Canvas.ForceUpdateCanvases();
        vivoxChat.verticalNormalizedPosition = 0f;
    }

    private void ChangeClientList(NetworkManager manager, ConnectionEventData data)
    {
        if (clientID == null) clientID = AuthenticationService.Instance.PlayerId;

        playerInfos = LobbyManager.Instance.GetAllPlayerInfo();
        if (playerInfos == null) return;
        
        receiverDropdown.options = new();
        TMP_Dropdown.OptionData optionData = new()
        {
            text = "All",
        };
        receiverDropdown.options.Add(optionData);

        foreach (PlayerInfo info in playerInfos)
        {
            if (info == null) continue;
            if (info.playerID == clientID) continue; // Don't add self to receiver list
            optionData = new()
            {
                text = info.playerName,
            };
            receiverDropdown.options.Add(optionData);

            if (shouldLog) Debug.Log($"Adding Player {info.playerName} to list");
        }

        receiverDropdown.value = 1;
        receiverDropdown.value = 0;
    }
}
