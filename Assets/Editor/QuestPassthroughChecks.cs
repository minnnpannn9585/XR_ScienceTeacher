using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

/// <summary>Host-side native ABI/composition/lifecycle checks. Quest hardware is still required for visual validation.</summary>
public static class QuestPassthroughChecks
{
    const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Type Feature = typeof(QuestPassthroughFeature);
    static readonly List<string> Calls = new List<string>();
    static IntPtr _inputFrame, _inputProjection, _overlay, _views;
    static bool _expectUnderlay;
    static int _frameResult;

    [MenuItem("XR/Run Passthrough Native Checks")]
    public static void Run()
    {
        Require(!EditorApplication.isPlaying, "Run outside Play Mode.");
        Require(IntPtr.Size == 8, "These checks target Quest ARM64 / Editor x64.");
        CheckLayout("PassthroughCreateInfo", 24, "flags", 16);
        CheckLayout("LayerCreateInfo", 40, "purpose", 32);
        CheckLayout("PassthroughLayer", 40, "layerHandle", 32);
        CheckLayout("SystemPassthroughProperties", 24, "supportsPassthrough", 16);
        CheckLayout("SystemProperties", 304, "positionTracking", 300);
        CheckLayout("FrameEndInfo", 40, "layers", 32);
        CheckLayout("ProjectionLayer", 48, "views", 40);

        var feature = ScriptableObject.CreateInstance<QuestPassthroughFeature>();
        var allocations = new List<IntPtr>();
        object oldActive = Get(null, "_active"), oldEndFrame = Get(null, "_endFrame");
        try
        {
            Set(feature, "_extensionReady", true);
            Set(feature, "_systemSupported", true);
            Bind(feature, "_pauseLayer", nameof(PauseLayer));
            Bind(feature, "_pausePassthrough", nameof(PausePassthrough));
            Bind(feature, "_destroyLayer", nameof(DestroyLayer));
            Bind(feature, "_destroyPassthrough", nameof(DestroyPassthrough));
            Bind(feature, "_startPassthrough", nameof(Succeed));
            Bind(feature, "_resumeLayer", nameof(Succeed));
            Bind(null, "_endFrame", nameof(SubmitFrame));
            Set(null, "_active", feature);
            Invoke(feature, "OnSessionCreate", 11UL);
            Require(!feature.IsSupported, "Session creation alone must not allow MR.");
            Invoke(feature, "OnSessionBegin", 11UL);
            Invoke(feature, "OnSessionStateChange", 2, 5);
            Require(feature.IsSupported, "Visible, begun session must be ready.");
            Set(feature, "_passthrough", 21UL);
            Set(feature, "_layer", 31UL);
            Require(feature.TrySetPassthrough(true) && feature.IsRunning, "Native start/resume must activate MR.");

            _inputFrame = Allocate(40, allocations);
            _inputProjection = Allocate(48, allocations);
            _overlay = Allocate(40, allocations);
            _views = Allocate(16, allocations);
            IntPtr layerPointers = Allocate(16, allocations);
            Marshal.WriteInt32(_inputFrame, 0, 12); // XR_TYPE_FRAME_END_INFO
            Marshal.WriteInt64(_inputFrame, 8, 1234); // next chain must survive
            Marshal.WriteInt64(_inputFrame, 16, 5678);
            Marshal.WriteInt32(_inputFrame, 24, 1); // OPAQUE
            Marshal.WriteInt32(_inputFrame, 28, 2);
            Marshal.WriteIntPtr(_inputFrame, 32, layerPointers);
            Marshal.WriteInt32(_inputProjection, 0, 35);
            Marshal.WriteInt64(_inputProjection, 8, 4321);
            Marshal.WriteInt64(_inputProjection, 16, 4); // preserve UNPREMULTIPLIED_ALPHA
            Marshal.WriteInt64(_inputProjection, 24, 99);
            Marshal.WriteInt32(_inputProjection, 32, 2);
            Marshal.WriteIntPtr(_inputProjection, 40, _views);
            Marshal.WriteInt32(_overlay, 0, 36); // quad should be forwarded verbatim
            Marshal.WriteIntPtr(layerPointers, 0, _inputProjection);
            Marshal.WriteIntPtr(layerPointers, 8, _overlay);
            _frameResult = 0;
            _expectUnderlay = true;
            Require((int)Invoke(null, "InterceptEndFrame", 11UL, _inputFrame) == 0, "Forward native result.");
            Require(Marshal.ReadInt64(_inputProjection, 16) == 4, "Original projection flags must never be modified.");
            Require(Marshal.ReadInt32(_inputFrame, 28) == 2 && Marshal.ReadIntPtr(_inputFrame, 32) == layerPointers,
                "Original frame and layer pointers must never be modified.");

            // Empty frames preserve shouldRender=false behavior.
            Marshal.WriteInt32(_inputFrame, 28, 0);
            _expectUnderlay = false;
            Invoke(null, "InterceptEndFrame", 11UL, _inputFrame);
            Marshal.WriteInt32(_inputFrame, 28, 2);

            Calls.Clear();
            Require(feature.TrySetPassthrough(false) && !feature.IsRunning, "VR must stop passthrough.");
            Require(string.Join(",", Calls) == "pause layer,pause passthrough", "Pause layer before passthrough.");
            Invoke(null, "InterceptEndFrame", 11UL, _inputFrame);
            Require(feature.TrySetPassthrough(true), "Paused resources must resume.");
            Invoke(feature, "OnSessionStateChange", 5, 3);
            Require(!feature.IsSupported && !feature.IsRunning, "Hidden session must suspend passthrough.");
            Invoke(feature, "OnSessionStateChange", 3, 4);
            Require(feature.IsSupported && !feature.IsRunning, "Visible session must wait for the owner's mode request.");
            Require(feature.TrySetPassthrough(true), "Visible session must resume on request.");

            // Native compositor errors must surface as loss of availability, without a second submission.
            _expectUnderlay = true;
            _frameResult = -2;
            Require((int)Invoke(null, "InterceptEndFrame", 11UL, _inputFrame) == -2, "Forward compositor failure.");
            Require(!feature.IsRunning && !feature.IsSupported && feature.LastError != null, "Compositor failure must request VR fallback.");
            Require(feature.TrySetPassthrough(false), "VR fallback must still pause failed composition.");

            Invoke(feature, "OnSessionDestroy", 11UL);
            Require(Get(feature, "_frameBuffer").Equals(IntPtr.Zero) && Get(feature, "_layerPointers").Equals(IntPtr.Zero),
                "Session destruction must free native frame buffers.");
            Require(Get(feature, "_layer").Equals(0UL) && Get(feature, "_passthrough").Equals(0UL),
                "Session destruction must release handles.");

            // A failed resume must never report MR or leak partially started resources.
            Invoke(feature, "OnSessionCreate", 12UL);
            Invoke(feature, "OnSessionBegin", 12UL);
            Invoke(feature, "OnSessionStateChange", 2, 5);
            Set(feature, "_passthrough", 22UL);
            Set(feature, "_layer", 32UL);
            Bind(feature, "_resumeLayer", nameof(Fail));
            Calls.Clear();
            Require(!feature.TrySetPassthrough(true) && !feature.IsRunning, "Failed native resume must not activate MR.");
            Require(string.Join(",", Calls) == "destroy layer,destroy passthrough", "Failed start must destroy layer before parent.");
            Debug.Log("[Quest Passthrough Checks] PASS: ARM64 ABI, underlay order, alpha, original frame preservation, pause/resume, session teardown and native failure fallback.");
        }
        finally
        {
            Invoke(feature, "OnSessionDestroy", 0UL);
            Set(null, "_active", oldActive);
            Set(null, "_endFrame", oldEndFrame);
            foreach (var allocation in allocations) Marshal.FreeHGlobal(allocation);
            UnityEngine.Object.DestroyImmediate(feature);
        }
    }

    static int SubmitFrame(ulong session, IntPtr frame)
    {
        if (!_expectUnderlay)
        {
            Require(frame == _inputFrame, "VR / empty frames must be forwarded unchanged.");
            return _frameResult;
        }
        Require(frame != _inputFrame, "MR must submit its own frame copy.");
        Require(Marshal.ReadInt32(frame, 28) == 3, "One underlay must be prepended.");
        Require(Marshal.ReadInt64(frame, 8) == 1234 && Marshal.ReadInt64(frame, 16) == 5678 && Marshal.ReadInt32(frame, 24) == 1,
            "Preserve frame next, display time and OPAQUE environment blend.");
        IntPtr layers = Marshal.ReadIntPtr(frame, 32), underlay = Marshal.ReadIntPtr(layers);
        Require(Marshal.ReadInt32(underlay) == 1000118003 && Marshal.ReadInt64(underlay, 32) == 31,
            "Passthrough must be first with the native layer handle.");
        Require(Marshal.ReadInt64(underlay, 24) == 0, "Passthrough space must be XR_NULL_HANDLE.");
        Require(Marshal.ReadInt64(underlay, 16) == 2, "Passthrough must enable source-alpha blending.");
        IntPtr projection = Marshal.ReadIntPtr(layers, 8);
        Require(projection != _inputProjection && Marshal.ReadInt64(projection, 16) == 6, "Enable source-alpha on a copy.");
        Require(Marshal.ReadInt64(projection, 8) == 4321 && Marshal.ReadInt64(projection, 24) == 99 &&
            Marshal.ReadInt32(projection, 32) == 2 && Marshal.ReadIntPtr(projection, 40) == _views,
            "Projection next, space, views and view count must survive.");
        Require(Marshal.ReadIntPtr(layers, 16) == _overlay, "Keep other layer pointers and order.");
        return _frameResult;
    }

    static int Succeed(ulong handle) => 0;
    static int Fail(ulong handle) => -2;
    static int PauseLayer(ulong handle) { Calls.Add("pause layer"); return 0; }
    static int PausePassthrough(ulong handle) { Calls.Add("pause passthrough"); return 0; }
    static int DestroyLayer(ulong handle) { Calls.Add("destroy layer"); return 0; }
    static int DestroyPassthrough(ulong handle) { Calls.Add("destroy passthrough"); return 0; }
    static object Get(object target, string name) => Feature.GetField(name, Fields).GetValue(target);
    static void Set(object target, string name, object value) => Feature.GetField(name, Fields).SetValue(target, value);
    static object Invoke(object target, string name, params object[] args) => Feature.GetMethod(name, Fields).Invoke(target, args);
    static void Bind(object target, string field, string method)
    {
        var info = Feature.GetField(field, Fields);
        info.SetValue(target, Delegate.CreateDelegate(info.FieldType, typeof(QuestPassthroughChecks).GetMethod(method, Fields)));
    }
    static IntPtr Allocate(int size, List<IntPtr> allocations)
    {
        var pointer = Marshal.AllocHGlobal(size);
        Marshal.Copy(new byte[size], 0, pointer, size);
        allocations.Add(pointer);
        return pointer;
    }
    static void CheckLayout(string name, int size, string member, int offset)
    {
        Type type = Feature.GetNestedType(name, BindingFlags.NonPublic);
        Require(Marshal.SizeOf(type) == size && Marshal.OffsetOf(type, member).ToInt32() == offset, name + " must match OpenXR ARM64 ABI.");
    }
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[Quest Passthrough Checks] " + message);
    }
}
