using System;
using UnityEditor;
using UnityEngine;

// Modified Source Code Based on https://gram.gs/gramlog/creating-node-based-editor-unity/

public class Connection
{
    public ConnectionPoint inPoint;
    public ConnectionPoint outPoint;
    public Action<Connection> OnClickRemoveConnection;
    public Vector2 MidPoint { get { return (inPoint.node.rect.center + outPoint.node.rect.center) * 0.5f; } }

    public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint, Action<Connection> OnClickRemoveConnection)
    {
        this.inPoint = inPoint;
        this.outPoint = outPoint;
        this.OnClickRemoveConnection = OnClickRemoveConnection;
    }

    public void Draw()
    {
        Handles.DrawBezier(
            inPoint.node.rect.center,
            outPoint.node.rect.center,
            inPoint.node.rect.center + Vector2.left * 10f,
            outPoint.node.rect.center - Vector2.left * 10f,
            Color.white,
            null,
            3f
        );
 
        if (Handles.Button(MidPoint, Quaternion.identity, 4, 8, Handles.RectangleHandleCap))
        {
            if (OnClickRemoveConnection != null)
            {
                OnClickRemoveConnection(this);
            }
        }
    }
}
