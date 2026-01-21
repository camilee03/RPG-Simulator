using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ControlPlayerInteraction : MonoBehaviour
{
    PlayerInfo playerInfo;
    [SerializeField] Toggle muteToggle;
    [SerializeField] Toggle hearToggle;
    [SerializeField] TMP_Text playerNameText;

    public void InitializeUI(PlayerInfo newPlayer)
    {
        playerInfo = newPlayer;

        playerNameText.text = playerInfo.playername;
        muteToggle.isOn = false;
        hearToggle.isOn = true;
    }

    public void Mute(bool mute)
    {
        if (mute)
        {
            //
        }
        else
        {
            //
        }
    }

    // Can't be changed by player, only by audio
    public void Hear(bool hear)
    {
        if (hear)
        {

        }
        else 
        { 

        }
    }

    public void RunToPlayer()
    {

    }
}
