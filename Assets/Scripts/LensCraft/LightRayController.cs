using System.Collections.Generic;
using UnityEngine;

public class LightRayController : MonoBehaviour
{
    OpticalBenchController _bench;
    LineRenderer _parallel;
    LineRenderer _center;
    LineRenderer _focus;
    MeshFilter _dashParallel;
    MeshFilter _dashCenter;
    MeshFilter _dashFocus;
    ParticleSystem _flow;
    ParticleSystem.Particle[] _particles;
    readonly List<Vector3> _path = new List<Vector3>(24);
    readonly List<Color> _pathColor = new List<Color>(24);

    public void Build(OpticalBenchController bench)
    {
        _bench = bench;
        _parallel = MakeLine(new Color(1f, 0.28f, 0.28f));
        _center = MakeLine(new Color(0.3f, 0.95f, 0.45f));
        _focus = MakeLine(new Color(0.35f, 0.55f, 1f));
        _dashParallel = MakeDash(new Color(1f, 0.45f, 0.45f, 0.85f));
        _dashCenter = MakeDash(new Color(0.45f, 1f, 0.6f, 0.85f));
        _dashFocus = MakeDash(new Color(0.5f, 0.7f, 1f, 0.85f));
        var go = new GameObject("RayFlow");
        go.transform.SetParent(transform, false);
        _flow = go.AddComponent<ParticleSystem>();
        var main = _flow.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.startLifetime = 1.2f;
        main.startSize = 0.012f;
        main.maxParticles = 72;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = _flow.emission;
        emission.rateOverTime = 0f;
        go.GetComponent<ParticleSystemRenderer>().material = LabFactory.ParticleMat(Color.white);
        _particles = new ParticleSystem.Particle[72];
        _flow.Play();
    }

    public void Redraw()
    {
        _path.Clear();
        _pathColor.Clear();
        bool show = _bench.LightOn && _bench.ShowRays && _bench.Imaging.ObjectDistance > 0.01f;
        float u = _bench.ObjectDistance;
        float f = _bench.Focal;
        float axis = OpticalBenchController.AxisY;
        float objectHeight = OpticalBenchController.ObjectHeight;
        Vector3 tip = new Vector3(-u, axis + objectHeight, 0f);
        Vector3 center = new Vector3(0f, axis, 0f);
        Vector3 focusFar = new Vector3(f, axis, 0f);
        bool image = _bench.Imaging.HasImage;
        bool real = image && _bench.Imaging.IsReal;
        float v = _bench.Imaging.ImageDistance;
        float imageY = axis - objectHeight * (image && !float.IsInfinity(v) ? v / u : 0f);
        Vector3 imagePoint = new Vector3(float.IsInfinity(v) ? 0.4f : v, imageY, 0f);

        Vector3 lensParallel = new Vector3(0f, tip.y, 0f);
        Vector3 afterParallel = real || !image
            ? (image ? imagePoint : lensParallel + (focusFar - lensParallel).normalized * 0.42f)
            : lensParallel + (focusFar - lensParallel).normalized * 0.36f;
        Draw(_parallel, _dashParallel, show && _bench.RayParallel, tip, lensParallel, afterParallel, !real && image, imagePoint, new Color(1f, 0.28f, 0.28f));

        Vector3 through = image
            ? (real ? imagePoint : center + (center - tip).normalized * 0.34f)
            : center + (center - tip).normalized * 0.45f;
        Draw(_center, _dashCenter, show && _bench.RayCenter, tip, center, through, !real && image, imagePoint, new Color(0.3f, 0.95f, 0.45f));

        float yHit = Mathf.Abs(u - f) < 0.012f ? axis : axis + (-objectHeight * f / (u - f));
        yHit = Mathf.Clamp(yHit, axis - 0.09f, axis + 0.09f);
        Vector3 lensFocus = new Vector3(0f, yHit, 0f);
        Vector3 afterFocus = new Vector3(real ? v : 0.4f, yHit, 0f);
        if (!image)
        {
            afterFocus = new Vector3(0.42f, yHit, 0f);
        }
        else if (!real)
        {
            afterFocus = new Vector3(0.36f, yHit, 0f);
        }

        Draw(_focus, _dashFocus, show && _bench.RayFocus, tip, lensFocus, afterFocus, !real && image, imagePoint, new Color(0.35f, 0.55f, 1f));
        Flow();
    }

    void Draw(LineRenderer line, MeshFilter dash, bool on, Vector3 a, Vector3 b, Vector3 c, bool dashedToImage, Vector3 imagePoint, Color color)
    {
        line.enabled = on;
        dash.gameObject.SetActive(on && dashedToImage);
        if (!on)
        {
            return;
        }

        line.positionCount = 3;
        line.SetPosition(0, a);
        line.SetPosition(1, b);
        line.SetPosition(2, c);
        Push(a, b, color);
        Push(b, c, color);
        if (dashedToImage)
        {
            FillDash(dash.sharedMesh, b, imagePoint);
            Push(imagePoint, b, color * 0.7f);
        }
        else
        {
            dash.sharedMesh.Clear();
        }
    }

    void Push(Vector3 a, Vector3 b, Color color)
    {
        _path.Add(a);
        _path.Add(b);
        _pathColor.Add(color);
    }

    void Flow()
    {
        if (_flow == null || _particles == null)
        {
            return;
        }

        int count = 0;
        float speed = 0.28f;
        int segments = _path.Count / 2;
        int per = segments == 0 ? 0 : Mathf.Max(1, 36 / segments);
        for (int s = 0; s < segments && count < _particles.Length; s++)
        {
            Vector3 a = transform.TransformPoint(_path[s * 2]);
            Vector3 b = transform.TransformPoint(_path[s * 2 + 1]);
            float length = Vector3.Distance(a, b);
            if (length < 0.001f)
            {
                continue;
            }

            for (int i = 0; i < per && count < _particles.Length; i++)
            {
                float phase = Mathf.Repeat(Time.time * speed / length + i / (float)per, 1f);
                _particles[count].position = Vector3.Lerp(a, b, phase);
                _particles[count].startColor = _pathColor[s];
                _particles[count].startSize = 0.012f;
                _particles[count].remainingLifetime = 1f;
                _particles[count].startLifetime = 1f;
                count++;
            }
        }

        _flow.SetParticles(_particles, count);
    }

    LineRenderer MakeLine(Color color)
    {
        var go = new GameObject("Ray");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.widthMultiplier = 0.006f;
        line.positionCount = 0;
        line.material = LabFactory.ParticleMat(color);
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.alignment = LineAlignment.View;
        return line;
    }

    MeshFilter MakeDash(Color color)
    {
        var go = new GameObject("Dash");
        go.transform.SetParent(transform, false);
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = new Mesh { name = "Dash" };
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = LabFactory.Lit(color, true, 0f, 0.1f, true, color);
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return filter;
    }

    static void FillDash(Mesh mesh, Vector3 a, Vector3 b)
    {
        var verts = new List<Vector3>(64);
        var tris = new List<int>(96);
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.001f)
        {
            mesh.Clear();
            return;
        }

        dir /= len;
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.Cross(dir, Vector3.forward);
        }

        side.Normalize();
        const float dash = 0.028f;
        const float gap = 0.018f;
        const float width = 0.0045f;
        for (float t = 0f; t < len; t += dash + gap)
        {
            float t1 = Mathf.Min(len, t + dash);
            Vector3 p0 = a + dir * t;
            Vector3 p1 = a + dir * t1;
            int i = verts.Count;
            verts.Add(p0 - side * width);
            verts.Add(p0 + side * width);
            verts.Add(p1 + side * width);
            verts.Add(p1 - side * width);
            tris.Add(i);
            tris.Add(i + 2);
            tris.Add(i + 1);
            tris.Add(i);
            tris.Add(i + 3);
            tris.Add(i + 2);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }
}
