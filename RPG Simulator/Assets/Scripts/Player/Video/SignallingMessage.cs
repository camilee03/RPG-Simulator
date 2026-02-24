using System;
using UnityEngine;

public class SignallingMessage : MonoBehaviour
{
    public readonly SignallingMessageType Type;
    public readonly string ChannelID;
    public readonly string Message;

    public SignallingMessage(string message)
    {
        var messageArray = message.Split('!');

        if (messageArray.Length < 3)
        {
            Type = SignallingMessageType.OTHER;
            ChannelID = "";
            Message = message;
        }
        else if (Enum.TryParse(messageArray[0], out SignallingMessageType messageType))
        {
            switch (messageType)
            {
                case SignallingMessageType.OFFER:
                case SignallingMessageType.ANSWER:
                case SignallingMessageType.CANDIDATE:
                    Type = messageType;
                    ChannelID = messageArray[1];
                    Message = messageArray[2];
                    break;
                case SignallingMessageType.OTHER:
                default:
                    break;
            }
        }
    }
}

public enum SignallingMessageType { OFFER, ANSWER, CANDIDATE, OTHER }

