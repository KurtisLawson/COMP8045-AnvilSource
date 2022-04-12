using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// [ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    
    // Server communication fields   
    public string connectionIP = "127.0.0.1";
    public int connectionPort = 25001;
    protected IPAddress localAdd;

    // Threading / TCP References
    protected Thread genThread;
    TcpListener listener;
    TcpClient client;

    // Received data field.
    string genJSON;
    List<MeshData> receivedTerrain = new List<MeshData>(); // This will be populated by parsing genJSON
    
    Vector3 receivedPos = Vector3.zero;
    bool running;

    // Start is called before the first frame update
    void Start()
    {
        GenerateTerrain();
    }

    private void Update()
    {
        transform.position = receivedPos; //assigning receivedPos in SendAndReceive()
    }
    
    public void GenerateTerrain()
    {
        Debug.Log("Opening Thread... ");
        // Assign the Generation Script to a new Thread.
        ThreadStart ts = new ThreadStart(Generate);
        genThread = new Thread(ts);

        // Start the Thread.
        genThread.Start();

    }

    protected void Generate()
    {
        localAdd = IPAddress.Parse(connectionIP);
        listener = new TcpListener(IPAddress.Any, connectionPort);
        listener.Start();

        client = listener.AcceptTcpClient();

        running = true;
        while (running)
        {
            SendAndReceive();
        }
        listener.Stop();

    }

    private void SendAndReceive()
    {
        NetworkStream nwStream = client.GetStream();
        byte[] buffer = new byte[client.ReceiveBufferSize];

        // -- receive from python - host data
        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize); // Get data as bytes from Python
        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead); // Convert bytes to a string

        if (dataReceived != null) {
            //---Using received data---
            receivedPos = StringToVector3(dataReceived); //<-- assigning receivedPos value from Python
            print("received pos data, and moved the Cube!");

            //---Sending Data to Host----
            byte[] myWriteBuffer = Encoding.ASCII.GetBytes("Hey I got your message Python! Do You see this massage?"); //Converting string to byte data
            nwStream.Write(myWriteBuffer, 0, myWriteBuffer.Length); //Sending the data in Bytes to Python
        }
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }
}
