using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class VivoxPlayer : NetworkBehaviour
{
    [Header("Attributes")]
    new string name;
    Sprite profileImageSprite;
    bool isMuted;
    bool canHear;


    [Header("Components")]
    [SerializeField] Image profileImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] Toggle isMutedToggle;
    [SerializeField] Toggle canHearToggle;

    public void TeleportToPlayer()
    {

    }

    public void MessagePlayer()
    {

    }

    /// <summary>
    /// Turns on and off the client's ability to hear the player. 
    /// </summary>
    public void TogglePlayerAudio()
    {

    }

    /// <summary>
    /// Turns on and off the player's ability to hear the client.
    /// </summary>
    public void TogglePlayerSpeaker()
    {

    }


}
