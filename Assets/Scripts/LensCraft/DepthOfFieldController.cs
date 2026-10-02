using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthOfFieldController : MonoBehaviour
{
    Volume _volume;
    DepthOfField _dof;

    public void Build()
    {
        _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 20f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _volume.sharedProfile = profile;
        _dof = profile.Add<DepthOfField>(true);
        _dof.mode.Override(DepthOfFieldMode.Gaussian);
        _dof.gaussianStart.Override(1.2f);
        _dof.gaussianEnd.Override(2.4f);
        _dof.gaussianMaxRadius.Override(1f);
        _volume.weight = 0f;
    }

    public void Apply(OpticalBenchController bench, Camera camera)
    {
        if (_volume == null || _dof == null)
        {
            return;
        }

        _volume.weight = bench.DepthOn ? 1f : 0f;
        if (!bench.DepthOn || camera == null || bench.ScreenPlate == null)
        {
            return;
        }

        float focus = Vector3.Distance(camera.transform.position, bench.ScreenPlate.transform.position);
        float blur = Mathf.Lerp(0.25f, 1.3f, 1f - bench.Sharpness);
        _dof.gaussianStart.Override(Mathf.Max(0.2f, focus * 0.65f));
        _dof.gaussianEnd.Override(focus + blur);
        _dof.gaussianMaxRadius.Override(Mathf.Lerp(0.6f, 1.35f, 1f - bench.Sharpness));
    }
}
