using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UiFactory
{
    static TMP_FontAsset _font;
    static Sprite _round;
    static Sprite _white;
    static Sprite _wash;

    public static TMP_FontAsset DefaultFont
    {
        get
        {
            if (_font != null)
            {
                return _font;
            }

            // simhei SDF 是静态图集，只含部分 CJK、没有 ASCII，缺字会完全不显示。
            // 必须用 simhei.ttf 做成动态 TMP 字体，运行时按需补字。
            Font simheiTtf = LoadSimheiTtf();
            if (simheiTtf != null)
            {
                _font = TMP_FontAsset.CreateFontAsset(simheiTtf);
                if (_font != null)
                {
                    _font.name = "simhei Dynamic";
                    _font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    AddLiberationFallback(_font);
                    return _font;
                }
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                _font = TMP_Settings.defaultFontAsset;
                AddLiberationFallback(_font);
                return _font;
            }

            var osFont = UnityEngine.Font.CreateDynamicFontFromOSFont("SimHei", 42);
            _font = osFont != null ? TMP_FontAsset.CreateFontAsset(osFont) : null;
            return _font;
        }
    }

    static Font LoadSimheiTtf()
    {
#if UNITY_EDITOR
        var fromAssets = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/simhei.ttf");
        if (fromAssets != null)
        {
            return fromAssets;
        }
#endif
        var fromResources = Resources.Load<Font>("simhei");
        if (fromResources != null)
        {
            return fromResources;
        }

        var config = Resources.Load<NetFoldFontConfig>("NetFoldFontConfig");
        return config != null ? config.SimheiTtf : null;
    }

    static void AddLiberationFallback(TMP_FontAsset font)
    {
        if (font == null)
        {
            return;
        }

        var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fallback == null)
        {
            return;
        }

        if (font.fallbackFontAssetTable == null)
        {
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        }

        if (!font.fallbackFontAssetTable.Contains(fallback))
        {
            font.fallbackFontAssetTable.Add(fallback);
        }
    }

    public static Sprite RoundSprite
    {
        get
        {
            if (_round != null)
            {
                return _round;
            }

            const int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float r = 28f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Min(x, s - 1 - x);
                    float dy = Mathf.Min(y, s - 1 - y);
                    float a = 1f;
                    if (dx < r && dy < r)
                    {
                        float d = Vector2.Distance(new Vector2(dx, dy), new Vector2(r, r));
                        a = Mathf.Clamp01(r - d + 0.5f);
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            _round = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _round;
        }
    }

    public static Sprite WhiteSprite
    {
        get
        {
            if (_white != null)
            {
                return _white;
            }

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            return _white;
        }
    }

    public static Sprite WashSprite
    {
        get
        {
            if (_wash != null)
            {
                return _wash;
            }

            const int w = 256;
            const int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color ink = NetFoldTheme.Void;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x / (w - 1f);
                    float v = y / (h - 1f);
                    float fade = Mathf.SmoothStep(0.94f, 0.02f, Mathf.InverseLerp(0.02f, 0.72f, u));
                    float edge = Mathf.SmoothStep(0.15f, 1f, Mathf.Abs(v - 0.5f) * 2f);
                    fade = Mathf.Clamp01(fade + edge * 0.28f * (1f - u));
                    Color c = Color.Lerp(ink, NetFoldTheme.Horizon, (1f - Mathf.Abs(v - 0.42f) * 1.4f) * 0.35f);
                    c.a = fade;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            _wash = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _wash;
        }
    }

    public static Canvas CreateOverlay(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static Canvas CreateWorld(string name, Transform parent, Vector3 pos, Vector2 size, Vector3 euler)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        go.transform.localScale = Vector3.one * 0.0012f;
        return canvas;
    }

    public static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var img = go.GetComponent<Image>();
        img.sprite = RoundSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        go.AddComponent<CanvasGroup>();
        Hairline(go.transform);
        return img;
    }

    public static Image ScreenWash(Transform parent)
    {
        var go = new GameObject("Wash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = WashSprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;
        return img;
    }

    static void Hairline(Transform parent)
    {
        var go = new GameObject("Hairline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-36f, 2f);
        rt.anchoredPosition = new Vector2(0f, -12f);
        var img = go.GetComponent<Image>();
        img.sprite = WhiteSprite;
        img.color = NetFoldTheme.Hairline;
        img.raycastTarget = false;
    }

    public static TMP_Text Label(Transform parent, string name, string text, int size, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = DefaultFont;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color ?? NetFoldTheme.Text;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    public static Button Button(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, UnityAction onClick, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var img = go.GetComponent<Image>();
        img.sprite = RoundSprite;
        img.type = Image.Type.Sliced;
        Color fill = color ?? NetFoldTheme.AccentDeep;
        img.color = fill;
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = fill;
        colors.highlightedColor = Color.Lerp(fill, NetFoldTheme.Ivory, 0.18f);
        colors.pressedColor = Color.Lerp(fill, Color.black, 0.28f);
        colors.selectedColor = fill;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        Label(go.transform, "Label", text, 26, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        btn.onClick.AddListener(() =>
        {
            if (FeedbackService.Instance != null)
            {
                FeedbackService.Instance.Click();
            }

            onClick?.Invoke();
        });
        return btn;
    }
}

