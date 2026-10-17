using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>Creates the project's configured copy of Unity's official hands rig; sample sources stay intact.</summary>
public static class OfficialXRSetup
{
    public const string ActionsPath = "Assets/XR/Input/Science XRI Input Actions.inputactions";
    public const string RigPath = "Assets/Resources/XR/ScienceXR.prefab";
    public const string SampleRigPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";

    [MenuItem("XR/Prepare Official Interaction Rig")]
    public static void Prepare()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before preparing the XR rig.");
        RuntimeMaterialSetup.EnsureAssets();
        Directory.CreateDirectory("Assets/Resources/XR");
        AssetDatabase.Refresh();
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (actions == null) throw new InvalidOperationException("Project XRI input actions are missing.");
        var references = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<InputActionReference>()
            .Where(reference => reference.action != null).GroupBy(reference => reference.action.id)
            .ToDictionary(group => group.Key, group => group.First());
        var root = PrefabUtility.LoadPrefabContents(SampleRigPath);
        try
        {
            root.SetActive(false);
            // Make a standalone configured prefab, retaining the official nested
            // interactors, hand meshes, gesture events and stabilization settings.
            if (PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Science XR";
            RemapActions(root, actions, references);
            var origin = root.GetComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 1.6f;
            origin.CameraFloorOffsetObject.transform.localPosition = Vector3.zero;
            origin.Camera.tag = "MainCamera";
            origin.Camera.nearClipPlane = 0.05f;

            var locomotion = root.transform.Find("Locomotion System");
            if (locomotion != null) locomotion.gameObject.SetActive(false);
            foreach (var driver in root.GetComponentsInChildren<CharacterControllerDriver>(true)) driver.enabled = false;
            foreach (var body in root.GetComponentsInChildren<CharacterController>(true)) body.enabled = false;
            foreach (var provider in root.GetComponentsInChildren<LocomotionProvider>(true)) provider.enabled = false;
            foreach (var manager in root.GetComponentsInChildren<ActionBasedControllerManager>(true)) manager.enabled = false;
            foreach (var gaze in root.GetComponentsInChildren<XRGazeInteractor>(true)) gaze.gameObject.SetActive(false);

            foreach (var controller in root.GetComponentsInChildren<ActionBasedController>(true))
            {
                controller.modelPrefab = null;
                controller.rotateAnchorAction = default;
                controller.directionalAnchorRotationAction = default;
                controller.translateAnchorAction = default;
                controller.scaleToggleAction = default;
                controller.scaleDeltaAction = default;
                controller.uiScrollAction = default;
            }
            foreach (var interactor in root.GetComponentsInChildren<XRBaseInteractor>(true))
                interactor.interactionLayers = 0; // Existing experiments use their domain bridge.
            foreach (var ray in root.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (ray.name.Contains("Teleport"))
                {
                    ray.gameObject.SetActive(false);
                    continue;
                }
                ray.raycastMask = ~0;
                ray.enableUIInteraction = true;
                ray.allowAnchorControl = false;
            }

            var inputManager = root.GetComponent<InputActionManager>() ?? root.AddComponent<InputActionManager>();
            inputManager.actionAssets = new List<InputActionAsset> { actions };
            var modality = root.GetComponent<XRInputModalityManager>();
            ConfigureController(root, modality, false, references);
            ConfigureController(root, modality, true, references);
            ConfigureHand(modality.leftHand, false, references);
            ConfigureHand(modality.rightHand, true, references);
            PrefabUtility.SaveAsPrefabAsset(root, RigPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[XR] Official XRI hands rig prepared: native aim poses, official rays/pokes/stabilizers, Oculus controller hand models, no locomotion.");
    }

    static void RemapActions(GameObject root, InputActionAsset actions, Dictionary<Guid, InputActionReference> references)
    {
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) throw new InvalidOperationException("Missing sample script. Import Starter Assets, Hands Interaction Demo and XR Hands HandVisualizer.");
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue is InputActionReference reference && reference.action != null &&
                    references.TryGetValue(reference.action.id, out var replacement))
                    property.objectReferenceValue = replacement;
                else if (property.objectReferenceValue is InputActionAsset)
                    property.objectReferenceValue = actions;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static InputActionProperty Action(Dictionary<Guid, InputActionReference> references, string path)
    {
        var reference = references.Values.First(entry => entry.action.actionMap.name + "/" + entry.action.name == path);
        return new InputActionProperty(reference);
    }

    static void ConfigureController(GameObject root, XRInputModalityManager modality, bool right,
        Dictionary<Guid, InputActionReference> references)
    {
        string side = right ? "Right" : "Left";
        string map = "XRI " + side + "Hand";
        var controllerObject = right ? modality.rightController : modality.leftController;
        var controller = controllerObject.GetComponent<ActionBasedController>();
        var group = new GameObject(side + " Controller Mode");
        group.transform.SetParent(controllerObject.transform.parent, false);
        controllerObject.transform.SetParent(group.transform, false);
        if (right) modality.rightController = group;
        else modality.leftController = group;

        // Grip and aim are distinct OpenXR poses. The model follows grip through
        // the official pose driver; it never changes the ray's aim pose or angle.
        var grip = new GameObject(side + " Controller Grip");
        grip.transform.SetParent(group.transform, false);
        var driver = grip.AddComponent<TrackedPoseDriver>();
        driver.positionInput = Action(references, map + "/Controller Grip Position");
        driver.rotationInput = Action(references, map + "/Controller Grip Rotation");
        driver.trackingStateInput = Action(references, map + "/Tracking State");
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Oculus Hands/Prefabs/" + side + " Hand Model.prefab");
        if (modelAsset == null) throw new InvalidOperationException("Oculus " + side + " hand model is missing.");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, grip.transform);
        var animation = model.AddComponent<XRControllerHandAnimation>();
        animation.Animator = model.GetComponentInChildren<Animator>(true);
        animation.Grip = Action(references, map + " Interaction/Select Value");
        animation.Trigger = Action(references, map + " Interaction/Activate Value");
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = ControllerMaterial();
        var fingertip = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("index_ignore"));
        var ray = controllerObject.GetComponentsInChildren<XRRayInteractor>(true).First(r => !r.name.Contains("Teleport"));
        group.AddComponent<XRHandCursor>().Configure(right, true, controller, ray, null, fingertip != null ? fingertip : grip.transform,
            null, Action(references, map + " Interaction/Select"), Action(references, map + " Interaction/Activate"));
    }

    static void ConfigureHand(GameObject hand, bool right, Dictionary<Guid, InputActionReference> references)
    {
        string map = "XRI " + (right ? "Right" : "Left") + "Hand";
        var ray = hand.GetComponentsInChildren<XRRayInteractor>(true).First(r => !r.name.Contains("Teleport"));
        var poke = hand.GetComponentInChildren<XRPokeInteractor>(true);
        var tracking = hand.GetComponentInChildren<XRHandTrackingEvents>(true);
        if (tracking == null) throw new InvalidOperationException("Official hand mesh/tracking events are missing.");
        hand.AddComponent<XRHandCursor>().Configure(right, false, ray.GetComponent<ActionBasedController>(), ray, poke, poke.transform,
            tracking, Action(references, map + " Interaction/Select"), Action(references, map + " Interaction/Activate"));
    }

    static Material ControllerMaterial()
    {
        const string path = "Assets/Resources/XR/OculusControllerHand.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Resources.Load<Material>(RuntimeMaterials.ResourceFolder + "LitOpaque"));
        material.name = "Oculus Controller Hand";
        material.SetColor("_BaseColor", new Color(0.82f, 0.86f, 0.9f, 1f));
        material.SetFloat("_Smoothness", 0.25f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
