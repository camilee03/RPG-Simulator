using UnityEngine;

public class DebugSettings : MonoBehaviour
{
    public enum LogLevel
    {
        None,
        Networking,
        Vivox,
        Player,
        Video,
        All
    }

    public static DebugSettings Instance { get; private set; }

    private void Awake()
    {
        if (this != Instance && Instance != null) Destroy(this);
        else Instance = this; 
    }

    [SerializeField] LogLevel LogType;

    [field: SerializeField]
    public bool DoVivox { get; private set; }

    [field: SerializeField]
    public bool EnableInput { get; private set; }

    public bool ShouldLog(LogLevel level)
    {
        return LogType == LogLevel.All || LogType == level;
    }
}
