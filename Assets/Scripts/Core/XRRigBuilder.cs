using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;

public static class XRRigBuilder
{
    public const string ResourcePath = "XR/ScienceXR";

    public static XROrigin Build(Transform parent, Vector3 originPos)
    {
        var prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab == null)
            throw new InvalidOperationException("Official XR rig missing. Run XR/Prepare Official Interaction Rig before building.");

        // Saved inactive so only InputAdapter activates the selected camera and
        // the official modality manager after all scene references are assigned.
        var rig = UnityEngine.Object.Instantiate(prefab, parent, false);
        rig.name = "XR Origin";
        rig.transform.position = originPos;
        var origin = rig.GetComponent<XROrigin>();
        var camera = origin.Camera;
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.05f;
        if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        StudioSet.ApplyCamera(camera);

        if (UnityEngine.Object.FindObjectOfType<XRInteractionManager>() == null)
        {
            var manager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            manager.transform.SetParent(parent, false);
        }
        return origin;
    }
}
