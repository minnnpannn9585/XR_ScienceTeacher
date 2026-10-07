using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>Run with -batchmode -executeMethod PolishSmokeChecks.Run -quit.</summary>
public static class PolishSmokeChecks
{
    [MenuItem("Tools/Science Studio/Run Polish Smoke Checks")]
    public static void Run()
    {
        GameObject root = new GameObject("PolishSmokeChecks");
        string[] modules = { "NetFold", "ParticleDrift", "LensCraft" };
        var existed = new bool[modules.Length];
        var scores = new int[modules.Length];
        for (int i = 0; i < modules.Length; i++)
        {
            string key = "ScienceStudio.BestStars." + modules[i];
            existed[i] = PlayerPrefs.HasKey(key);
            scores[i] = PlayerPrefs.GetInt(key);
        }
        try
        {
            foreach (string module in modules)
            {
                PlayerPrefs.DeleteKey("ScienceStudio.BestStars." + module);
                Require(ChallengeProgress.BestStars(module) == 0, "Fresh module starts at zero");
                Require(!ChallengeProgress.Record(module, 0), "Incomplete attempt is not a record");
                Require(ChallengeProgress.Record(module + "Challenge", 2), "Challenge saves a record");
                Require(ChallengeProgress.BestStars(module + "Free") == 2, "Modes share module progress");
                Require(!ChallengeProgress.Record(module, 1), "Lower result cannot replace best");
                Require(!ChallengeProgress.Record(module, 2), "Equal result is not a new record");
                Require(ChallengeProgress.Record(module, 3), "Improved result replaces best");
                Require(ChallengeProgress.BestStars(module) == 3, "Three stars retained");
            }
            Require(!ChallengeProgress.Record("MainMenu", 3), "Unknown scene cannot save a result");
            Require(ChallengeProgress.StarsText(-1) == "☆☆☆", "Negative star count clamps");
            Require(ChallengeProgress.StarsText(5) == "★★★", "Large star count clamps");

            var rating = root.AddComponent<StarRatingController>();
            rating.ResetRound();
            Require(rating.EvaluateStars() == 0, "Incomplete challenge has zero stars");
            rating.RegisterSuccess();
            Require(rating.EvaluateStars() == 3, "Unaided success has three stars");
            rating.RegisterHint();
            Require(rating.EvaluateStars() == 2, "One hint has two stars");
            rating.RegisterRetry();
            Require(rating.EvaluateStars() == 1, "Retry has one star");
            rating.ResetRound();
            Require(rating.EvaluateStars() == 0 && rating.HintCount == 0 && rating.RetryCount == 0, "Restart clears round state");

            var canvas = UiFactory.CreateOverlay("ChecksCanvas", root.transform);
            Require(canvas.GetComponent<CanvasScaler>().screenMatchMode == CanvasScaler.ScreenMatchMode.Expand, "Layout remains inside viewport");
            var button = UiFactory.Button(canvas.transform, "Button", "挑战", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);
            Require(button.targetGraphic.color == Color.white, "Button base does not multiply the tint");
            Require(button.colors.normalColor == NetFoldTheme.AccentDeep, "Button uses intended fill");
            Require(button.colors.selectedColor != button.colors.normalColor, "Keyboard selection is visible");
            UiFactory.SetButtonColor(button, NetFoldTheme.Error);
            Require(button.targetGraphic.color == Color.white && button.colors.normalColor == NetFoldTheme.Error, "State updates keep a neutral base");

            var geometryUi = root.AddComponent<NetFoldUI>();
            geometryUi.Build(root.AddComponent<NetFoldLab>(), GameMode.Challenge);
            Require(geometryUi.Result != null && geometryUi.Result.Retry != null, "Geometry challenge creates a retryable result");
            var lensUi = root.AddComponent<LensCraftUI>();
            lensUi.Build(root.AddComponent<OpticalBenchController>(), GameMode.Challenge);
            Require(lensUi.Result != null && lensUi.Result.Retry != null, "Lens challenge creates a retryable result");
            var particleUi = root.AddComponent<ParticleDriftUI>();
            particleUi.Build(root.AddComponent<ExperimentController>(), null, GameMode.Challenge);
            Require(particleUi.Result != null && particleUi.Result.Retry != null, "Particle challenge creates a retryable result");

            var challenge = root.AddComponent<ChallengeController>();
            bool advanced = false;
            // An unregistered active tween exercises cancellation without creating
            // a DontDestroyOnLoad host while the editor is outside Play Mode.
            var pending = new Sequence().OnComplete(() => advanced = true);
            typeof(ChallengeController).GetField("_advance", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(challenge, pending);
            challenge.Stop();
            Require(!pending.IsActive && !advanced, "Stopping cancels pending next question");

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/polish-smoke-checks.txt", "PASS: progress persistence, rating, UI tint/navigation, viewport scaling, all three challenge result flows, delayed question cancellation.\n");
            Debug.Log("POLISH_SMOKE_CHECKS_PASSED");
        }
        finally
        {
            // Never leave test scores in the player's progress.
            for (int i = 0; i < modules.Length; i++)
            {
                string key = "ScienceStudio.BestStars." + modules[i];
                if (existed[i]) PlayerPrefs.SetInt(key, scores[i]);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Polish check failed: " + message);
    }
}
