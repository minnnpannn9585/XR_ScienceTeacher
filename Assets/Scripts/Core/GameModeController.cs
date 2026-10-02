using System;
using UnityEngine;

public enum GameMode
{
    Learn,
    Challenge,
    Free
}

public enum LearnStep
{
    Recognize = 0,
    Unfold = 1,
    Projection = 2,
    Section = 3
}

public class GameModeController : MonoBehaviour
{
    public GameMode Mode { get; private set; } = GameMode.Learn;
    public LearnStep Step { get; private set; } = LearnStep.Recognize;

    public event Action<GameMode> ModeChanged;
    public event Action<LearnStep> StepChanged;

    public void SetMode(GameMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        if (mode == GameMode.Learn)
        {
            Step = LearnStep.Recognize;
        }

        ModeChanged?.Invoke(Mode);
        if (mode == GameMode.Learn)
        {
            StepChanged?.Invoke(Step);
        }
    }

    public void SetStep(LearnStep step)
    {
        Step = step;
        if (Mode != GameMode.Learn)
        {
            Mode = GameMode.Learn;
            ModeChanged?.Invoke(Mode);
        }

        StepChanged?.Invoke(Step);
    }

    public void NextStep()
    {
        if (Mode != GameMode.Learn)
        {
            SetMode(GameMode.Learn);
            return;
        }

        if (Step < LearnStep.Section)
        {
            SetStep((LearnStep)((int)Step + 1));
        }
        else
        {
            SetMode(GameMode.Challenge);
        }
    }

    public void PrevStep()
    {
        if (Mode == GameMode.Challenge)
        {
            SetMode(GameMode.Learn);
            SetStep(LearnStep.Section);
            return;
        }

        if (Step > LearnStep.Recognize)
        {
            SetStep((LearnStep)((int)Step - 1));
        }
    }

    public static string StepTitle(LearnStep step)
    {
        switch (step)
        {
            case LearnStep.Recognize:
                return "步骤 1 / 4  认识几何体";
            case LearnStep.Unfold:
                return "步骤 2 / 4  展开与折叠";
            case LearnStep.Projection:
                return "步骤 3 / 4  三视图投影";
            case LearnStep.Section:
                return "步骤 4 / 4  截面探究";
            default:
                return string.Empty;
        }
    }

    public static string StepHint(LearnStep step)
    {
        switch (step)
        {
            case LearnStep.Recognize:
                return "立体图形由面围成，面和面相交成棱，棱和棱相交成顶点。下面依次看正方体、圆柱、圆锥和三棱柱。";
            case LearnStep.Unfold:
                return "沿着某些棱把立体图形剪开、铺平，得到的平面图形叫展开图。展开图可以再折回原来的立体。";
            case LearnStep.Projection:
                return "从正面、上面和左面看同一个立体，得到主视图、俯视图和左视图。长对正，高平齐，宽相等。";
            case LearnStep.Section:
                return "用一个平面去截立体图形，交线围成的图形叫截面。正方体的截面可以是三边形到六边形。";
            default:
                return string.Empty;
        }
    }
}
