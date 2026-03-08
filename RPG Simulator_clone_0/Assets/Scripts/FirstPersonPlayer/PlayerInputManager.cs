using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


/// <summary>
/// Used for defining player input when it is not directly attached to the parent. 
/// </summary>
/// 
public class PlayerInputManager : NetworkBehaviour
{
    [SerializeField] Notebook notebook;
    [SerializeField] GameObject settings;
    [SerializeField] ProcessVideo processVideo;
    [SerializeField] CinemachineCamera defaultCamera;
    [SerializeField] NonDefaultUI nonDefaultUI;
    [SerializeField] GameObject vivoxChat;

    [SerializeField] FirstPersonLook cameraController;
    [SerializeField] FirstPersonMovement playerController;

    [Header("UI Elements")]
    [SerializeField] GameObject standardUI;
    [SerializeField] GameObject drawingUI;

    DiceRoller roller;

    [Header("Bools")]
    public bool chatOpen = false;

    public ActionType readyAction;
    public ActionType currentAction { get; private set; }
    public GameObject currentObject { private get; set; }
    private Whiteboard currentWhiteboard;
    

    private void Start()
    {
        roller = GetComponentInChildren<DiceRoller>();
    }

    #region Inputs with Input Text Box

    private void OnNotebook()
    {
        if (CheckOtherUI()) return;

        if (!notebook.isOpen)
        {
            if (notebook.Open()) SetPlayerMovement(false);
        }
    }

    private void OnChat()
    {
        if (CheckOtherUI()) return;

        if (!chatOpen)
        {
            playerController.StopMovement();
            Cursor.lockState = CursorLockMode.None;

            cameraController.enabled = chatOpen;
            playerController.enabled = chatOpen;

            chatOpen = true;

            EventSystem.current.SetSelectedGameObject(vivoxChat);
        }
    }
    void OnRoll()
    {
        if (CheckOtherUI()) return;
        roller.RollDice();
    }

    void OnOracle()
    {
        if (CheckOtherUI()) return;
        roller.OracleRoll();
        SetPlayerMovement(false);
        Cursor.lockState = CursorLockMode.None;
    }

    #endregion

    void OnSettings()
    {
        if (CheckOtherUI()) return;

        settings.SetActive(!settings.activeSelf);

        if (settings.activeSelf)
        {
            SetPlayerMovement(false);
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            SetPlayerMovement(true);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    #region Action Inputs
    public void OnEscape()
    {
        if (notebook.isOpen)
        {
            if (notebook.Close()) SetPlayerMovement(true);
        }
        else if (roller.isVisible)
        {
            roller.Exit();
        }
        else if (roller.isOracleVisible)
        {
            roller.ExitOracle();
            Cursor.lockState = CursorLockMode.Locked;
            SetPlayerMovement(true);
        }
        else if (chatOpen)
        {
            playerController.ResumeMovement();
            Cursor.lockState = CursorLockMode.Locked;

            cameraController.enabled = chatOpen;
            playerController.enabled = chatOpen;

            chatOpen = false;
        }
        // Only stop action if not doing anything else
        else StopAction();
    }

    private void StopAction()
    {
        // stop current action
        switch (currentAction)
        {
            case ActionType.Sit:
                playerController.OnSit(currentObject);
                currentAction = ActionType.None;
                break;
            case ActionType.Draw:
                if (currentWhiteboard.StopDrawing(defaultCamera))
                {
                    SetPlayerMovement(true);
                    standardUI.SetActive(true);
                    drawingUI.SetActive(false);
                    currentAction = ActionType.None;
                    currentWhiteboard = null;
                }
                break;
            default: currentAction = ActionType.None; break;
        }
    }

    public void OnAction()
    {
        if (CheckOtherUI()) return;

        if (!IsOwner && !DebugSettings.Instance.OfflineTesting) return;

        if (currentAction != ActionType.None) StopAction();

        else
        {
            // start a new action
            switch (readyAction)
            {
                case ActionType.Sit: 
                    playerController.OnSit(currentObject);
                    currentAction = readyAction;
                    break;
                case ActionType.Draw:
                    currentWhiteboard = currentObject.GetComponent<Whiteboard>();
                    if (currentWhiteboard.StartDrawing(defaultCamera))
                    {
                        SetPlayerMovement(false);
                        standardUI.SetActive(false);
                        drawingUI.SetActive(true);
                        nonDefaultUI.InitializeDrawingElements(currentWhiteboard);
                        currentAction = readyAction;
                    }
                    break;

                default: currentAction = readyAction; break;
            }

        }
    }

    #endregion


    #region Helper Functions
    /// <summary>
    /// Returns true if any UI that has input text 
    /// is currently open, to prevent input conflicts.
    /// </summary>
    private bool CheckOtherUI()
    {
        return notebook.isOpen || chatOpen || roller.isOracleVisible;
    }

    private void SetPlayerMovement(bool isOn)
    {
        if (!isOn) playerController.StopMovement();
        else playerController.ResumeMovement();

        cameraController.enabled = isOn;
        playerController.enabled = isOn;
    }

    #endregion


    #region Player to Player Actions

    void OnAudio()
    {
        if (readyAction == ActionType.Player)
        {
            // Toggle Audio
            /*
            VivoxPlayer player = currentObject.GetComponent<VivoxPlayer>();
            player.TogglePlayerAudio();
            */
        }
    }

    void OnSpeaker()
    {
        if (readyAction == ActionType.Player)
        {
            // Toggle Speaker
            /*
            VivoxPlayer player = currentObject.GetComponent<VivoxPlayer>();
            player.TogglePlayerSpeaker();
            */
        }
    }

    #endregion


    #region Debug
    public void OnCamera()
    {
        if (CheckOtherUI()) return;

        if (processVideo.enabled) processVideo.StopCapture();

        processVideo.enabled = !processVideo.enabled;

        if (processVideo.enabled) processVideo.StartCapture();
    }

    #endregion
}

public enum ActionType { None, Sit, Player, Draw, }
