using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;

public static class XRRigBuilder
{
    public static XROrigin Build(Transform parent, Vector3 originPos)
    {
        var originGo = new GameObject("XR Origin");
        // Stay inactive until InputAdapter picks the XR camera. Building it live
        // would add a second AudioListener next to the desktop camera.
        originGo.SetActive(false);
        originGo.transform.SetParent(parent, false);
        originGo.transform.position = originPos;
        var origin = originGo.AddComponent<XROrigin>();
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        origin.CameraYOffset = 1.6f;

        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(originGo.transform, false);

        var camGo = new GameObject("XR Camera");
        camGo.transform.SetParent(offset.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        camGo.tag = "MainCamera";
        camGo.AddComponent<UniversalAdditionalCameraData>();
        StudioSet.ApplyCamera(cam);
        camGo.AddComponent<AudioListener>();
        var tpd = camGo.AddComponent<TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(new InputAction("HeadPos", expectedControlType: "Vector3", binding: "<XRHMD>/centerEyePosition"));
        tpd.rotationInput = new InputActionProperty(new InputAction("HeadRot", expectedControlType: "Quaternion", binding: "<XRHMD>/centerEyeRotation"));
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offset;
        origin.Origin = originGo;

        if (Object.FindObjectOfType<XRInteractionManager>() == null)
        {
            var manager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            manager.transform.SetParent(parent, false);
        }

        CreateHand(offset.transform, "Right Controller", true);
        CreateHand(offset.transform, "Left Controller", false);
        return origin;
    }

    static void CreateHand(Transform parent, string name, bool right)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var controller = go.AddComponent<ActionBasedController>();
        controller.enableInputTracking = false;
        controller.enableInputActions = true;

        var aim = new GameObject("Aim");
        aim.transform.SetParent(go.transform, false);
        var pokeGo = new GameObject("Poke");
        pokeGo.transform.SetParent(go.transform, false);

        var ray = go.AddComponent<XRRayInteractor>();
        ray.rayOriginTransform = aim.transform;
        ray.maxRaycastDistance = 12f;
        // XRI also uses this mask for Canvas graphics, so zero disables all UI hits.
        ray.raycastMask = ~0;
        ray.interactionLayers = 0; // World objects are handled by InputAdapter.
        ray.enableUIInteraction = false;
        ray.lineType = XRRayInteractor.LineType.StraightLine;

        var line = go.AddComponent<XRInteractorLineVisual>();
        line.lineWidth = 0.004f;
        line.validColorGradient = Solid(NetFoldTheme.Accent);
        line.invalidColorGradient = Solid(new Color(1f, 1f, 1f, 0.2f));
        line.enabled = false;

        var poke = pokeGo.AddComponent<XRPokeInteractor>();
        poke.pokeDepth = 0.05f;
        poke.pokeWidth = 0.012f;
        poke.pokeSelectWidth = 0.015f;
        poke.pokeHoverRadius = 0.02f;
        poke.requirePokeFilter = true;
        poke.enableUIInteraction = true;

        var cursor = go.AddComponent<XRHandCursor>();
        cursor.Configure(right, parent, controller, ray, line, poke, aim.transform, pokeGo.transform);
    }

    static Gradient Solid(Color c)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
        return g;
    }
}
