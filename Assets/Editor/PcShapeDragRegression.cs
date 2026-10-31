using System;
using UnityEngine;

/// <summary>Checks desktop solid rotation without modifying the active experiment.</summary>
public static class PcShapeDragRegression
{
    public static void Run(InputAdapter adapter)
    {
        if (adapter == null || adapter.UseXR || adapter.PcCamera == null)
            throw new InvalidOperationException("PC shape regression requires the desktop camera.");

        var root = new GameObject("PC Shape Drag Regression");
        try
        {
            var shape = root.AddComponent<ShapeController>();
            shape.AllowIdleSpin = false;
            var camera = adapter.PcCamera;
            var start = new Vector2(camera.pixelWidth * 0.6f, camera.pixelHeight * 0.6f);
            Ray initialRay = camera.ScreenPointToRay(start);
            shape.OnDragStart(Vector3.zero, initialRay);
            for (int i = 0; i < 60; i++) shape.OnDrag(Vector3.zero, initialRay);
            Require(Quaternion.Angle(root.transform.rotation, Quaternion.identity) < 0.001f,
                "stationary left hold leaves the solid still");

            Ray movedRay = camera.ScreenPointToRay(start + Vector2.right * 80f);
            shape.OnDrag(Vector3.zero, movedRay);
            Quaternion singleMove = root.transform.rotation;
            Require(Mathf.Abs(Quaternion.Angle(singleMove, Quaternion.identity) - 14.4f) < 0.05f,
                "mouse movement rotates the solid at the expected sensitivity");
            for (int i = 0; i < 60; i++) shape.OnDrag(Vector3.zero, movedRay);
            Require(Quaternion.Angle(root.transform.rotation, singleMove) < 0.001f,
                "stopping the mouse stops rotation");
            shape.OnDragEnd();

            root.transform.rotation = Quaternion.identity;
            shape.OnDragStart(Vector3.zero, initialRay);
            for (int i = 1; i <= 10; i++)
                shape.OnDrag(Vector3.zero, camera.ScreenPointToRay(start + Vector2.right * (8f * i)));
            Require(Quaternion.Angle(root.transform.rotation, singleMove) < 0.05f,
                "rotation is unchanged when the same movement spans more frames");
            shape.OnDragEnd();
            Debug.Log("PC_SHAPE_DRAG_REGRESSION_PASSED: stationary hold, mouse movement, and frame-independent rotation.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("PC shape drag regression failed: " + message);
    }
}
