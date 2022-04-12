using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class JsonMesh
{
    public float[] worldPos { get; set; }
    public float[][] verts { get; set; }
    public int[] indices { get; set; }
}

public class RestTest : MonoBehaviour
{

    public async void TestGet()
    {
        HttpClient client = new HttpClient(new JsonSerializationOption());
        // string url = "https://jsonplaceholder.typicode.com/todos/1"; // Free API url for testing JSON
        string url = "http://10.0.0.63:105/Generate/";

        User user = await client.Get<User>(url);

        Debug.Log($"User : {user.id}, Title : {user.title}");

    }

    [ContextMenu("Test Post Request")]
    public async void TestPost()
    {
        HttpClient client = new HttpClient(new JsonSerializationOption());
        // string url = "https://jsonplaceholder.typicode.com/todos/1"; // Free API url for testing JSON
        string url = "http://10.0.0.63:105/Generate/";

        // User user = await client.Post<User>(url, "");
        // Debug.Log($"User : {user.id}, Title : {user.title}, Vert of [1] : {user.verts[1]}");

        JsonMesh mesh = await client.Post<JsonMesh>(url, "");
        Debug.Log($"User : {mesh.worldPos[0]}, Vert : {mesh.verts[0][1]}, Indices : {mesh.indices[2]}");

    }
}
