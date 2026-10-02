using UnityEngine;

public class ImageFormationController : MonoBehaviour
{
    OpticalBenchController _bench;
    Transform _ghost;

    public void Build(OpticalBenchController bench)
    {
        _bench = bench;
        _ghost = new GameObject("VirtualImage").transform;
        _ghost.SetParent(transform, false);
        var body = LabFactory.Primitive(PrimitiveType.Cube, "Body", _ghost, new Vector3(0f, -0.02f, 0f), new Vector3(0.012f, 0.04f, 0.012f), LabFactory.Lit(new Color(0.7f, 0.85f, 1f, 0.45f), true, 0f, 0.2f, true, new Color(0.4f, 0.7f, 1f)), false);
        var flame = LabFactory.Primitive(PrimitiveType.Sphere, "Flame", _ghost, new Vector3(0f, 0.02f, 0f), new Vector3(0.02f, 0.028f, 0.02f), LabFactory.Lit(new Color(0.75f, 0.9f, 1f, 0.4f), true, 0f, 0.15f, true, new Color(0.5f, 0.8f, 1f)), false);
        body.GetComponent<MeshRenderer>().material = new Material(body.GetComponent<MeshRenderer>().sharedMaterial);
        flame.GetComponent<MeshRenderer>().material = new Material(flame.GetComponent<MeshRenderer>().sharedMaterial);
        _ghost.gameObject.AddComponent<FaceCamera>();
        _ghost.gameObject.SetActive(false);
    }

    public void Apply()
    {
        ImageResult image = _bench.Imaging;
        float sharp = _bench.Sharpness;
        bool realOnScreen = _bench.LightOn && image.HasImage && image.IsReal && !float.IsInfinity(image.ImageDistance);
        float height = 0f;
        if (image.HasImage && !float.IsInfinity(image.ImageDistance))
        {
            height = -OpticalBenchController.ObjectHeight * image.ImageDistance / image.ObjectDistance;
        }

        _bench.ScreenPlate.ShowImage(height, image.AbsMagnification, realOnScreen ? sharp : 0f, realOnScreen);
        bool ghost = _bench.LightOn && image.HasImage && !image.IsReal;
        _ghost.gameObject.SetActive(ghost);
        if (!ghost)
        {
            return;
        }

        float scale = Mathf.Clamp(image.AbsMagnification, 0.2f, 3f);
        _ghost.localPosition = new Vector3(image.ImageDistance, OpticalBenchController.AxisY + height, 0f);
        _ghost.localScale = Vector3.one * scale;
    }
}
