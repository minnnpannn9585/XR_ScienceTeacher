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

        bool on = bench.DepthOn;
        _volume.weight = on ? 1f : 0f;
        if (StudioSet.MobileHeadset)
        {
            SetPost(camera, on);
            if (Camera.main != null && Camera.main != camera)
            {
                SetPost(Camera.main, on);
            }
        }

        if (!on || camera == null || bench.ScreenPlate == null)
        {
            return;
        }

        float focus = Vector3.Distance(camera.transform.position, bench.ScreenPlate.transform.position);
        if (StudioSet.MobileHeadset)
        {
            float mobileBlur = Mathf.Lerp(0.15f, 0.4f, 1f - bench.Sharpness);
            _dof.gaussianStart.Override(Mathf.Max(0.2f, focus * 0.75f));
            _dof.gaussianEnd.Override(focus + mobileBlur);
            _dof.gaussianMaxRadius.Override(Mathf.Lerp(0.2f, 0.45f, 1f - bench.Sharpness));
            return;
        }

        float blur = Mathf.Lerp(0.25f, 1.3f, 1f - bench.Sharpness);
        _dof.gaussianStart.Override(Mathf.Max(0.2f, focus * 0.65f));
        _dof.gaussianEnd.Override(focus + blur);
        _dof.gaussianMaxRadius.Override(Mathf.Lerp(0.6f, 1.35f, 1f - bench.Sharpness));
    }

    static void SetPost(Camera camera, bool enabled)
    {
        if (camera == null)
        {
            return;
        }

        var data = camera.GetComponent<UniversalAdditionalCameraData>();
        if (data != null)
        {
            data.renderPostProcessing = enabled;
        }
    }
}
