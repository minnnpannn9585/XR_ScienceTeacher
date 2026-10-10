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
            result.Nature = Loc.Get("lens.noImage");
            result.Rule = Loc.Get("lens.rule.focus");
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

        string upright = result.IsInverted ? Loc.Get("lens.inverted") : Loc.Get("lens.upright");
        string size = result.Scale == ImageScaleKind.Equal ? Loc.Get("lens.equal") : result.Scale == ImageScaleKind.Magnified ? Loc.Get("lens.magnified") : Loc.Get("lens.reduced");
        string kind = result.IsReal ? Loc.Get("lens.real") : Loc.Get("lens.virtual");
        result.Nature = Loc.Format("lens.nature", upright, size, kind);
        if (!result.IsReal)
        {
            result.Rule = Loc.Get("lens.rule.inside");
        }
        else if (u > 2f * f + 0.012f)
        {
            result.Rule = Loc.Get("lens.rule.beyond");
        }
        else if (Mathf.Abs(u - 2f * f) <= 0.016f)
        {
            result.Rule = Loc.Get("lens.rule.twice");
        }
        else
        {
            result.Rule = Loc.Get("lens.rule.between");
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
