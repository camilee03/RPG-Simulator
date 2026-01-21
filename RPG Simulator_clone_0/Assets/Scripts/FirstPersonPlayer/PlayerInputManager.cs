using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Used for defining player input when it is not directly attached to the parent. 
/// </summary>
/// 
public class PlayerInputManager : NetworkBehaviour
{
    [SerializeField] Notebook notebook;
    [SerializeField] GameObject stats;
    [SerializeField] StreamVideo streamVideo;

    [SerializeField] FirstPersonLook cameraController;
    [SerializeField] FirstPersonMovement playerController;

    DiceRoller roller;

    public ActionType readyAction;
    public ActionType currentAction { get; private set; }
    public GameObject currentObject { private get; set; }
    

    private void Start()
    {
        roller = GetComponentInChildren<DiceRoller>();
    }


    void OnNotebook()
    {
        if (!notebook.isOpen)
        {
            SetPlayerMovement(false);
            notebook.Open();
        }
    }


    private void SetPlayerMovement(bool isOn)
    {
        if (!isOn) playerController.StopMovement();
        else playerController.ResumeMovement();

        cameraController.enabled = isOn;
        playerController.enabled = isOn;
    }

    public void OnEscape()
    {
        if (notebook.isOpen)
        {
            notebook.Close();
            SetPlayerMovement(true);
        }
        if (roller.isVisible)
        {
            roller.Exit();
        }
    }

    public void OnAction()
    {
        if (notebook.isOpen) return;
        if (!IsOwner && !DebugSettings.Instance.EnableInput) return;

        if (currentAction != ActionType.None)
        {
            // stop current action
            switch (currentAction)
            {
                case ActionType.Player: break;
                case ActionType.Sit:
                    playerController.OnSit(currentObject);
                    break;
                default: break;
            }
            currentAction = ActionType.None;
        }

        else
        {
            // start a new action
            switch (readyAction)
            {
                case ActionType.None: break;
                case ActionType.Player: break;
                case ActionType.Sit: 
                    playerController.OnSit(currentObject);
                    break;
                default: break;
            }

            currentAction = readyAction;
        }
    }

    void OnRoll()
    {
        if (notebook.isOpen) return;
        roller.RollDice();
    }


    #region Debug
    void OnCamera()
    {
        if (streamVideo.enabled) streamVideo.StopCapture();

        streamVideo.enabled = !streamVideo.enabled;

        if (streamVideo.enabled) streamVideo.StartCapture();
    }

    #endregion
}

public enum ActionType { None, Sit, Player, }
