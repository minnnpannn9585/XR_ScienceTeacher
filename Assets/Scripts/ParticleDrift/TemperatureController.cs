using UnityEngine;

public class TemperatureController : MonoBehaviour
{
    public bool CurveVisible = true;
    public Texture2D CurveTexture { get; private set; }

    readonly float[] _samples = new float[96];
    int _count;
    float _timer;
    Color32[] _pix;
    const int W = 280;
    const int H = 96;

    public void Build()
    {
        CurveTexture = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "TemperatureCurve"
        };
        _pix = new Color32[W * H];
        ResetCurve(25f);
    }

    public void ResetCurve(float celsius)
    {
        _count = 0;
        _timer = 0f;
        Push(celsius);
        Paint(0f);
    }

    public void Tick(float dt, float celsius, float progress)
    {
        _timer += dt;
        if (_timer < 0.15f)
        {
            return;
        }

        _timer = 0f;
        Push(celsius);
        Paint(progress);
    }

    public static float DurationFor(float celsius)
    {
        float p = Mathf.Pow(Mathf.Clamp01(celsius / 100f), 0.85f);
        return Mathf.Lerp(14f, 2.2f, p);
    }

    public static float MotionScale(float celsius)
    {
        return Mathf.Lerp(0.28f, 2.8f, Mathf.Clamp01(celsius / 100f));
    }

    public static int BandFor(float celsius)
    {
        float duration = DurationFor(celsius);
        if (duration <= 4.2f)
        {
            return 0;
        }

        if (duration <= 8.8f)
        {
            return 1;
        }

        return 2;
    }

    public static string BandName(int band)
    {
        if (band == 0)
        {
            return "很快";
        }

        if (band == 1)
        {
            return "中等";
        }

        return "很慢";
    }

    void Push(float celsius)
    {
        if (_count < _samples.Length)
        {
            _samples[_count++] = celsius;
            return;
        }

        for (int i = 1; i < _samples.Length; i++)
        {
            _samples[i - 1] = _samples[i];
        }

        _samples[_samples.Length - 1] = celsius;
    }

    void Paint(float progress)
    {
        var bg = new Color32(8, 18, 36, 230);
        var grid = new Color32(40, 64, 96, 255);
        var line = new Color32(120, 214, 255, 255);
        var mark = new Color32(255, 176, 72, 255);
        for (int i = 0; i < _pix.Length; i++)
        {
            _pix[i] = bg;
        }

        for (int g = 1; g < 4; g++)
        {
            int y = Mathf.Clamp(Mathf.RoundToInt((g * 25f / 100f) * (H - 10) + 4), 0, H - 1);
            for (int x = 0; x < W; x++)
            {
                _pix[y * W + x] = grid;
            }
        }

        if (_count >= 2)
        {
            for (int i = 1; i < _count; i++)
            {
                int x0 = Mathf.RoundToInt((i - 1) / (float)(_samples.Length - 1) * (W - 1));
                int x1 = Mathf.RoundToInt(i / (float)(_samples.Length - 1) * (W - 1));
                DrawLine(x0, SampleY(_samples[i - 1]), x1, SampleY(_samples[i]), line);
            }
        }

        if (progress > 0.02f)
        {
            int y = Mathf.Clamp(Mathf.RoundToInt(progress * (H - 10) + 4), 0, H - 1);
            for (int x = 0; x < W; x += 2)
            {
                _pix[y * W + x] = mark;
            }
        }

        CurveTexture.SetPixels32(_pix);
        CurveTexture.Apply(false);
    }

    int SampleY(float celsius)
    {
        return Mathf.Clamp(Mathf.RoundToInt((Mathf.Clamp(celsius, 0f, 100f) / 100f) * (H - 10) + 4), 1, H - 2);
    }

    void DrawLine(int x0, int y0, int x1, int y1, Color32 color)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        if (steps < 1)
        {
            steps = 1;
        }

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            Plot(x, y, color);
            Plot(x, y + 1, color);
        }
    }

    void Plot(int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= W || y >= H)
        {
            return;
        }

        _pix[y * W + x] = color;
    }
}
