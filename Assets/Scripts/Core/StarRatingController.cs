using UnityEngine;

public class StarRatingController : MonoBehaviour
{
    public int HintCount { get; private set; }
    public int RetryCount { get; private set; }
    public bool PassedOnce { get; private set; }

    public void ResetRound()
    {
        HintCount = 0;
        RetryCount = 0;
        PassedOnce = false;
    }

    public void RegisterHint()
    {
        HintCount++;
    }

    public void RegisterRetry()
    {
        RetryCount++;
    }

    public void RegisterSuccess()
    {
        PassedOnce = true;
    }

    public int EvaluateStars()
    {
        if (!PassedOnce)
        {
            return 0;
        }

        if (RetryCount == 0 && HintCount == 0)
        {
            return 3;
        }

        if (HintCount == 1 && RetryCount == 0)
        {
            return 2;
        }

        return 1;
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

        if (stars == 1)
        {
            return HintCount >= 2 ? Loc.Get("stars.1.hints") : Loc.Get("stars.1.retry");
        }

        return Loc.Get("stars.0");
    }
}
