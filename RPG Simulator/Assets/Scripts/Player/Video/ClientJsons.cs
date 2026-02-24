using System;
using UnityEngine;

public class ClientJsons : MonoBehaviour
{
}


[Serializable]
public class CandidateInit : IJson<CandidateInit>
{
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;

    public static CandidateInit FromJson(string json)
    {
        return JsonUtility.FromJson<CandidateInit>(json);
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}

[Serializable]
public class SessionDescription : IJson<SessionDescription>
{
    public string type;
    public string sdp;
    public static SessionDescription FromJson(string json)
    {
        return JsonUtility.FromJson<SessionDescription>(json);
    }
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}

