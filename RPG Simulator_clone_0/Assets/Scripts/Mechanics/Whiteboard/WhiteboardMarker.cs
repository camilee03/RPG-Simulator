using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;

public class WhiteboardMarker : NetworkBehaviour
{
    [SerializeField] LineRenderer markerLine;
    List<Vector2> points;
    private Color color;
    private float width;

    [Header("Debug Settings")]
    private readonly DebugSettings.LogLevel logLevel = DebugSettings.LogLevel.Whiteboard;
    private bool shouldLog;

    private void Start()
    {
        shouldLog = DebugSettings.Instance.ShouldLog(logLevel);
    }

    public void UpdateLine(Vector2 pos)
    {
        if (points == null)
        {
            points = new();
            SetPoint(pos);
            return;
        }

        // if this is a new point, add it
        if (Vector2.Distance(points.Last(), pos) > .1f)
        {
            SetPoint(pos);
        }
    }

    [ClientRpc]
    public void UpdateLineClientRpc(Vector2 pos)
    {
        UpdateLine(pos);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateLineServerRpc(Vector2 pos)
    {
        UpdateLineClientRpc(pos);
    }

    void SetPoint(Vector2 point)
    {
        points.Add(point);

        markerLine.positionCount = points.Count;
        markerLine.SetPosition(points.Count - 1, new Vector3(point.x, point.y, 10.8f));

        markerLine.materials[0].SetColor("_BaseColor", color);
        
        markerLine.startWidth = width;
        markerLine.endWidth = width;

        if (shouldLog) Debug.Log($"Added point {point} to line. Total points: {points.Count}");
    }

    public void ChangeColor(Color color)
    {
        this.color = color;
    }

    [ClientRpc]
    public void ChangeColorClientRpc(Color color)
    {
        ChangeColor(color);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ChangeColorServerRpc(Color color)
    {
        ChangeColorClientRpc(color);
    }

    public void ChangeWidth(float width)
    {
        this.width = width;
    }

    [ClientRpc]
    public void ChangeWidthClientRpc(float width)
    {
        ChangeWidth(width);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ChangeWidthServerRpc(float width)
    {
        ChangeWidthClientRpc(width);
    }
}
