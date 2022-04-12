using System;
using UnityEditor;
using UnityEngine;

// Modified Source Code Based on https://gram.gs/gramlog/creating-node-based-editor-unity/
public class Node
{
    // Data Fields
    public float islandWidth = 100;
    public float islandLength = 100;
    public float islandElevation = 100;

    // Visual Config
    public Rect rect;
    private Rect menuBar;
    private float menuBarHeight = 30f;

    // Internal State Fields
    public string title;
    public bool isDragged;
    public bool isSelected;

    // These are our style references
    private GUIStyle style;
    private GUIStyle textStyle;
    private GUIStyle defaultNodeStyle;
    private GUIStyle selectedNodeStyle;

    // Action Events
    private Action<Node> OnRemoveNode;

    // These are our connection buttons.
    //      TODO - We may only need one of these in the future.
    public ConnectionPoint inPoint;
    public ConnectionPoint outPoint;

    // Our constructor must include the actions that will be invoked when the ConnectionPoints are clicked / dragged.
    public Node(Vector2 position, float width, float height, GUIStyle nodeStyle, GUIStyle selectedStyle, GUIStyle inPointStyle, GUIStyle outPointStyle, Action<ConnectionPoint> OnClickInPoint, Action<ConnectionPoint> OnClickOutPoint, Action<Node> OnClickRemoveNode)
    {
        rect = new Rect(position.x, position.y, width, height);

        // Set our style references
        style = nodeStyle;
        defaultNodeStyle = nodeStyle;
        selectedNodeStyle = selectedStyle;

        // Create our connection points.
        inPoint = new ConnectionPoint(this, ConnectionPointType.In, inPointStyle, OnClickInPoint);
        outPoint = new ConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint);

        // Subscribe to events
        OnRemoveNode = OnClickRemoveNode;

    }

    // Update the position of the node.
    public void Drag(Vector2 delta)
    {
        rect.position += delta;
    }
    
    // Draw the two connection points and the Node rect
    public void Draw()
    {
        inPoint.Draw();
        outPoint.Draw();
        GUI.Box(rect, title, style);

        GUILayout.BeginArea(rect);

        // Open Menu Area
        menuBar = new Rect(0, 0, rect.width, menuBarHeight);
        GUILayout.BeginArea(menuBar, EditorStyles.toolbar);

        // World Pos X and Y
        String s = "x : " + rect.position.x + ", y : " + rect.position.y;
        GUIStyle labelStyle = new GUIStyle();
        GUILayout.Label(s);

        // Close Menu Area
        GUILayout.EndArea();

        DrawDataFields();

        GUILayout.EndArea();
        
    }

    private void DrawDataFields() {
        

        // Length, Width
        // EditorGUILayout.FloatField("Width", islandWidth);
        // EditorGUILayout.FloatField("Length", islandLength);

        // Elevation

    }
    
    public bool ProcessEvents(Event e)
    {
        // The event has come in, and can be read using 'type'
        switch (e.type)
        {
            // EVENT TYPE - Mouse Click Down
            case EventType.MouseDown:

                // Left Mouse Click
                if (e.button == 0)
                {
                    // If we clicked within the Node rect, then it is considered "dragged"
                    if (rect.Contains(e.mousePosition))
                    {
                        isDragged = true;
                        isSelected = true;
                        style = selectedNodeStyle;

                        // Update GUI state
                        GUI.changed = true;
                    }

                    // If we deselect the Node rect, then explicitly set selected to false.
                    else
                    {
                        isSelected = false;
                        style = defaultNodeStyle;

                        // Update GUI state
                        GUI.changed = true;
                    }
                }

                // Right Mouse Click (within selected node)
                if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
                {
                    ProcessContextMenu();
                    e.Use();
                }

                break;

            // EVENT TYPE - Mouse Click Release
            case EventType.MouseUp:
                isDragged = false;
                break;

            // Mouse Position Changed
            case EventType.MouseDrag:
                // No button has been clicked, and the node is being dragged.
                if (e.button == 0 && isDragged)
                {
                    Drag(e.delta);
                    e.Use();
                    return true;
                }
                break;
        }
        
        return false;
    }

    // This is the context menu when Right-Clicking a selected node.
    private void ProcessContextMenu()
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
        genericMenu.ShowAsContext();
    }
    
    // Function for removing a selected node.
    private void OnClickRemoveNode()
    {
        if (OnRemoveNode != null)
        {
            OnRemoveNode(this);
        }
    }
}
