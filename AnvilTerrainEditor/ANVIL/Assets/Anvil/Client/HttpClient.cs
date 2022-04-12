using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class HttpClient
{
    private readonly ISerializationOption serializationOption;
    public HttpClient(ISerializationOption serializationOption)
    {
        this.serializationOption = serializationOption;
    }

    public async Task<TResultType> Post<TResultType>(string url, string payload)
    {
        try
        {
            Debug.Log("Posting to " + url);
            Debug.Log("Payload : " + payload);
            
            // Establish a web request
            using var www = UnityWebRequest.Put(url, payload);

            // Set the content type - What are we dealing with?
            www.SetRequestHeader("Content-Type", serializationOption.ContentType);

            var operation = www.SendWebRequest();

            while (!operation.isDone) // Async await for response
            { 
                await Task.Yield();
            }

            var response = www.downloadHandler.text; // Get the response packet

            // Check for success
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Failed : {www.error}");
                return default;
            }

            Debug.Log("Parsing " + response);

            TResultType result = serializationOption.Deserialize<TResultType>(response);
            return result;

        }

        catch(Exception ex)
        {
            Debug.LogError($"{nameof(Post)} failed. {ex.Message}");
            return default;
        }
    }

    public async Task<TResultType> Get<TResultType>(string url)
    {
        try
        {
            // Establish a web request
            using var www = UnityWebRequest.Get(url);

            // Set the content type - What are we dealing with?
            www.SetRequestHeader("Content-Type", serializationOption.ContentType);

            var operation = www.SendWebRequest();

            while (!operation.isDone) // Async await for response
            { 
                await Task.Yield();
            }

            var response = www.downloadHandler.text; // Get the response packet

            // Check for success
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Failed : {www.error}");
                return default;
            }

            Debug.Log("Parsing " + response);

            TResultType result = serializationOption.Deserialize<TResultType>(response);
            return result;
        }

        catch(Exception ex)
        {
            Debug.LogError($"{nameof(Get)} failed. {ex.Message}");
            return default;
        }

    }
}
