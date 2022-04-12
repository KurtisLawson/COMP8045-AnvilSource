using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

// Modified Source Code Based on https://gram.gs/gramlog/creating-node-based-editor-unity/
public enum ConnectionPointType { In, Out }

// This is the little node button we click and drag to create a connection.
public class ConnectionPoint
{
    public Rect rect;
 
    public ConnectionPointType type;
 
    public Node node;
 
    public GUIStyle style;
 
    public Action<ConnectionPoint> OnClickConnectionPoint;

    // Each Connection Point should belong to a single node.
    public ConnectionPoint(Node node, ConnectionPointType type, GUIStyle style, Action<ConnectionPoint> OnClickConnectionPoint)
    {
        this.node = node;
        this.type = type;
        this.style = style;
        this.OnClickConnectionPoint = OnClickConnectionPoint;
        rect = new Rect(0, 0, 20f, 80f);
    }

    // A connection point draws a button at a specified point. 
    public void Draw()
    {
        rect.y = node.rect.y + (node.rect.height * 0.5f) - rect.height * 0.5f + 5f;

        // We'll make two connection points, an "In" and an "Out".
        //      TODO - For the Anvil Editor, the direction should be agnostic.
        switch (type)
        {
            case ConnectionPointType.In:
                rect.x = node.rect.x - rect.width + 8f;
                break;
 
            case ConnectionPointType.Out:
                rect.x = node.rect.x + node.rect.width - 8f;
                break;
        }
        
        // This is our click handler for the ConnectionPoint
        if (GUI.Button(rect, "", style))
        {
            if (OnClickConnectionPoint != null)
            {
                OnClickConnectionPoint(this);
            }
        }
    }
}
