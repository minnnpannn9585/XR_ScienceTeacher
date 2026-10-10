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
                return Loc.Get("netfold.step1.title");
            case LearnStep.Unfold:
                return Loc.Get("netfold.step2.title");
            case LearnStep.Projection:
                return Loc.Get("netfold.step3.title");
            case LearnStep.Section:
                return Loc.Get("netfold.step4.title");
            default:
                return string.Empty;
        }
    }

    public static string StepHint(LearnStep step)
    {
        switch (step)
        {
            case LearnStep.Recognize:
                return Loc.Get("netfold.step1.body");
            case LearnStep.Unfold:
                return Loc.Get("netfold.step2.body");
            case LearnStep.Projection:
                return Loc.Get("netfold.step3.body");
            case LearnStep.Section:
                return Loc.Get("netfold.step4.body");
            default:
                return string.Empty;
        }
    }
}
