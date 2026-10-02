using UnityEngine;

public class ParticleDriftInstaller : MonoBehaviour
{
    public GameMode StartMode = GameMode.Learn;

    void Awake()
    {
        if (FindObjectOfType<ExperimentController>() != null)
        {
            return;
        }

        var lab = new GameObject("ParticleDriftLab").AddComponent<ExperimentController>();
        lab.Bootstrap(StartMode);
    }
}
