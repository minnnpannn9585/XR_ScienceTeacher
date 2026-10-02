using UnityEngine;

public class DyeDiffusionController : MonoBehaviour
{
    public void Drop(BeakerController beaker, float volume01)
    {
        if (beaker == null)
        {
            return;
        }

        int count = Mathf.RoundToInt(Mathf.Lerp(12f, 32f, Mathf.Clamp01(volume01)));
        beaker.BeginDiffusion(count);
        if (FeedbackService.Instance != null)
        {
            FeedbackService.Instance.Click();
        }
    }
}
