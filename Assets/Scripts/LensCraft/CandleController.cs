using UnityEngine;

public class CandleController : MonoBehaviour, IInteractable
{
    public OpticalBenchController Bench;
    public bool IsSelected { get; private set; }
    public bool CanDrag => Bench == null || !Bench.LockCandle;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    ParticleSystem _flame;
    Light _glow;

    public void Build()
    {
        LabFactory.Primitive(PrimitiveType.Cylinder, "Stick", transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.012f, 0.045f, 0.012f), LabFactory.Lit(new Color(0.95f, 0.9f, 0.75f), false, 0f, 0.25f), false);
        LabFactory.Primitive(PrimitiveType.Cube, "Wax", transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.028f, 0.07f, 0.028f), LabFactory.Lit(new Color(0.93f, 0.82f, 0.55f), false, 0f, 0.3f), true);
        var flameGo = new GameObject("Flame");
        flameGo.transform.SetParent(transform, false);
        flameGo.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        _flame = flameGo.AddComponent<ParticleSystem>();
        var main = _flame.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.18f;
        main.startSize = 0.02f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.25f), new Color(1f, 0.35f, 0.05f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 40;
        var emission = _flame.emission;
        emission.rateOverTime = 22f;
        var shape = _flame.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.004f;
        shape.angle = 12f;
        var renderer = flameGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = LabFactory.ParticleMat(new Color(1f, 0.7f, 0.2f, 1f));
        _glow = flameGo.AddComponent<Light>();
        _glow.type = LightType.Point;
        _glow.range = 0.45f;
        _glow.intensity = 1.1f;
        _glow.color = new Color(1f, 0.72f, 0.28f);
        var box = gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.06f, 0f);
        box.size = new Vector3(0.09f, 0.16f, 0.09f);
    }

    public void Place(float objectDistance)
    {
        transform.localPosition = new Vector3(-objectDistance, 0f, 0f);
    }

    public void SetLit(bool on)
    {
        if (_flame != null)
        {
            var emission = _flame.emission;
            emission.rateOverTime = on ? 22f : 0f;
        }

        if (_glow != null)
        {
            _glow.intensity = on ? 1.1f : 0.05f;
        }
    }

    public void OnSelect()
    {
        IsSelected = true;
    }

    public void OnDeselect()
    {
        IsSelected = false;
    }

    public void OnDragStart(Vector3 worldPoint, Ray pointerRay)
    {
    }

    public void OnDrag(Vector3 worldPoint, Ray pointerRay)
    {
        if (!CanDrag || Bench == null)
        {
            return;
        }

        if (!Bench.RayOnBench(pointerRay, out Vector3 local))
        {
            return;
        }

        float u = Mathf.Clamp(-local.x, 0.08f, 0.72f);
        Bench.ObjectDistance = u;
        Place(u);
    }

    public void OnDragEnd()
    {
    }

    public void Rotate(Vector2 delta)
    {
    }

    public void Scale(float delta)
    {
    }
}
