using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class MolecularMotionController : MonoBehaviour
{
    struct Mol
    {
        public Vector3 Home;
        public Vector3 Cluster;
        public Vector3 Now;
        public float Phase;
        public int Kind;
    }

    public int MoleculeCount => _mols.Count;
    public Vector2[] DyeXZ { get; private set; }
    public int DyeCount { get; private set; }
    public float AverageSpeed { get; private set; }

    readonly List<Mol> _mols = new List<Mol>(128);
    ParticleSystem _ps;
    ParticleSystemRenderer _renderer;
    ParticleSystem.Particle[] _buf;
    VisualEffect _vfx;
    float _radius = 0.045f;
    float _height = 0.07f;
    float _spread;
    float _boost = 1f;
    float _boostTime;
    float _fieldTime;
    float _flash;
    bool _rendered;
    Material _particleMaterial;
    Mesh _particleMesh;

    public void Build(float radius, float height)
    {
        _radius = radius;
        _height = height;
        DyeXZ = new Vector2[40];
        _buf = new ParticleSystem.Particle[160];
        for (int i = 0; i < 56; i++)
        {
            _mols.Add(Make(0));
        }

        var host = new GameObject("Molecules");
        host.transform.SetParent(transform, false);
        _ps = host.AddComponent<ParticleSystem>();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = _ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = 8f;
        main.startSpeed = 0f;
        main.startSize = 0.011f;
        main.maxParticles = 160;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.simulationSpeed = 0f;
        main.gravityModifier = 0f;
        var emission = _ps.emission;
        emission.enabled = false;
        var shape = _ps.shape;
        shape.enabled = false;
        _renderer = host.GetComponent<ParticleSystemRenderer>();
        _particleMaterial = LabFactory.ParticleMat(Color.white);
        _renderer.sharedMaterial = _particleMaterial;
        // Actual round silhouettes instead of untextured billboard squares.
        // 120 triangles per molecule, no transparent sprite overdraw or bloom.
        _particleMesh = BuildMoleculeMesh();
        _renderer.renderMode = ParticleSystemRenderMode.Mesh;
        _renderer.mesh = _particleMesh;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _ps.Play();
        SetupVfx(host.transform);
        SetRendered(false);
    }

    public void SetHeight(float height)
    {
        _height = height;
        for (int i = 0; i < _mols.Count; i++)
        {
            Mol mol = _mols[i];
            mol.Home.y = Mathf.Clamp(mol.Home.y, 0.012f, Mathf.Max(0.02f, _height - 0.008f));
            mol.Cluster.y = _height * 0.55f;
            _mols[i] = mol;
        }
    }

    public void SetSpread(float spread)
    {
        _spread = Mathf.Clamp01(spread);
    }

    public void BeginDye(int count)
    {
        ClearKind(1);
        count = Mathf.Clamp(count, 1, 36);
        for (int i = 0; i < count; i++)
        {
            _mols.Add(Make(1));
        }

        _spread = 0f;
    }

    public void ClearDye()
    {
        ClearKind(1);
        _spread = 0f;
        DyeCount = 0;
    }

    public void AddAlcohol(int count)
    {
        ClearKind(2);
        for (int i = 0; i < count; i++)
        {
            _mols.Add(Make(2));
        }
    }

    public void ClearAlcohol()
    {
        ClearKind(2);
    }

    public void Boost(float scale)
    {
        _boost = scale;
        _boostTime = 1.5f;
    }

    public void PulseField()
    {
        _fieldTime = 3.2f;
    }

    public void Flash()
    {
        _flash = 0.9f;
    }

    public void SetRendered(bool visible)
    {
        _rendered = visible;
        if (_renderer != null)
        {
            _renderer.enabled = visible;
        }

        if (_vfx != null)
        {
            _vfx.enabled = visible && _vfx.visualEffectAsset != null;
        }
    }

    public void Tick(float dt, float celsius)
    {
        if (_boostTime > 0f)
        {
            _boostTime -= dt;
            if (_boostTime <= 0f)
            {
                _boost = 1f;
            }
        }

        if (_fieldTime > 0f)
        {
            _fieldTime -= dt;
        }

        if (_flash > 0f)
        {
            _flash -= dt;
        }

        float scale = TemperatureController.MotionScale(celsius) * _boost;
        AverageSpeed = scale;
        float freq = 3.4f * scale;
        float amp = 0.0042f * scale;
        bool field = _fieldTime > 0f;
        DyeCount = 0;
        int n = _mols.Count;
        for (int i = 0; i < n; i++)
        {
            Mol mol = _mols[i];
            float spread = mol.Kind == 1 ? _spread : 1f;
            Vector3 pos = Vector3.Lerp(mol.Cluster, mol.Home, spread);
            float s = Time.time * freq + mol.Phase;
            pos.x += Mathf.Sin(s) * amp;
            pos.y += Mathf.Cos(s * 1.37f) * amp * 0.55f;
            pos.z += Mathf.Cos(s * 0.83f) * amp;
            if (field)
            {
                float push = (1f - Mathf.Clamp01(_fieldTime / 3.2f)) * 0.028f * Mathf.Max(0.6f, scale);
                pos.x += mol.Kind == 1 ? push : -push;
            }

            pos = Clamp(pos);
            mol.Now = pos;
            _mols[i] = mol;
            if (mol.Kind == 1 && DyeCount < DyeXZ.Length)
            {
                DyeXZ[DyeCount++] = new Vector2(pos.x, pos.z);
            }

            if (i < _buf.Length)
            {
                var particle = new ParticleSystem.Particle();
                particle.position = transform.TransformPoint(pos);
                particle.startColor = ColorOf(mol.Kind);
                float size = mol.Kind == 1 ? 0.008f : 0.0068f;
                particle.startSize = _rendered ? size * (1f + 0.12f * Mathf.Clamp01(scale / 2.4f)) : size;
                particle.remainingLifetime = 8f;
                particle.startLifetime = 8f;
                _buf[i] = particle;
            }
        }

        if (_ps != null)
        {
            _ps.SetParticles(_buf, Mathf.Min(n, _buf.Length));
        }

        if (_vfx != null && _vfx.enabled)
        {
            _vfx.playRate = Mathf.Lerp(0.35f, 2.5f, Mathf.Clamp01(celsius / 100f));
            if (_vfx.HasFloat("Rate"))
            {
                _vfx.SetFloat("Rate", Mathf.Lerp(4f, 18f, Mathf.Clamp01(celsius / 100f)));
            }
        }
    }

    void SetupVfx(Transform parent)
    {
        if (StudioSet.MobileHeadset)
        {
            return;
        }

        var asset = Resources.Load<VisualEffectAsset>("VFX/MoleculeMotion");
        if (asset == null)
        {
            return;
        }

        var go = new GameObject("MoleculeVfx");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, _height * 0.5f, 0f);
        go.transform.localScale = Vector3.one * 0.018f;
        _vfx = go.AddComponent<VisualEffect>();
        _vfx.visualEffectAsset = asset;
        _vfx.playRate = 1f;
    }

    Mol Make(int kind)
    {
        return new Mol
        {
            Home = RandomInside(),
            Cluster = new Vector3(Random.Range(-0.008f, 0.008f), _height * 0.55f, Random.Range(-0.008f, 0.008f)),
            Phase = Random.Range(0f, Mathf.PI * 2f),
            Kind = kind
        };
    }

    Vector3 RandomInside()
    {
        Vector2 circle = Random.insideUnitCircle * _radius;
        float y = Random.Range(0.012f, Mathf.Max(0.02f, _height - 0.008f));
        return new Vector3(circle.x, y, circle.y);
    }

    Vector3 Clamp(Vector3 p)
    {
        Vector2 xz = new Vector2(p.x, p.z);
        if (xz.magnitude > _radius)
        {
            xz = xz.normalized * _radius;
        }

        p.x = xz.x;
        p.z = xz.y;
        p.y = Mathf.Clamp(p.y, 0.01f, Mathf.Max(0.02f, _height - 0.006f));
        return p;
    }

    Color ColorOf(int kind)
    {
        if (_flash > 0f)
        {
            return Color.Lerp(kind == 1 ? new Color(0.92f, 0.09f, 0.48f) : new Color(0.40f, 0.83f, 0.96f), NetFoldTheme.Success, Mathf.Clamp01(_flash));
        }

        if (kind == 1)
        {
            return new Color(0.92f, 0.09f, 0.48f, 1f);
        }

        if (kind == 2)
        {
            return new Color(0.98f, 0.84f, 0.22f, 1f);
        }

        return new Color(0.40f, 0.83f, 0.96f, 1f);
    }

    void ClearKind(int kind)
    {
        for (int i = _mols.Count - 1; i >= 0; i--)
        {
            if (_mols[i].Kind == kind)
            {
                _mols.RemoveAt(i);
            }
        }
    }

    static Mesh BuildMoleculeMesh()
    {
        const int longitude = 12, latitude = 6;
        var vertices = new Vector3[(longitude + 1) * (latitude + 1)];
        var normals = new Vector3[vertices.Length];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>(longitude * (latitude - 1) * 6);
        for (int y = 0; y <= latitude; y++)
        {
            float theta = y * Mathf.PI / latitude;
            for (int x = 0; x <= longitude; x++)
            {
                float phi = x * Mathf.PI * 2f / longitude;
                int v = y * (longitude + 1) + x;
                normals[v] = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                vertices[v] = normals[v] * 0.5f;
                uv[v] = new Vector2((float)x / longitude, (float)y / latitude);
                if (y == latitude || x == longitude) continue;
                int next = v + longitude + 1;
                if (y > 0) { triangles.Add(v); triangles.Add(v + 1); triangles.Add(next); }
                if (y < latitude - 1) { triangles.Add(v + 1); triangles.Add(next + 1); triangles.Add(next); }
            }
        }
        var mesh = new Mesh { name = "Molecule Sphere 120", vertices = vertices, normals = normals, uv = uv };
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDestroy()
    {
        if (_particleMaterial != null) Destroy(_particleMaterial);
        if (_particleMesh != null) Destroy(_particleMesh);
    }
}
