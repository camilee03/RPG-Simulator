using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class FirstPersonLook : NetworkBehaviour
{
    [SerializeField] NetworkTransform character;
    [SerializeField] NetworkTransform head;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;

    NetworkTransform networkTransform;

    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Player;

    bool canMove;
    bool headOnly = false;

    void Start()
    {
        // Get the character from the FirstPersonMovement in parents.
        if (character == null) character = GetComponentInParent<FirstPersonMovement>().networkTransform;
        networkTransform = GetComponent<NetworkTransform>();

        // Disable unessential components
        AudioListener listener = GetComponent<AudioListener>();
        CinemachineCamera camera = GetComponent<CinemachineCamera>();
        if (IsOwner || DebugSettings.Instance.OfflineTesting)
        {
            this.tag = "MainCamera";
            camera.Priority = 1;
            listener.enabled = true;
            canMove = true;
        }
        else { camera.Priority = -1; listener.enabled = false; }
    }

    void Update()
    {
        if (!IsOwner && !DebugSettings.Instance.OfflineTesting) return;


        if (canMove)
        {
            // Get smooth velocity.
            Vector2 mouseDelta = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
            frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
            velocity += frameVelocity;
            velocity.y = Mathf.Clamp(velocity.y, -90, 90);

            if (headOnly)
            {
                // only rotate head
                Quaternion lrRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
                Quaternion udRotation = Quaternion.AngleAxis(-velocity.y - 180, Vector3.right);
                head.transform.localRotation = udRotation * lrRotation;
            }
            else
            {
                // Rotate head (and therefore camera) up-down and controller left-right from velocity.
                //networkTransform.transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
                head.transform.localRotation = Quaternion.AngleAxis(-velocity.y-180, Vector3.right);
                character.transform.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
            }
        }
    }

    public void StopMovement()
    {
        frameVelocity = Vector2.zero;
        canMove = false;
    }

    public void ResumeMovement()
    {
        canMove = true;
    }

    public void MoveHead(bool check)
    {
        // reset movement 
        StopMovement();
        ResumeMovement();

        headOnly = check;
    }
}
