using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

[Serializable]
public class ConnectionData
{
    public Vector2 pos;
    public int outNode;
    public int inNode;

    public ConnectionData(Vector2 pos, int outNode, int inNode) {
        this.pos = pos;
        this.outNode = outNode;
        this.inNode = inNode;
    }
}

// Nodes will be saved with their index so they can be placed back within the same point in the list on load.
//      Connections will reference two indices, meaning the order in the list will matter.
[Serializable]
public class NodeData
{
    public int index;
    public Vector2 pos;
    public float length;
    public float width;
    public float elevation;

    public NodeData(int index, Vector2 pos, float length, float width, float elevation) {
        this.index = index;
        this.pos = pos;
        this.length = length;
        this.width = width;
        this.elevation = elevation;
    }
}

[Serializable]
public class GeneratedTerrain
{
    public List<MeshData> terrain;

    public GeneratedTerrain() {
        terrain = new List<MeshData>();
    }
    public GeneratedTerrain(List<MeshData> terrain)
    {
        this.terrain = terrain;
    }
}

// This is going to be final curated data for the nodes.
public class GraphData
{
    // This will be the X, Y screen pos of the root node in the graph.
    //      We can use the node positions as an offset to replace the nodes on the graph load.
    public Vector2 worldPosOffset; 

    // This will include an index, x-y positions, width, length and elevation.
    public List<NodeData> nodes;

    // This will simply be 2 indices: Two nodes that share a connection.
    public List<ConnectionData> connections;
}

// Modified Source Code Based on https://gram.gs/gramlog/creating-node-based-editor-unity/
public class TerrainEditor : EditorWindow
{
    // TEMP - DEFAULT MESHES
    Vector3[] defaultIslandMesh = {
        new Vector3(-1, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 0, -1),
        new Vector3(-1, 0, -1)
    };

    int[] defaultIslandIndices = {
        0, 1, 2,
        0, 2, 3
    };

    Vector3[] defaultBridgeMesh = {
        new Vector3(-1, 0, 0.5f),
        new Vector3(1, 0, 0.5f),
        new Vector3(1, 0, -0.5f)
    };

    int[] defaultBridgeIndices = {
        0, 1, 2
    };

    float distanceScalar = 10;
    [SerializeField] protected GameObject prefab_GenTerrain;
    private GraphData currentGraph;
    private GraphData CurrentGraph { get { return currentGraph; } }
    private string graphPath = "Assets/Anvil/graphData.txt";

    // Icon Textures
    private Texture2D infoIconSmall;

    // Here is our collections of Nodes and Connections
    private List<Node> nodes;
    private List<Connection> connections; // connections[i].OutPoint > connections[i].InPoint

    // Menu Bar
    private Rect menuBar;
    private float menuBarHeight = 20f;

    // Toggles
    private bool autoGenerate = false;
    private bool autoSave = true;

    // Here is the styling references
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;

    // We need to keep track of the connection points that are clicked and dragged
    private ConnectionPoint selectedInPoint;
    private ConnectionPoint selectedOutPoint;

    // Canvas Drag
    private Vector2 offset;
    private Vector2 drag;


    [MenuItem("Window/Anvil Editor")]
    private static void OpenWindow()
    {
        TerrainEditor window = GetWindow<TerrainEditor>();
        window.titleContent = new GUIContent("Anvil Editor");
    }

    private void OnEnable()
    {
        // Load Icon files
        infoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;

        // Set the style of the individual nodes
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);

        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
 
        // Set the style of the connection points
        inPointStyle = new GUIStyle();
        inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn.png") as Texture2D;
        inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn on.png") as Texture2D;
        inPointStyle.border = new RectOffset(4, 4, 12, 12);
 
        outPointStyle = new GUIStyle();
        outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn.png") as Texture2D;
        outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn on.png") as Texture2D;
        outPointStyle.border = new RectOffset(4, 4, 12, 12);

        LoadGraph(graphPath);

    }

    private void OnDisable()
    {
        SaveChanges();
    }

    // This is basically our "update"
    private void OnGUI()
    {
        
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);

        DrawConnections();
        DrawConnectionLine(Event.current);

        DrawNodes();

        ProcessNodeEvents(Event.current);
        ProcessEvents(Event.current);

        DrawMenuBar();
 
        if (GUI.changed) Repaint();
    }

    private void DrawMenuBar()
    {
        menuBar = new Rect(0, 0, position.width, menuBarHeight);

        // Open Menu Area
        GUILayout.BeginArea(menuBar, EditorStyles.toolbar);

        GUILayout.BeginHorizontal();

        autoGenerate = GUILayout.Toggle(autoGenerate, new GUIContent("Auto"), EditorStyles.toolbarButton, GUILayout.Width(50));
        if (GUILayout.Button(new GUIContent("Generate Terrain"), EditorStyles.toolbarButton, GUILayout.Width(150))) {

            SaveChanges();
            GenerateTerrain();
        }

        GUILayout.FlexibleSpace();

        autoSave = GUILayout.Toggle(autoSave, new GUIContent("Auto"), EditorStyles.toolbarButton, GUILayout.Width(50));
        if (GUILayout.Button(new GUIContent("Save Graph"), EditorStyles.toolbarButton, GUILayout.Width(100))) {

            SaveChanges();
        }

        GUILayout.EndHorizontal();

        // Close Menu Area
        GUILayout.EndArea();
    }

    private async void GenerateTerrain() {
        Debug.Log("Generating New Terrain");

        GenTerrain terrain = Instantiate(prefab_GenTerrain).GetComponent<GenTerrain>();
        terrain.InitTerrain();

        // Create JSON for the graph.
        string graphJSON = JsonUtility.ToJson(currentGraph);

        // Open an Http Client Request
        HttpClient client = new HttpClient(new JsonSerializationOption());
        string url = "http://10.0.0.63:105/Generate/";

        // Await response from the generator.
        GeneratedTerrain genTerrain = await client.Post<GeneratedTerrain>(url, graphJSON);
        Debug.Log( $"From Terrain Generator, {genTerrain.terrain.Count} mesh objects : " + genTerrain.terrain[0].verts[1].x + ", Indices " + genTerrain.terrain[0].indices[2] );

        int count = 0;
        foreach (MeshData mesh in genTerrain.terrain)
        {

            // The islands are always ordered first
            if (count < currentGraph.nodes.Count) {
                
                Vector3 nodePos = mesh.worldPos / distanceScalar;
                terrain.AddIsland(nodePos, mesh.verts, mesh.indices);
            }

            // Once all islands are placed, bridges are ordered last.
            else {
                Vector3 nodePos = mesh.worldPos / distanceScalar;
                terrain.AddBridge(nodePos, mesh.verts, mesh.indices);
            }

            count++;
            
        }

        terrain.transform.localScale = new Vector3(7, 7, 7);
        terrain.SetColliders();


        // Add an island for each node. We'll need to get the node data of the active graph.
        // List<NodeData> nodeData = currentGraph.nodes;
        // foreach (NodeData node in nodeData)
        // {
        //     // Generate Mesh Data.
        //     Vector3[] genMesh = (Vector3[]) defaultIslandMesh.Clone();
        //     int[] genIndices = (int[]) defaultIslandIndices.Clone();

        //     // Add new mesh to generated terrain.
        //     Vector3 nodePos = new Vector3(node.pos.x, 0, node.pos.y) / distanceScalar;
        //     terrain.AddIsland(nodePos, genMesh, genIndices);
        // }

        // List<ConnectionData> connectionData = currentGraph.connections;
        // foreach (ConnectionData connection in connectionData)
        // {
        //     // Generate Mesh Data.
        //     Vector3[] genMesh = (Vector3[]) defaultBridgeMesh.Clone();
        //     int[] genIndices = (int[]) defaultBridgeIndices.Clone();

        //     // Add new mesh to generated terrain.
        //     Vector3 bridgePos = new Vector3(connection.pos.x, 0, connection.pos.y) / distanceScalar;
        //     terrain.AddBridge(bridgePos, genMesh, genIndices);
        // }
    }

    // Draw the grid lines across the canvas.
    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);
 
        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
 
        offset += drag * 0.5f;
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);
 
        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }
 
        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }
 
        Handles.color = Color.white;
        Handles.EndGUI();
    }

    // Draw a Bezier curve for the current connection.
    private void DrawConnectionLine(Event e)
    {
        if (selectedInPoint != null && selectedOutPoint == null)
        {
            Handles.DrawBezier(
                selectedInPoint.rect.center,
                e.mousePosition,
                selectedInPoint.rect.center + Vector2.left * 50f,
                e.mousePosition - Vector2.left * 50f,
                Color.white,
                null,
                2f
            );
 
            GUI.changed = true;
        }
 
        if (selectedOutPoint != null && selectedInPoint == null)
        {
            Handles.DrawBezier(
                selectedOutPoint.rect.center,
                e.mousePosition,
                selectedOutPoint.rect.center - Vector2.left * 50f,
                e.mousePosition + Vector2.left * 50f,
                Color.white,
                null,
                2f
            );
 
            GUI.changed = true;
        }
    }

    // For each node, call their draw method.
    private void DrawNodes()
    {
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Draw();
            }
        }
    }

    // For each connection, call their draw method.
    private void DrawConnections()
    {
        if (connections != null)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                connections[i].Draw();
            } 
        }
    }

    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;

        switch (e.type)
        {
            // Right Click in Canvas
            case EventType.MouseDown:
                if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 2)
                {
                    OnDrag(e.delta);
                }
                break;
        }
    }
    
    // Move all nodes by the delta of the canvas.
    //      TODO - Do not update their spacial position.
    private void OnDrag(Vector2 delta)
    {
        drag = delta;
 
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Drag(delta);
            }
        }
 
        GUI.changed = true;
    }

    private void ProcessNodeEvents(Event e)
    {
        if (nodes != null)
        {
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                bool guiChanged = nodes[i].ProcessEvents(e);
 
                if (guiChanged)
                {
                    GUI.changed = true;
                }
            }
        }
    }

    // This is where we can add contextual action menus.
    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition)); 
        genericMenu.ShowAsContext();
    }
 
    private void OnClickAddNode(Vector2 mousePosition)
    {
        if (nodes == null)
        {
            nodes = new List<Node>();
        }

         // This constructor includes function evocation for connection events.
        nodes.Add(new Node(mousePosition, 100, 100, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
    }

    private void OnLoadAddNodes(GraphData graphData) {

        List<NodeData> newNodes = graphData.nodes;
        nodes = new List<Node>();
        
        // Fill the list with the requisite number of slots.
        for (int i = 0; i < newNodes.Count; ++i) {
            nodes.Add(null);
        }

        for (int i = 0; i < newNodes.Count; ++i)
        {
            // Debug.Log("Loading node at index " + newNodes[i].index);
            Vector2 nodePos = new Vector2(newNodes[i].pos.x + (position.width), -newNodes[i].pos.y + (position.height));
            nodes[newNodes[i].index] = new Node(nodePos, newNodes[i].width, newNodes[i].length, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode);
        }

        // offset = 
    }

    // ---
    //  OnClick Connection Events
    // ---

    private void OnClickOutPoint(ConnectionPoint outPoint)
    {
        selectedOutPoint = outPoint;
 
        if (selectedInPoint != null)
        {
            if (selectedOutPoint.node != selectedInPoint.node)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }

    private void OnClickInPoint(ConnectionPoint inPoint)
    {
        selectedInPoint = inPoint;
 
        if (selectedOutPoint != null)
        {
            if (selectedOutPoint.node != selectedInPoint.node)
            {
                CreateConnection();
                ClearConnectionSelection(); 
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }
 
    private void OnClickRemoveConnection(Connection connection)
    {
        connections.Remove(connection);
    }

    private void OnClickRemoveNode(Node node)
    {
        if (connections != null)
        {
            List<Connection> connectionsToRemove = new List<Connection>();
 
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].inPoint == node.inPoint || connections[i].outPoint == node.outPoint)
                {
                    connectionsToRemove.Add(connections[i]);
                }
            }
 
            for (int i = 0; i < connectionsToRemove.Count; i++)
            {
                connections.Remove(connectionsToRemove[i]);
            }
 
            connectionsToRemove = null;
        }
 
        nodes.Remove(node);
    }
 
    private void CreateConnection()
    {
        if (connections == null)
        {
            connections = new List<Connection>();
        }
 
        connections.Add(new Connection(selectedInPoint, selectedOutPoint, OnClickRemoveConnection));
    }

    private void OnLoadAddConnections(GraphData loadedGraph)
    {
        
        List<ConnectionData> newConnections = loadedGraph.connections;
        connections = new List<Connection>();
        
        // Fill the list with the requisite number of slots.
        for (int i = 0; i < newConnections.Count; ++i) {
            ConnectionPoint inPoint = nodes[newConnections[i].inNode].inPoint;
            ConnectionPoint outPoint = nodes[newConnections[i].outNode].outPoint;

            connections.Add(new Connection (inPoint, outPoint, OnClickRemoveConnection) );
        }
    }
 
    private void ClearConnectionSelection()
    {
        selectedInPoint = null;
        selectedOutPoint = null;
    }

    public override void SaveChanges()
    {
        // If some nodes exist on the graph, save their data.
        if (nodes != null && nodes.Count >= 0) {

            // Your custom save procedures here
            GraphData graph = new GraphData();

            // 1. Set the world pos offset by the root node.
            graph.worldPosOffset = new Vector2(nodes[0].rect.center.x, nodes[0].rect.center.y);

            // 2. Create a list of node data objects from existing nodes.
            List<NodeData> nodeData = FetchNodeData(graph);

            // 3. Create a list of node connection data objects
            List<ConnectionData> connectionData = FetchConnectionData(graph);
            
            // 4. Assign the newly created lists.
            graph.nodes = nodeData;
            graph.connections = connectionData;

            // 5. Create JSON Object of the graph.
            WriteGraphToFile(graph);
            currentGraph = graph;

            // Debug.Log($"{this} saved successfully!!!");
        }

        base.SaveChanges();
    }

    private List<NodeData> FetchNodeData(GraphData graph) {
        List<NodeData> nodeData = new List<NodeData>();
        if (nodes != null) {
            for (int i = 0; i < nodes.Count; ++i) {

                // Each node must set it's position based on the world position offset. 
                float nodePosX = nodes[i].rect.center.x - graph.worldPosOffset.x;
                float nodePosY = -(nodes[i].rect.center.y - graph.worldPosOffset.y);

                NodeData newNode = new NodeData(i, new Vector2(nodePosX, nodePosY), nodes[i].islandLength, nodes[i].islandWidth, nodes[i].islandElevation);
                nodeData.Add(newNode);

            }
        }

        return nodeData;
    }

    private List<ConnectionData> FetchConnectionData(GraphData graph) {
        List<ConnectionData> connectionData = new List<ConnectionData>();
        if (connections != null) {
            for (int j = 0; j < connections.Count; ++j) {

                // Each connection has 2 node references.
                Vector2 connectionPos = new Vector2();
                connectionPos.x = connections[j].MidPoint.x - graph.worldPosOffset.x;
                connectionPos.y = -(connections[j].MidPoint.y - graph.worldPosOffset.y);

                int connectionInNodeIndex = nodes.IndexOf(connections[j].inPoint.node);
                int connectionOutNodeIndex = nodes.IndexOf(connections[j].outPoint.node);

                ConnectionData newConnection = new ConnectionData(connectionPos, connectionInNodeIndex, connectionOutNodeIndex);
                connectionData.Add(newConnection);
            }
        }
        
        return connectionData;
    }

    private void WriteGraphToFile(GraphData graph) {
        
        string graphJSON = JsonUtility.ToJson(graph);

        // Debug.Log("Saving " + graph.nodes.Count + " nodes, with " + graph.connections.Count + " connections.");
        // Debug.Log(graphJSON);

        if (!File.Exists(graphPath))
        {
            // Check if directory exists.
            if (!Directory.Exists("Assets/Anvil")) {
                Directory.CreateDirectory("Assets/Anvil");
            }

            // Debug.Log("Creating file at path " + graphPath);
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(graphPath))
            {
                sw.WriteLine(graphJSON);
            }
        }

        else {
            // Debug.Log("Opening file at path " + graphPath);
            // Open the file and overwrite.
            using (StreamWriter sw = new StreamWriter(graphPath, false))
            {
                sw.WriteLine(graphJSON);
            }
        }
    }

    public bool LoadGraph(string filePath) {
        GraphData loadedGraph = ReadGraphFromFile(filePath);

        if (loadedGraph == null) {
            return false;
        }

        // Create a new node element for each node loaded.
        // Debug.Log("Nodes Loaded : " + loadedGraph.nodes.Count + " total");

        OnLoadAddNodes(loadedGraph);

        OnLoadAddConnections(loadedGraph);

        return true;
    }

    private GraphData ReadGraphFromFile(string filePath) {
        GraphData loadedGraph = null;
        string graphJSON;

        if (File.Exists(graphPath))
        {
            using (StreamReader sr = new StreamReader(graphPath))
            {
                graphJSON = sr.ReadLine();
            }

            
            loadedGraph = JsonUtility.FromJson<GraphData>(graphJSON);
        }

        return loadedGraph;
    }
    

}
