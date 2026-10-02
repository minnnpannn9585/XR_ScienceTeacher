using UnityEngine;

public class NetFoldInstaller : MonoBehaviour
{
    public GameMode StartMode = GameMode.Learn;

    void Awake()
    {
        if (FindObjectOfType<NetFoldLab>() != null)
        {
            return;
        }

        var lab = new GameObject("NetFoldLab").AddComponent<NetFoldLab>();
        lab.Bootstrap(StartMode);
    }
}
