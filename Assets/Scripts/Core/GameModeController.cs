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
                return "步骤 1 / 4  认识正方体";
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
                return "立体图形由面围成，面和面相交成棱，棱和棱相交成顶点。正方体有 6 个全等的正方形面、12 条棱、8 个顶点。相对的面互相平行，相邻的棱互相垂直。后面的展开、三视图和截面都会用到这些数量。";
            case LearnStep.Unfold:
                return "沿着某些棱把立体剪开、铺平，得到的平面图形叫展开图。展开图必须能折回去，重新围成原来的立体。正方体一共有 11 种展开图。这些正方形要能折成封闭的盒子，面不能重叠，也不能缺一个面。";
            case LearnStep.Projection:
                return "三视图是从三个方向看同一个立体：正面是主视图，上面是俯视图，左面是左视图。蓝色是主视方向，绿色是俯视方向，紫色是左视方向。主视图和俯视图一样长，叫长对正；主视图和左视图一样高，叫高平齐；俯视图和左视图一样宽，叫宽相等。画三视图时，三个图的位置和尺寸都要符合这三句话。";
            case LearnStep.Section:
                return "用一个平面去截立体图形，平面和表面相交围成的图形叫截面。正方体的截面不一定是正方形。平面切得越斜，边数可以变多，截面可以是三角形、四边形、五边形，最多是六边形。右侧的截面边数会跟着切割角度变化。";
            default:
                return string.Empty;
        }
    }
}
