using UnityEngine;

public enum ImageScaleKind
{
    None,
    Reduced,
    Equal,
    Magnified
}

public struct ImageResult
{
    public float ObjectDistance;
    public float ImageDistance;
    public float AbsMagnification;
    public bool HasImage;
    public bool IsReal;
    public bool IsInverted;
    public ImageScaleKind Scale;
    public string Nature;
    public string Rule;
}

public static class LensMath
{
    public static ImageResult Evaluate(float u, float f)
    {
        u = Mathf.Max(0.05f, u);
        f = Mathf.Max(0.08f, f);
        var result = new ImageResult { ObjectDistance = u };
        if (Mathf.Abs(u - f) <= 0.012f)
        {
            result.HasImage = false;
            result.ImageDistance = float.PositiveInfinity;
            result.AbsMagnification = 0f;
            result.Scale = ImageScaleKind.None;
            result.Nature = "不成像";
            result.Rule = "u = f，折射光线平行射出，不成像";
            return result;
        }

        float v = (u * f) / (u - f);
        result.HasImage = true;
        result.ImageDistance = v;
        result.IsReal = v > 0f;
        result.IsInverted = result.IsReal;
        result.AbsMagnification = Mathf.Abs(v / u);
        if (result.IsReal && Mathf.Abs(u - 2f * f) <= 0.016f)
        {
            result.Scale = ImageScaleKind.Equal;
        }
        else if (result.AbsMagnification < 0.92f)
        {
            result.Scale = ImageScaleKind.Reduced;
        }
        else if (result.AbsMagnification > 1.08f)
        {
            result.Scale = ImageScaleKind.Magnified;
        }
        else
        {
            result.Scale = ImageScaleKind.Equal;
        }

        string upright = result.IsInverted ? "倒立" : "正立";
        string size = result.Scale == ImageScaleKind.Equal ? "等大" : result.Scale == ImageScaleKind.Magnified ? "放大" : "缩小";
        string kind = result.IsReal ? "实像" : "虚像";
        result.Nature = upright + size + kind;
        if (!result.IsReal)
        {
            result.Rule = "u < f，成正立放大虚像";
        }
        else if (u > 2f * f + 0.012f)
        {
            result.Rule = "u > 2f，成倒立缩小实像";
        }
        else if (Mathf.Abs(u - 2f * f) <= 0.016f)
        {
            result.Rule = "u = 2f，成倒立等大实像";
        }
        else
        {
            result.Rule = "f < u < 2f，成倒立放大实像";
        }

        return result;
    }

    public static float Sharpness(ImageResult image, float screenDistance)
    {
        if (!image.HasImage || !image.IsReal || float.IsInfinity(image.ImageDistance))
        {
            return 0f;
        }

        float error = Mathf.Abs(screenDistance - image.ImageDistance);
        return 1f - Mathf.Clamp01(error / 0.07f);
    }

    public static string Centimeters(float meters)
    {
        if (float.IsNaN(meters) || float.IsInfinity(meters))
        {
            return "—";
        }

        return (meters * 100f).ToString("0.0") + " cm";
    }
}
