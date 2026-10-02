using UnityEngine;

public class NetFoldBurstDriver : MonoBehaviour
{
    ParticleSystem _ps;

    public static NetFoldBurstDriver Create(Transform parent)
    {
        var go = new GameObject("NetFoldVfxBurst");
        go.transform.SetParent(parent, false);
        var driver = go.AddComponent<NetFoldBurstDriver>();
        driver.Build();
        return driver;
    }

    void Build()
    {
        _ps = gameObject.AddComponent<ParticleSystem>();
        var main = _ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.8f;
        main.startSpeed = 1.8f;
        main.startSize = 0.03f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;
        var emission = _ps.emission;
        emission.rateOverTime = 0f;
        var trails = _ps.trails;
        trails.enabled = true;
        trails.lifetime = 0.25f;
    }

    public void Play(Vector3 pos, Color color)
    {
        transform.position = pos;
        var main = _ps.main;
        main.startColor = color;
        _ps.Emit(28);
    }
}
