using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ViewTriggers : NetworkBehaviour
{
    Transform lastHit;
    bool keepUI;
    bool hasUI;

    [SerializeField] TMP_Text promptText;
    [SerializeField] PlayerInputManager playerInputManager;

    private void OnEnable()
    {
        Ticker.OnFastTick += ViewTicker;
    }
    private void OnDisable()
    {
        Ticker.OnFastTick -= ViewTicker;
    }

    private void ViewTicker()
    {
        if (!IsOwner && !DebugSettings.Instance.OfflineTesting) return;

        if (playerInputManager.currentAction != ActionType.None) { keepUI = false; return; }

        CheckGaze();
    }

    void CheckGaze()
    {
        Ray gazeRay = new(transform.position, transform.rotation * Vector3.forward);
        LayerMask mask = LayerMask.GetMask("Default");

        if (Physics.Raycast(gazeRay, out RaycastHit hit, 5, mask, QueryTriggerInteraction.Collide))
        {
            // Check if we're hitting the same object
            if (lastHit == hit.transform && hasUI) keepUI = true;
            else keepUI = false;
            lastHit = hit.transform;

            // Perform relevant functions
            playerInputManager.currentObject = lastHit.gameObject;
            if (hit.transform.CompareTag("SitPoint")) ReadySitProtocol();
            else if (hit.transform.CompareTag("Player")) ReadyPlayerProtocol();
            else if (hit.transform.CompareTag("Whiteboard")) ReadyWhiteboardProtocol();
            else
            {
                keepUI = false;
                playerInputManager.readyAction = ActionType.None;
                playerInputManager.currentObject = null;
            }
        }
        else keepUI = false;
    }


    void ReadySitProtocol()
    {
        InputAction inputAction = InputSystem.actions["Action"];
        StartCoroutine(FadeInUI($"Press {inputAction.GetBindingDisplayString(0)} to Sit"));
        playerInputManager.readyAction = ActionType.Sit;
    }

    void ReadyWhiteboardProtocol()
    {
        InputAction inputAction = InputSystem.actions["Action"];
        StartCoroutine(FadeInUI($"Press {inputAction.GetBindingDisplayString(0)} to Draw"));
        playerInputManager.readyAction = ActionType.Draw;
    }

    void ReadyPlayerProtocol()
    {
        playerInputManager.readyAction = ActionType.Player;
    }


    IEnumerator FadeInUI(string text)
    {
        keepUI = true;
        hasUI = true;
        float segment = 0.01f;

        // Fade in
        promptText.text = text;
        Color initialColor = promptText.color;

        for (float i = 0; i < 1; i += segment)
        {
            initialColor.a = i;
            promptText.color = initialColor;
        }

        while (keepUI)
        {
            yield return new WaitForEndOfFrame();
        }

        // Fade out
        for (float i = 1; i > 0; i -= segment)
        {
            initialColor.a = i;
            promptText.color = initialColor;
        }

        initialColor.a = 0;
        promptText.color = initialColor;

        hasUI = false;
    }
}