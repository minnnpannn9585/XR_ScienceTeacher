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

    public static XROrigin Attach(InputAdapter input, Transform parent, Vector3 tableSurface, float deskDepth)
    {
        ConfigureEventSystem();
        var originPos = new Vector3(tableSurface.x, 0f, tableSurface.z - deskDepth * 0.5f - XRDeskStation.FrontClearance);
        var origin = XRRigBuilder.Build(parent, originPos);
        var station = parent.gameObject.AddComponent<XRDeskStation>();
        station.Configure(origin, tableSurface, deskDepth);
        if (input == null)
        {
            return origin;
        }

        input.XrOrigin = origin.gameObject;
        input.XrHead = origin.Camera != null ? origin.Camera.transform : null;
        input.RightRay = null;
        input.LeftRay = null;
        var cursors = origin.GetComponentsInChildren<XRHandCursor>(true);
        for (int i = 0; i < cursors.Length; i++)
        {
            if (!cursors[i].UsingController) continue;
            if (cursors[i].Right) input.RightRay = cursors[i].RayInteractor;
            else input.LeftRay = cursors[i].RayInteractor;
        }

        input.ApplyPlatform();
        return origin;
    }
}
