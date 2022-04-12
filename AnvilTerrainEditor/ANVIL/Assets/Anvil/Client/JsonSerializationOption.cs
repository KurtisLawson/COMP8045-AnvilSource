using UnityEngine;
using Newtonsoft.Json;
using System;

public class JsonSerializationOption : ISerializationOption
{
    public string ContentType { get { return "application/json"; } }

    // Takes some string and deserializes it into a compatible JSON object.
    public T Deserialize<T>(string text)
    {
        try
        {
            // T result = JsonUtility.DeserializeObject<T>(text);
            T result = JsonUtility.FromJson<T>(text);
            Debug.Log($"Success : {text}");
            return result;
        }

        catch(Exception ex)
        {
            Debug.LogError($"{this} could not parse json {text}. {ex.Message}");
            return default;
        }
    }
}
