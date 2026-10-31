using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Quest reconstruction passthrough underlay for the existing Unity OpenXR loader.
/// Uses XR_FB_passthrough, without an additional XR loader or camera permission.
/// </summary>
#if UNITY_EDITOR
[UnityEditor.XR.OpenXR.Features.OpenXRFeature(
    UiName = "Quest VR / MR Passthrough",
    BuildTargetGroups = new[] { BuildTargetGroup.Android },
    Company = "XR Science Teacher", Version = "1.0.0",
    Desc = "Optional Quest passthrough underlay for the VR / MR button.",
    FeatureId = FeatureId, OpenxrExtensionStrings = "XR_FB_passthrough")]
#endif
public sealed class QuestPassthroughFeature : OpenXRFeature
{
    public const string FeatureId = "com.xrscienceteacher.openxr.questpassthrough";
    const int MaxLayers = 64;
    const int ProjectionType = 35;
    const ulong SourceAlpha = 2;
    static readonly object Gate = new object();
    // These references must survive for as long as native code can call the hooks.
    static readonly GetProcAddr ProcHook = InterceptGetProcAddr;
    static readonly EndFrame FrameHook = InterceptEndFrame;
    static GetProcAddr _getProcAddr;
    static EndFrame _endFrame;
    static QuestPassthroughFeature _active;

    ulong _instance, _session, _passthrough, _layer;
    bool _extensionReady, _systemSupported, _sessionBegun, _visible, _running, _compositionFault;
    string _lastError;
    CreatePassthrough _createPassthrough;
    CreateLayer _createLayer;
    HandleCall _destroyPassthrough, _startPassthrough, _pausePassthrough;
    HandleCall _destroyLayer, _resumeLayer, _pauseLayer;
    GetSystemProperties _getSystemProperties;
    IntPtr _frameBuffer, _layerPointers, _projectionCopies, _passthroughBuffer;
    static readonly int ProjectionSize = Marshal.SizeOf<ProjectionLayer>();

    /// <summary>True only when the extension, device, and visible session are ready.</summary>
    public bool IsSupported
    {
        get { lock (Gate) return Ready; }
    }

    public bool IsRunning
    {
        get { lock (Gate) return Ready && _running; }
    }

    public string LastError
    {
        get { lock (Gate) return _lastError; }
    }

    bool Ready => _extensionReady && _systemSupported && _session != 0 &&
                  _sessionBegun && _visible && !_compositionFault && _endFrame != null;

    /// <summary>Call on the main thread; make the camera transparent only after success.</summary>
    public bool TrySetPassthrough(bool enabled)
    {
        lock (Gate)
        {
            if (!enabled)
            {
                Pause();
                return true;
            }
            if (!Ready) return false;
            if (_running) return true;
            _lastError = null;
            if (_passthrough == 0)
            {
                var create = new PassthroughCreateInfo { type = 1000118001 };
                if (!Check(_createPassthrough(_session, ref create, out _passthrough), "xrCreatePassthroughFB"))
                    return false;
                var layer = new LayerCreateInfo { type = 1000118002, passthrough = _passthrough, purpose = 0 };
                if (!Check(_createLayer(_session, ref layer, out _layer), "xrCreatePassthroughLayerFB"))
                {
                    DestroyPassthrough();
                    return false;
                }
            }
            if (!Check(_startPassthrough(_passthrough), "xrPassthroughStartFB") ||
                !Check(_resumeLayer(_layer), "xrPassthroughLayerResumeFB"))
            {
                DestroyPassthrough();
                return false;
            }
            var underlay = new PassthroughLayer { type = 1000118003, flags = SourceAlpha, layerHandle = _layer };
            Marshal.StructureToPtr(underlay, _passthroughBuffer, false);
            _running = true;
            return true;
        }
    }

    protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
    {
        lock (Gate)
        {
            _getProcAddr = Marshal.GetDelegateForFunctionPointer<GetProcAddr>(func);
            _endFrame = null;
            return Marshal.GetFunctionPointerForDelegate(ProcHook);
        }
    }

    protected override bool OnInstanceCreate(ulong xrInstance)
    {
        lock (Gate)
        {
            _instance = xrInstance;
            _extensionReady = false;
            _systemSupported = false;
            _lastError = null;
            // The ABI below targets Quest ARM64, and the Editor mock checks run on x64.
            if (IntPtr.Size != 8 || !OpenXRRuntime.IsExtensionEnabled("XR_FB_passthrough")) return false;
            _createPassthrough = Load<CreatePassthrough>("xrCreatePassthroughFB");
            _createLayer = Load<CreateLayer>("xrCreatePassthroughLayerFB");
            _destroyPassthrough = Load<HandleCall>("xrDestroyPassthroughFB");
            _startPassthrough = Load<HandleCall>("xrPassthroughStartFB");
            _pausePassthrough = Load<HandleCall>("xrPassthroughPauseFB");
            _destroyLayer = Load<HandleCall>("xrDestroyPassthroughLayerFB");
            _resumeLayer = Load<HandleCall>("xrPassthroughLayerResumeFB");
            _pauseLayer = Load<HandleCall>("xrPassthroughLayerPauseFB");
            _getSystemProperties = Load<GetSystemProperties>("xrGetSystemProperties");
            _extensionReady = _createPassthrough != null && _createLayer != null &&
                _destroyPassthrough != null && _startPassthrough != null && _pausePassthrough != null &&
                _destroyLayer != null && _resumeLayer != null && _pauseLayer != null && _getSystemProperties != null;
            if (_extensionReady) _active = this;
            return _extensionReady;
        }
    }

    protected override void OnSystemChange(ulong xrSystem)
    {
        lock (Gate)
        {
            _systemSupported = false;
            if (!_extensionReady) return;
            var extra = Marshal.AllocHGlobal(Marshal.SizeOf<SystemPassthroughProperties>());
            try
            {
                Marshal.StructureToPtr(new SystemPassthroughProperties { type = 1000118000 }, extra, false);
                var properties = new SystemProperties { type = 5, next = extra, systemName = "" };
                if (Check(_getSystemProperties(_instance, xrSystem, ref properties), "xrGetSystemProperties"))
                    _systemSupported = Marshal.PtrToStructure<SystemPassthroughProperties>(extra).supportsPassthrough != 0;
            }
            finally { Marshal.FreeHGlobal(extra); }
        }
    }

    protected override void OnSessionCreate(ulong xrSession)
    {
        lock (Gate)
        {
            ReleaseSession();
            _session = xrSession;
            _compositionFault = false;
            if (!_extensionReady) return;
            _frameBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<FrameEndInfo>());
            _layerPointers = Marshal.AllocHGlobal((MaxLayers + 1) * IntPtr.Size);
            _projectionCopies = Marshal.AllocHGlobal(MaxLayers * ProjectionSize);
            _passthroughBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<PassthroughLayer>());
        }
    }

    protected override void OnSessionBegin(ulong xrSession)
    {
        lock (Gate) _sessionBegun = true;
    }

    protected override void OnSessionStateChange(int oldState, int newState)
    {
        lock (Gate)
        {
            // VISIBLE and FOCUSED. Keep MR visible beneath the system menu.
            _visible = newState == 4 || newState == 5;
            if (!_visible) Pause();
        }
    }

    protected override void OnSessionEnd(ulong xrSession)
    {
        lock (Gate) { _sessionBegun = false; _visible = false; Pause(); }
    }

    protected override void OnSessionLossPending(ulong xrSession)
    {
        lock (Gate) { _sessionBegun = false; _visible = false; Pause(); }
    }

    protected override void OnSessionExiting(ulong xrSession)
    {
        lock (Gate) { _sessionBegun = false; _visible = false; Pause(); }
    }

    protected override void OnSessionDestroy(ulong xrSession)
    {
        lock (Gate) ReleaseSession();
    }

    protected override void OnInstanceLossPending(ulong xrInstance)
    {
        lock (Gate) { _sessionBegun = false; _visible = false; Pause(); }
    }

    protected override void OnInstanceDestroy(ulong xrInstance)
    {
        lock (Gate)
        {
            ReleaseSession();
            _extensionReady = false;
            _systemSupported = false;
            _instance = 0;
            if (ReferenceEquals(_active, this)) _active = null;
            // Preserve the chained delegates until the loader installs its next hook.
        }
    }

    T Load<T>(string name) where T : Delegate
    {
        var utf8 = Marshal.StringToHGlobalAnsi(name);
        try
        {
            if (_getProcAddr(_instance, utf8, out var address) < 0 || address == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }
        finally { Marshal.FreeHGlobal(utf8); }
    }

    bool Check(int result, string operation)
    {
        if (result >= 0) return true;
        _lastError = operation + " failed (XrResult " + result + ").";
        return false;
    }

    void Pause()
    {
        if (!_running) return;
        _running = false;
        // Serialize with xrEndFrame so no submitted frame can retain a destroyed handle.
        bool layerPaused = Check(_pauseLayer(_layer), "xrPassthroughLayerPauseFB");
        bool passthroughPaused = Check(_pausePassthrough(_passthrough), "xrPassthroughPauseFB");
        if (!layerPaused || !passthroughPaused) DestroyPassthrough();
    }

    void DestroyPassthrough()
    {
        _running = false;
        if (_layer != 0) { _destroyLayer(_layer); _layer = 0; }
        if (_passthrough != 0) { _destroyPassthrough(_passthrough); _passthrough = 0; }
    }

    void ReleaseSession()
    {
        DestroyPassthrough();
        Free(ref _frameBuffer);
        Free(ref _layerPointers);
        Free(ref _projectionCopies);
        Free(ref _passthroughBuffer);
        _session = 0;
        _sessionBegun = false;
        _visible = false;
    }

    static void Free(ref IntPtr pointer)
    {
        if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer);
        pointer = IntPtr.Zero;
    }

    [MonoPInvokeCallback(typeof(GetProcAddr))]
    static int InterceptGetProcAddr(ulong instance, IntPtr name, out IntPtr function)
    {
        lock (Gate)
        {
            int result = _getProcAddr(instance, name, out function);
            if (result >= 0 && function != IntPtr.Zero && Marshal.PtrToStringAnsi(name) == "xrEndFrame")
            {
                _endFrame = Marshal.GetDelegateForFunctionPointer<EndFrame>(function);
                function = Marshal.GetFunctionPointerForDelegate(FrameHook);
            }
            return result;
        }
    }

    [MonoPInvokeCallback(typeof(EndFrame))]
    static int InterceptEndFrame(ulong session, IntPtr frameEndInfo)
    {
        lock (Gate)
        {
            // This callback runs on Unity's render thread: no Unity object APIs or logging.
            var feature = _active;
            if (ReferenceEquals(feature, null) || !feature.Ready || !feature._running || feature._session != session)
                return _endFrame(session, frameEndInfo);
            IntPtr submitted = frameEndInfo;
            try { submitted = feature.ComposeFrame(frameEndInfo); }
            catch (Exception)
            {
                feature._compositionFault = true;
                feature._lastError = "Unable to compose passthrough frame; returning to VR.";
            }
            int result = _endFrame(session, submitted);
            if (result < 0)
            {
                feature._compositionFault = true;
                feature._lastError = "xrEndFrame rejected the passthrough frame; returning to VR.";
            }
            return result;
        }
    }

    IntPtr ComposeFrame(IntPtr source)
    {
        var frame = Marshal.PtrToStructure<FrameEndInfo>(source);
        // Preserve the runtime's empty-frame behavior (e.g. shouldRender == false).
        if (frame.layerCount == 0) return source;
        if (frame.layerCount > MaxLayers || frame.layers == IntPtr.Zero)
            throw new InvalidOperationException("Unexpected OpenXR layer count.");
        Marshal.WriteIntPtr(_layerPointers, _passthroughBuffer);
        for (int i = 0; i < frame.layerCount; i++)
        {
            IntPtr layer = Marshal.ReadIntPtr(frame.layers, i * IntPtr.Size);
            if (layer != IntPtr.Zero && Marshal.ReadInt32(layer) == ProjectionType)
            {
                var projection = Marshal.PtrToStructure<ProjectionLayer>(layer);
                projection.layerFlags |= SourceAlpha;
                // Copy the complete projection header; Unity owns the original and its views/next chain.
                layer = IntPtr.Add(_projectionCopies, i * ProjectionSize);
                Marshal.StructureToPtr(projection, layer, false);
            }
            Marshal.WriteIntPtr(_layerPointers, (i + 1) * IntPtr.Size, layer);
        }
        frame.layerCount++;
        frame.layers = _layerPointers;
        // Quest uses OPAQUE environment blending; the underlay is a compositor layer.
        // Keep Unity's environmentBlendMode, displayTime and next chain unchanged.
        Marshal.StructureToPtr(frame, _frameBuffer, false);
        return _frameBuffer;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GetProcAddr(ulong instance, IntPtr name, out IntPtr function);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int EndFrame(ulong session, IntPtr frame);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CreatePassthrough(ulong session, ref PassthroughCreateInfo info, out ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int CreateLayer(ulong session, ref LayerCreateInfo info, out ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int HandleCall(ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GetSystemProperties(ulong instance, ulong system, ref SystemProperties properties);

    [StructLayout(LayoutKind.Sequential)] struct PassthroughCreateInfo { public int type; public IntPtr next; public ulong flags; }
    [StructLayout(LayoutKind.Sequential)] struct LayerCreateInfo { public int type; public IntPtr next; public ulong passthrough, flags; public int purpose; }
    [StructLayout(LayoutKind.Sequential)] struct PassthroughLayer { public int type; public IntPtr next; public ulong flags, space, layerHandle; }
    [StructLayout(LayoutKind.Sequential)] struct SystemPassthroughProperties { public int type; public IntPtr next; public uint supportsPassthrough; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] struct SystemProperties
    {
        public int type; public IntPtr next; public ulong systemId; public uint vendorId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string systemName;
        public uint maxSwapchainImageHeight, maxSwapchainImageWidth, maxLayerCount, orientationTracking, positionTracking;
    }
    [StructLayout(LayoutKind.Sequential)] struct FrameEndInfo
    {
        public int type; public IntPtr next; public long displayTime;
        public int environmentBlendMode; public uint layerCount; public IntPtr layers;
    }
    [StructLayout(LayoutKind.Sequential)] struct ProjectionLayer
    {
        public int type; public IntPtr next; public ulong layerFlags, space;
        public uint viewCount; public IntPtr views;
    }
}
