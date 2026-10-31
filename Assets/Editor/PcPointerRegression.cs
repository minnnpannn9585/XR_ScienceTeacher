using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Exercises queued device events through the real PC provider and adapter.</summary>
public static class PcPointerRegression
{
    static readonly MethodInfo ProviderUpdate = typeof(PCInputProvider).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly MethodInfo AdapterUpdate = typeof(InputAdapter).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo Dragging = typeof(InputAdapter).GetField("_dragging", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Run(TriangleAnglesLab lab)
    {
        var adapter = lab.GetComponent<InputAdapter>();
        var provider = adapter.Provider as PCInputProvider;
        Require(provider != null && !adapter.UseXR, "PC provider active");
        var settings = InputSystem.settings;
        bool settingsWereDirty = EditorUtility.IsDirty(settings);
        provider.Shutdown();
        bool originalMergingDisabled = settings.disableRedundantEventsMerging;
        provider.Initialize(adapter);
        Require(settings.disableRedundantEventsMerging, "PC runtime preserves mouse press events before callbacks");
        Require(EditorUtility.IsDirty(settings) == settingsWereDirty, "runtime input setting does not dirty its asset");
        var originalMouse = Mouse.current;
        var originalKeyboard = Keyboard.current;
        var mouse = InputSystem.AddDevice<Mouse>("PC Regression Mouse");
        var keyboard = InputSystem.AddDevice<Keyboard>("PC Regression Keyboard");
        bool backObserved = false;
        Action observeBack = () => backObserved = true;
        try
        {
            Canvas.ForceUpdateCanvases();
            EventSystem.current.SetSelectedGameObject(null);
            var handle = lab.GetComponentInChildren<TriangleVertexHandle>();
            var slider = lab.GetComponentInChildren<Slider>().GetComponent<RectTransform>();
            Vector2 ui = RectTransformUtility.WorldToScreenPoint(null, slider.TransformPoint(slider.rect.center));
            Reset(lab, adapter);
            Vector2 start = adapter.PcCamera.WorldToScreenPoint(handle.transform.position);
            Vector2 left = adapter.PcCamera.WorldToScreenPoint(handle.transform.position + Vector3.left * 0.4f);
            Vector2 right = adapter.PcCamera.WorldToScreenPoint(handle.transform.position + Vector3.right * 0.4f);

            // All events in each batch are processed before MonoBehaviour.Update,
            // reproducing fast mouse movement that used to miss the small vertex.
            Process(mouse, provider, adapter, State(start, true), State(left, true));
            Require(Mathf.Abs(lab.VertexX + 0.4f) < 0.005f, "rapid press-and-move selects the original vertex; x=" + lab.VertexX + " press=" + provider.SelectPressed + " pressRay=" + provider.PressRay.direction + " currentRay=" + provider.PointerRay.direction + " pressUI=" + provider.PressStartedOverUi);
            Require(ReferenceEquals(adapter.Current, handle) && (bool)Dragging.GetValue(adapter), "drag begins normally");
            Process(mouse, provider, adapter, State(ui, true));
            Require(provider.IsPointerOverUi && lab.VertexX > 0.3f, "started drag continues over UI");
            Process(mouse, provider, adapter, State(ui, false));
            Require(!(bool)Dragging.GetValue(adapter), "release over UI ends drag");
            float releasedX = lab.VertexX;
            Process(mouse, provider, adapter, State(left, false));
            Require(Mathf.Abs(lab.VertexX - releasedX) < 0.001f, "released vertex does not follow the pointer");

            Reset(lab, adapter);
            Process(mouse, provider, adapter, State(start, true), State(right, true), State(right, false));
            Require(provider.SelectPressed && provider.SelectReleased && !provider.SelectHeld, "coalesced press and release are represented");
            Require(Mathf.Abs(lab.VertexX - 0.4f) < 0.005f && !(bool)Dragging.GetValue(adapter), "one-frame drag applies its final position and ends");

            Reset(lab, adapter);
            Process(mouse, provider, adapter, State(ui, true), State(start, true), State(start, false));
            Require(provider.PressStartedOverUi && !provider.IsPointerOverUi, "UI gate uses press origin");
            Require(adapter.Current == null && Mathf.Abs(lab.VertexX) < 0.001f, "pressing UI cannot start a world drag after moving off it");

            // Keep this regression in its current scene while verifying the complete
            // keyboard -> provider -> adapter navigation event path.
            adapter.BackPressed -= SceneLoader.LoadMainMenu;
            adapter.BackPressed += observeBack;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            ProviderUpdate.Invoke(provider, null);
            AdapterUpdate.Invoke(adapter, null);
            Require(provider.BackPressed && backObserved, "Escape reaches the normal back-navigation event");
            PcShapeDragRegression.Run(adapter);
            Debug.Log("PC_POINTER_REGRESSION_PASSED: rapid drag, same-frame release, UI-origin gating, release over UI, and Escape navigation event.");
        }
        finally
        {
            adapter.BackPressed -= observeBack;
            adapter.BackPressed -= SceneLoader.LoadMainMenu;
            adapter.BackPressed += SceneLoader.LoadMainMenu;
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
            if (originalMouse != null) originalMouse.MakeCurrent();
            if (originalKeyboard != null) originalKeyboard.MakeCurrent();
            Reset(lab, adapter);
            provider.Shutdown();
            bool restored = settings.disableRedundantEventsMerging == originalMergingDisabled;
            bool dirtyStateUnchanged = EditorUtility.IsDirty(settings) == settingsWereDirty;
            provider.Initialize(adapter);
            Require(restored && dirtyStateUnchanged, "PC shutdown restores the input setting without dirtying its asset");
        }
    }

    static MouseState State(Vector2 position, bool held) => new MouseState { position = position }.WithButton(MouseButton.Left, held);

    static void Process(Mouse mouse, PCInputProvider provider, InputAdapter adapter, params MouseState[] states)
    {
        foreach (var state in states) InputSystem.QueueStateEvent(mouse, state);
        InputSystem.Update();
        ProviderUpdate.Invoke(provider, null);
        AdapterUpdate.Invoke(adapter, null);
    }

    static void Reset(TriangleAnglesLab lab, InputAdapter adapter)
    {
        adapter.ClearSelection();
        lab.ResetExperiment();
        Physics.SyncTransforms();
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("PC pointer regression failed: " + message);
    }
}
