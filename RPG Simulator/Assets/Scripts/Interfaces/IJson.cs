using UnityEngine;

public interface IJson<T>
{
    string ToJson();
    static T FromJson(string jsonString)
    {
        return JsonUtility.FromJson<T>(jsonString);
    }
}
