using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonMovement : NetworkBehaviour
{
    #region Setup
    public float speed = 5;
    bool canMove = true;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;

    Vector2 targetVelocity;

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new();


    [Header("Jumping")]
    public float jumpStrength = 2;
    public event System.Action Jumped;

    [SerializeField, Tooltip("Prevents jumping when the transform is in mid-air.")]
    GroundCheck groundCheck;

    [Header("Sitting")]
    bool isSitting = false;
    Quaternion currentSitRot;
    Vector3 currentSitPos;


    [Header("Components")]
    [SerializeField] NetworkAnimator networkAnimator;
    [SerializeField] FirstPersonLook look;
    public NetworkTransform networkTransform { get; private set; }
    NetworkRigidbody networkRigidbody;

    [Header("Animation Hashes")]
    private readonly int _isWalkingBool = Animator.StringToHash("isWalking");
    private readonly int _sitTrigger = Animator.StringToHash("Sit");
    private readonly int _standTrigger = Animator.StringToHash("Stand");


    [Header("Debug")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Player;

    void Reset()
    {
        // Try to get groundCheck.
        groundCheck = GetComponentInChildren<GroundCheck>();
    }


    private void Start()
    {
        // Get the rigidbody on this.
        networkRigidbody = GetComponent<NetworkRigidbody>();
        networkTransform = GetComponent<NetworkTransform>();
        Cursor.lockState = CursorLockMode.Locked;
        canMove = true;

        SetSpawnLocation();
        ResetInputSystem();
    }

    private void SetSpawnLocation()
    {
        if (!IsOwner) return;
        if (IsHost) MoveTo(new Vector3(-3, 2, -3));
        else MoveTo(new Vector3(-6, 2, -3));

    }

    private void ResetInputSystem()
    {
        // ensure only one input is enabled
        InputSystem.actions.Disable();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.currentActionMap?.Enable();

        playerInput.enabled = false;

        if (IsOwner)
        {
            playerInput.actions = InputSystem.actions;
            playerInput.enabled = true;
        }
    }

    #endregion


    #region Movement

    void FixedUpdate()
    {
        if (!IsOwner && !DebugSettings.Instance.EnableInput) return;

        // Apply movement.
        networkRigidbody.Rigidbody.isKinematic = false;
        networkRigidbody.Rigidbody.linearVelocity = (networkTransform.transform.rotation * new Vector3(targetVelocity.x, networkRigidbody.Rigidbody.linearVelocity.y, targetVelocity.y));

        //GetComponent<Rigidbody>().linearVelocity = (transform.rotation * new Vector3(targetVelocity.x, GetComponent<Rigidbody>().linearVelocity.y, targetVelocity.y));
        
        if (targetVelocity.x != 0 || targetVelocity.y != 0) networkAnimator.SetTrigger(_isWalkingBool, true);
        else networkAnimator.SetTrigger(_isWalkingBool, false);
    }

    private void OnMove(InputValue value)
    {
        if (!IsOwner && !DebugSettings.Instance.EnableInput) return;

        if (!Mathf.Approximately(value.Get<Vector2>().magnitude, 0) && canMove)
        {
            // Get targetMovingSpeed.
            float targetMovingSpeed = IsRunning ? runSpeed : speed;
            if (speedOverrides.Count > 0)
            {
                targetMovingSpeed = speedOverrides[^1]();
            }

            Vector2 inputVelocity = value.Get<Vector2>();

            targetVelocity = new(inputVelocity.x * targetMovingSpeed, inputVelocity.y * targetMovingSpeed);
            if (DebugSettings.Instance.ShouldLog(logLevel)) { Debug.Log($"[FirstPersonMovement] OnMove InputVector: {inputVelocity}"); }
        }
        else targetVelocity = Vector2.zero;
    }

    public void MoveTo(Vector3 pos)
    {
        networkTransform.transform.position = pos;
    }

    private void OnSprint(InputValue value)
    {
        // Update IsRunning from input.
        IsRunning = canRun && value.isPressed;
    }

    public void StopMovement()
    {
        targetVelocity = Vector2.zero;
        look.StopMovement();
    }

    public void ResumeMovement()
    {
        look.ResumeMovement();
    }

    #endregion


    #region Jump

    void OnJump()
    {
        if (!IsOwner && !DebugSettings.Instance.EnableInput) return;
        if (groundCheck && !groundCheck.isGrounded) return;
        if (DebugSettings.Instance.ShouldLog(logLevel)) { Debug.Log("[Jump] Jumping..."); }
        networkRigidbody.Rigidbody.AddForce(100 * jumpStrength * Vector3.up);
        Jumped?.Invoke();
    }

    #endregion

    #region Sit 

    public void OnSit(GameObject chair)
    {
        if (DebugSettings.Instance.ShouldLog(logLevel)) Debug.Log("[OnSit] Sitting...");

        isSitting = !isSitting;

        if (isSitting)
        {
            // align with chair
            currentSitPos = new Vector3(chair.transform.position.x, networkTransform.transform.position.y, chair.transform.position.z - 1f);
            currentSitRot = Quaternion.LookRotation(chair.transform.forward, networkTransform.transform.up);
            networkTransform.transform.SetPositionAndRotation(currentSitPos, currentSitRot);
                
            // animate
            networkAnimator.SetTrigger(_sitTrigger);

            // disable other movement
            canMove = false;
            look.MoveHead(true);

        }
        else
        {
            // re-enable other movement
            canMove = true;
            look.MoveHead(false);

            // re-align with chair
            networkTransform.transform.SetPositionAndRotation(currentSitPos, currentSitRot);

            // animate
            networkAnimator.SetTrigger(_standTrigger);
        }
    }

    #endregion
}