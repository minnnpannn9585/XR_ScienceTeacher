using UnityEngine;

public class StarRatingController : MonoBehaviour
{
    public int SuccessCount { get; private set; }
    public int RetryCount { get; private set; }
    public bool PassedOnce => SuccessCount > 0;

    public void ResetRound()
    {
        SuccessCount = 0;
        RetryCount = 0;
    }

    public void RegisterRetry()
    {
        RetryCount++;
    }

    public void RegisterSuccess()
    {
        SuccessCount = Mathf.Min(3, SuccessCount + 1);
    }

    public int EvaluateStars()
    {
        return Mathf.Clamp(SuccessCount, 0, 3);
    }

    public string EvaluateReason()
    {
        int stars = EvaluateStars();
        if (stars == 3)
        {
            return Loc.Get("stars.3");
        }

        if (stars == 2)
        {
            return Loc.Get("stars.2");
        }

        if (stars == 1) return Loc.Get("stars.1");
        return Loc.Get("stars.0");
    }
}
