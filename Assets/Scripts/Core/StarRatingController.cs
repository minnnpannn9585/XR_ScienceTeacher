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
            return "一次通过且未使用提示";
        }

        if (stars == 2)
        {
            return "使用了 1 次提示";
        }

        if (stars == 1)
        {
            return HintCount >= 2 ? "使用了 2 次以上提示或进行了重试" : "进行了重试";
        }

        return "尚未完成本关挑战";
    }
}
