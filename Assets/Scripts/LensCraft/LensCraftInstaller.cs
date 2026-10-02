using UnityEngine;

public class LensCraftInstaller : MonoBehaviour
{
    public GameMode StartMode = GameMode.Learn;

    void Awake()
    {
        if (FindObjectOfType<OpticalBenchController>() != null)
        {
            return;
        }

        var lab = new GameObject("LensCraftLab").AddComponent<OpticalBenchController>();
        lab.Bootstrap(StartMode);
    }
}
