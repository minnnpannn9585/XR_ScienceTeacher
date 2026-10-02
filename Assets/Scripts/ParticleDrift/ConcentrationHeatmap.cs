using UnityEngine;

public class ConcentrationHeatmap : MonoBehaviour
{
    public bool IsVisible { get; private set; }
    public float Center { get; private set; }
    public float Edge { get; private set; }

    const int Res = 28;
    Texture2D _tex;
    Color[] _cols;
    float[] _weights;
    Renderer _renderer;
    Transform _quad;
    int _frame;

    public void Build(float radius)
    {
        _tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "Heatmap"
        };
        _cols = new Color[Res * Res];
        _weights = new float[Res * Res];

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        var mat = new Material(shader);
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", _tex);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.white);
        }

        mat.mainTexture = _tex;
        UrpMaterialUtil.SetTransparent(mat);
        mat.renderQueue = 3160;

        var quad = LabFactory.Primitive(PrimitiveType.Quad, "Heatmap", transform, new Vector3(0f, 0.078f, 0f), new Vector3(radius * 2f, radius * 2f, 1f), mat, false);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        _quad = quad.transform;
        _renderer = quad.GetComponent<MeshRenderer>();
        _renderer.enabled = false;
        Clear();
    }

    public void SetHeight(float liquidHeight)
    {
        if (_quad != null)
        {
            _quad.localPosition = new Vector3(0f, liquidHeight + 0.006f, 0f);
        }
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (_renderer != null)
        {
            _renderer.enabled = visible;
        }
    }

    public void Clear()
    {
        Center = 0f;
        Edge = 0f;
        if (_cols == null)
        {
            return;
        }

        var low = new Color(0.12f, 0.28f, 0.85f, 0.78f);
        for (int i = 0; i < _cols.Length; i++)
        {
            _cols[i] = low;
            _weights[i] = 0f;
        }

        _tex.SetPixels(_cols);
        _tex.Apply(false);
    }

    public void Paint(Vector2[] dyeXz, int count)
    {
        if (!IsVisible || _weights == null)
        {
            return;
        }

        _frame++;
        if ((_frame & 1) == 1)
        {
            return;
        }

        for (int i = 0; i < _weights.Length; i++)
        {
            _weights[i] = 0f;
        }

        float radius = 0.05f;
        for (int i = 0; i < count; i++)
        {
            float u = Mathf.Clamp01((dyeXz[i].x / radius + 1f) * 0.5f);
            float v = Mathf.Clamp01((dyeXz[i].y / radius + 1f) * 0.5f);
            int cx = Mathf.RoundToInt(u * (Res - 1));
            int cy = Mathf.RoundToInt(v * (Res - 1));
            for (int y = cy - 3; y <= cy + 3; y++)
            {
                for (int x = cx - 3; x <= cx + 3; x++)
                {
                    if (x < 0 || y < 0 || x >= Res || y >= Res)
                    {
                        continue;
                    }

                    float dx = x - cx;
                    float dy = y - cy;
                    _weights[y * Res + x] += Mathf.Exp(-(dx * dx + dy * dy) * 0.28f);
                }
            }
        }

        float max = 0.0001f;
        for (int i = 0; i < _weights.Length; i++)
        {
            if (_weights[i] > max)
            {
                max = _weights[i];
            }
        }

        var low = new Color(0.1f, 0.25f, 0.9f, 0.8f);
        var high = new Color(0.95f, 0.08f, 0.16f, 0.88f);
        for (int i = 0; i < _cols.Length; i++)
        {
            _cols[i] = Color.Lerp(low, high, Mathf.Clamp01(_weights[i] / max));
        }

        int mid = (Res / 2) * Res + Res / 2;
        Center = count == 0 ? 0f : Mathf.Clamp01(_weights[mid] / max);
        float edgeSum = 0f;
        int edgeN = 0;
        for (int i = 0; i < Res; i++)
        {
            edgeSum += _weights[i] + _weights[(Res - 1) * Res + i];
            edgeN += 2;
        }

        Edge = count == 0 ? 0f : Mathf.Clamp01(edgeSum / (edgeN * max));
        _tex.SetPixels(_cols);
        _tex.Apply(false);
    }

    public string Describe()
    {
        if (Center <= 0.001f && Edge <= 0.001f)
        {
            return "尚无品红";
        }

        return "中心 " + Mathf.RoundToInt(Center * 100f) + "%    边缘 " + Mathf.RoundToInt(Edge * 100f) + "%";
    }
}
