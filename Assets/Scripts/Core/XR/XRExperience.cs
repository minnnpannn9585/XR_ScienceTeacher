using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class XRExperience
{
    public static void ConfigureEventSystem()
    {
        var system = Object.FindObjectOfType<EventSystem>();
        if (system == null)
        {
            var go = new GameObject("EventSystem");
            system = go.AddComponent<EventSystem>();
            system.sendNavigationEvents = false;
        }

        bool xr = XRSession.IsActive || InputAdapter.DetectXR();
        var xrModule = system.GetComponent<XRUIInputModule>();
        var mouseModule = system.GetComponent<InputSystemUIInputModule>();
        if (xr)
        {
            if (xrModule == null)
            {
                xrModule = system.gameObject.AddComponent<XRUIInputModule>();
            }

            xrModule.enabled = true;
            if (mouseModule != null)
            {
                mouseModule.enabled = false;
            }
        }
        else
        {
            if (mouseModule == null)
            {
                mouseModule = system.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            mouseModule.enabled = true;
            if (xrModule != null)
            {
                xrModule.enabled = false;
            }
        }
    }

    public static XROrigin Attach(InputAdapter input, Transform parent, Vector3 originPos)
    {
        ConfigureEventSystem();
        var origin = XRRigBuilder.Build(parent, originPos);
        if (input == null)
        {
            return origin;
        }

        input.XrOrigin = origin.gameObject;
        input.XrHead = origin.Camera != null ? origin.Camera.transform : null;
        input.RightRay = null;
        input.LeftRay = null;
        var rays = origin.GetComponentsInChildren<XRRayInteractor>(true);
        for (int i = 0; i < rays.Length; i++)
        {
            if (rays[i].name.Contains("Right"))
            {
                input.RightRay = rays[i];
            }
            else if (rays[i].name.Contains("Left"))
            {
                input.LeftRay = rays[i];
            }
        }

        input.ApplyPlatform();
        return origin;
    }
}
