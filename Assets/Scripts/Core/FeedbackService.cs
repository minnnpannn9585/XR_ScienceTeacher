using DG.Tweening;
using UnityEngine;

public class FeedbackService : MonoBehaviour
{
    public static FeedbackService Instance { get; private set; }

    AudioSource _source;
    AudioClip _ok;
    AudioClip _bad;
    AudioClip _click;
    ParticleSystem _burst;

    void Awake()
    {
        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _ok = ToneClip("ok", 880f, 0.16f, 0.22f, true);
        _bad = ToneClip("bad", 180f, 0.2f, 0.18f, false);
        _click = ToneClip("click", 620f, 0.05f, 0.1f, true);
        _burst = CreateBurst();
    }

    void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public void Click()
    {
        Play(_click, 0.45f);
    }

    public void Success(Vector3 worldPos)
    {
        Play(_ok, 0.7f);
        Burst(worldPos, NetFoldTheme.Success, 36);
    }

    public void Error(Vector3 worldPos, Transform shake = null)
    {
        Play(_bad, 0.7f);
        Burst(worldPos, NetFoldTheme.Error, 18);
        if (shake != null)
        {
            Vector3 origin = shake.localPosition;
            shake.DOPunchScale(Vector3.one * 0.06f, 0.28f, 8, 0.6f);
            DOVirtual.Float(0f, 1f, 0.22f, t =>
            {
                shake.localPosition = origin + Random.insideUnitSphere * 0.012f;
            }).OnComplete(() => shake.localPosition = origin);
        }
    }

    public void Burst(Vector3 worldPos, Color color, int count)
    {
        if (_burst == null)
        {
            return;
        }

        var main = _burst.main;
        main.startColor = color;
        _burst.transform.position = worldPos;
        _burst.Emit(count);
    }

    void Play(AudioClip clip, float volume)
    {
        if (clip != null && _source != null)
        {
            _source.PlayOneShot(clip, volume);
        }
    }

    static AudioClip ToneClip(string name, float freq, float duration, float volume, bool rise)
    {
        int hz = 22050;
        int samples = Mathf.CeilToInt(hz * duration);
        var clip = AudioClip.Create(name, samples, 1, hz, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float f = rise ? freq * (1f + 0.18f * t) : freq * (1f - 0.25f * t);
            float env = Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)hz)) * env * volume;
        }

        clip.SetData(data, 0);
        return clip;
    }

    static ParticleSystem CreateBurst()
    {
        var go = new GameObject("FeedbackBurst");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.4f;
        main.startLifetime = 0.7f;
        main.startSpeed = 1.6f;
        main.startSize = 0.035f;
        main.gravityModifier = 0.15f;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = grad;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = UrpMaterialUtil.CreateLit(Color.white, true, 0f, 0.2f, true, Color.white);
        return ps;
    }
}
