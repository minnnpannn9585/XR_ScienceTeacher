using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class StudioSet
{
    static Texture2D _wall;
    static Texture2D _pool;

    public static Material Stone()
    {
        return UrpMaterialUtil.CreateLit(NetFoldTheme.Stone, false, 0.12f, 0.58f);
    }

    public static Material StoneDeep()
    {
        return UrpMaterialUtil.CreateLit(NetFoldTheme.StoneDeep, false, 0.08f, 0.36f);
    }

    public static Material Brass()
    {
        return UrpMaterialUtil.CreateLit(NetFoldTheme.Brass, false, 0.92f, 0.62f, true, NetFoldTheme.Brass * 0.18f);
    }

    public static void Install(Transform root, Camera cam, Vector3 focus)
    {
        ApplyAtmosphere();
        ApplyCamera(cam);
        BuildRoom(root, focus, cam);
        BuildLights(root);
        BuildVolume(root);
        BuildProbe(root, focus);
    }

    public static void ApplyCamera(Camera cam)
    {
        if (cam == null)
        {
            return;
        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = NetFoldTheme.Void;
        var data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (data == null)
        {
            data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        data.renderPostProcessing = true;
        data.renderShadows = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
    }

    public static void DressDesk(Transform desk)
    {
        if (desk == null)
        {
            return;
        }

        var top = desk.GetComponent<MeshRenderer>();
        if (top != null)
        {
            top.sharedMaterial = Stone();
            top.shadowCastingMode = ShadowCastingMode.On;
            top.receiveShadows = true;
        }

        Vector3 scale = desk.lossyScale;
        Vector3 world = desk.position;
        float bottom = world.y - scale.y * 0.5f;
        Transform parent = desk.parent != null ? desk.parent : desk;

        float lipH = 0.012f;
        Place(parent, "BrassLip", new Vector3(world.x, bottom - lipH * 0.5f, world.z), new Vector3(scale.x + 0.05f, lipH, scale.z + 0.05f), Brass());

        float footH = 0.018f;
        float columnBottom = footH;
        float columnTop = bottom - lipH;
        float columnH = Mathf.Max(0.06f, columnTop - columnBottom);
        Place(parent, "Pedestal", new Vector3(world.x, columnBottom + columnH * 0.5f, world.z), new Vector3(scale.x * 0.58f, columnH, scale.z * 0.58f), StoneDeep());
        Place(parent, "PedestalFoot", new Vector3(world.x, footH * 0.5f, world.z), new Vector3(scale.x * 0.74f, footH, scale.z * 0.74f), Brass());
    }

    static void ApplyAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.32f, 0.33f, 0.36f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.14f, 0.15f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.055f, 0.05f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = NetFoldTheme.Void;
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.reflectionIntensity = 0.85f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
    }

    static void BuildRoom(Transform root, Vector3 focus, Camera cam)
    {
        Vector3 face = new Vector3(focus.x, 1.35f, focus.z);
        var back = Wall(root, "CoveBack", new Vector3(focus.x, 1.85f, focus.z + 4.15f), new Vector3(9.5f, 4.8f, 1f), face);
        Wall(root, "CoveLeft", new Vector3(focus.x - 3.5f, 1.85f, focus.z + 2.15f), new Vector3(6.2f, 4.8f, 1f), face);
        Wall(root, "CoveRight", new Vector3(focus.x + 3.5f, 1.85f, focus.z + 2.15f), new Vector3(6.2f, 4.8f, 1f), face);
        if (back != null)
        {
            back.GetComponent<MeshRenderer>().sharedMaterial = WallMat();
        }

        var floor = LabFactory.Primitive(PrimitiveType.Cube, "StudioFloor", root, new Vector3(focus.x, -0.02f, focus.z), new Vector3(16f, 0.04f, 16f), UrpMaterialUtil.CreateLit(NetFoldTheme.Floor, false, 0.06f, 0.38f), false);
        var floorRend = floor.GetComponent<MeshRenderer>();
        floorRend.shadowCastingMode = ShadowCastingMode.Off;
        floorRend.receiveShadows = true;

        var pool = LabFactory.Primitive(PrimitiveType.Quad, "LightPool", root, new Vector3(focus.x, 0.012f, focus.z), new Vector3(3.4f, 3.4f, 1f), PoolMat(), false);
        pool.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        var poolRend = pool.GetComponent<MeshRenderer>();
        poolRend.shadowCastingMode = ShadowCastingMode.Off;
        poolRend.receiveShadows = false;

        var practical = new GameObject("Practical").AddComponent<Light>();
        practical.transform.SetParent(root, false);
        practical.transform.position = new Vector3(focus.x - 0.15f, 1.15f, focus.z - 1.15f);
        practical.type = LightType.Point;
        practical.color = new Color(1f, 0.9f, 0.74f);
        practical.intensity = 3.2f;
        practical.range = 3.6f;
        practical.shadows = LightShadows.None;
    }

    static GameObject Wall(Transform root, string name, Vector3 pos, Vector3 scale, Vector3 face)
    {
        var go = LabFactory.Primitive(PrimitiveType.Quad, name, root, pos, scale, WallMat(), false);
        go.transform.rotation = Quaternion.LookRotation(face - pos, Vector3.up);
        var rend = go.GetComponent<MeshRenderer>();
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;
        return go;
    }

    static void Place(Transform parent, string name, Vector3 world, Vector3 scale, Material mat)
    {
        Vector3 local = parent.InverseTransformPoint(world);
        var go = LabFactory.Primitive(PrimitiveType.Cube, name, parent, local, scale, mat, false);
        var rend = go.GetComponent<MeshRenderer>();
        rend.shadowCastingMode = ShadowCastingMode.On;
        rend.receiveShadows = true;
    }

    static void BuildLights(Transform root)
    {
        var key = new GameObject("KeyLight").AddComponent<Light>();
        key.transform.SetParent(root, false);
        key.type = LightType.Directional;
        key.color = new Color(1f, 0.93f, 0.84f);
        key.intensity = 2.1f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.78f;
        key.shadowBias = 0.02f;
        key.shadowNormalBias = 0.18f;
        key.transform.rotation = Quaternion.Euler(36f, -32f, 0f);

        var fill = new GameObject("FillLight").AddComponent<Light>();
        fill.transform.SetParent(root, false);
        fill.type = LightType.Directional;
        fill.color = new Color(0.62f, 0.72f, 0.86f);
        fill.intensity = 0.55f;
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(14f, 148f, 0f);

        var rim = new GameObject("RimLight").AddComponent<Light>();
        rim.transform.SetParent(root, false);
        rim.type = LightType.Directional;
        rim.color = new Color(1f, 0.84f, 0.62f);
        rim.intensity = 0.7f;
        rim.shadows = LightShadows.None;
        rim.transform.rotation = Quaternion.Euler(22f, 196f, 0f);
    }

    static void BuildVolume(Transform root)
    {
        var go = new GameObject("StudioVolume");
        go.transform.SetParent(root, false);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.28f);
        color.contrast.Override(12f);
        color.saturation.Override(-6f);
        color.colorFilter.Override(new Color(1f, 0.98f, 0.94f));

        var split = profile.Add<SplitToning>(true);
        split.shadows.Override(new Color(0.32f, 0.4f, 0.55f));
        split.highlights.Override(new Color(1f, 0.86f, 0.68f));
        split.balance.Override(-8f);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.05f);
        bloom.intensity.Override(0.32f);
        bloom.scatter.Override(0.72f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.42f);
        vignette.color.Override(new Color(0.02f, 0.02f, 0.025f));
    }

    static void BuildProbe(Transform root, Vector3 focus)
    {
        var go = new GameObject("StudioProbe");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(focus.x, 1.2f, focus.z);
        var probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.size = new Vector3(14f, 8f, 14f);
        probe.center = Vector3.zero;
        probe.intensity = 0.8f;
        probe.boxProjection = true;
        probe.resolution = 128;
        probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
        probe.backgroundColor = NetFoldTheme.Horizon;
        probe.hdr = true;
        go.AddComponent<StudioProbeKick>().Probe = probe;
    }

    static Material WallMat()
    {
        if (_wall == null)
        {
            const int h = 64;
            _wall = new Texture2D(4, h, TextureFormat.RGBA32, false);
            _wall.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);
                Color bottom = new Color(0.16f, 0.16f, 0.17f);
                Color horizon = new Color(0.42f, 0.34f, 0.24f);
                Color top = new Color(0.16f, 0.17f, 0.2f);
                float band = Mathf.Exp(-Mathf.Pow((t - 0.62f) / 0.28f, 2f));
                Color c = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, t));
                c = Color.Lerp(c, horizon, band);
                _wall.SetPixel(0, y, c);
                _wall.SetPixel(1, y, c);
                _wall.SetPixel(2, y, c);
                _wall.SetPixel(3, y, c);
            }

            _wall.Apply();
        }

        return Unlit(_wall, Color.white, false, false);
    }

    static Material PoolMat()
    {
        if (_pool == null)
        {
            const int n = 128;
            _pool = new Texture2D(n, n, TextureFormat.RGBA32, false);
            _pool.wrapMode = TextureWrapMode.Clamp;
            float half = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * 0.85f;
                    _pool.SetPixel(x, y, new Color(0.9f * a, 0.78f * a, 0.55f * a, a));
                }
            }

            _pool.Apply();
        }

        return Unlit(_pool, Color.white, true, true);
    }

    static Material Unlit(Texture tex, Color tint, bool transparent, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find(transparent ? "Unlit/Transparent" : "Unlit/Texture");
        }

        var mat = new Material(shader);
        if (tex != null && mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
        }

        if (tex != null && mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tex);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", tint);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", tint);
        }

        if (mat.HasProperty("_Cull"))
        {
            mat.SetFloat("_Cull", 0f);
        }

        if (!transparent)
        {
            return mat;
        }

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
        }

        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetFloat("_ZWrite", 0f);
        }

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = additive ? 3100 : 3000;
        return mat;
    }
}

public class StudioProbeKick : MonoBehaviour
{
    public ReflectionProbe Probe;

    void LateUpdate()
    {
        if (Probe != null)
        {
            Probe.RenderProbe();
        }

        enabled = false;
    }
}
