using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class XRRigBuilder
{
    public static XROrigin Build(Transform parent, Vector3 originPos)
    {
        var originGo = new GameObject("XR Origin");
        originGo.transform.SetParent(parent, false);
        originGo.transform.position = originPos;
        var origin = originGo.AddComponent<XROrigin>();

        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(originGo.transform, false);
        offset.transform.localPosition = new Vector3(0f, 1.25f, 0f);

        var camGo = new GameObject("XR Camera");
        camGo.transform.SetParent(offset.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        camGo.tag = "MainCamera";
        camGo.AddComponent<UniversalAdditionalCameraData>();
        camGo.AddComponent<AudioListener>();
        var tpd = camGo.AddComponent<TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(new InputAction("HeadPos", expectedControlType: "Vector3", binding: "<XRHMD>/centerEyePosition"));
        tpd.rotationInput = new InputActionProperty(new InputAction("HeadRot", expectedControlType: "Quaternion", binding: "<XRHMD>/centerEyeRotation"));

        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offset;
        origin.Origin = originGo;

        var manager = Object.FindObjectOfType<XRInteractionManager>();
        if (manager == null)
        {
            manager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            manager.transform.SetParent(parent, false);
        }

        var right = CreateController(offset.transform, "Right Controller", true);
        var left = CreateController(offset.transform, "Left Controller", false);
        originGo.SetActive(false);
        return origin;
    }

    static GameObject CreateController(Transform parent, string name, bool right)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        string hand = right ? "RightHand" : "LeftHand";
        var controller = go.AddComponent<ActionBasedController>();
        controller.positionAction = new InputActionProperty(new InputAction(binding: $"<XRController>{{{hand}}}/devicePosition"));
        controller.rotationAction = new InputActionProperty(new InputAction(binding: $"<XRController>{{{hand}}}/deviceRotation"));
        controller.selectAction = new InputActionProperty(new InputAction(binding: $"<XRController>{{{hand}}}/trigger"));
        controller.activateAction = new InputActionProperty(new InputAction(binding: $"<XRController>{{{hand}}}/grip"));
        controller.enableInputTracking = true;
        controller.enableInputActions = true;

        var ray = go.AddComponent<XRRayInteractor>();
        ray.maxRaycastDistance = 8f;
        ray.raycastMask = ~0;
        var line = go.AddComponent<XRInteractorLineVisual>();
        line.lineWidth = 0.006f;
        line.validColorGradient = Solid(NetFoldTheme.Accent);
        line.invalidColorGradient = Solid(new Color(1f, 1f, 1f, 0.25f));
        return go;
    }

    static Gradient Solid(Color c)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
        return g;
    }
}
