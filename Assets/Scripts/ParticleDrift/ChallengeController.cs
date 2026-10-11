using System;
using UnityEngine;

namespace ParticleDrift
{
    public class ChallengeController : MonoBehaviour
    {
        public int Index { get; private set; }
        public bool Running { get; private set; }
        public float GivenTemperature { get; private set; }
        public const float ReferenceTemperature = 25f;
        public const float TargetMin = 5.5f;
        public const float TargetMax = 8.5f;

        public event Action<int, string, string> Presented;
        public event Action<int, string> Finished;

        StarRatingController _stars;
        bool _q2Rolled;

        public void Begin(StarRatingController stars)
        {
            _stars = stars;
            _stars.ResetRound();
            Index = 0;
            Running = true;
            _q2Rolled = false;
            Publish();
        }

        public void Stop()
        {
            Running = false;
        }

        public void Relayout()
        {
            if (!Running)
            {
                return;
            }

            Publish();
        }

        public bool TryGradeBeaker(BeakerController picked, BeakerController hot, out bool correct)
        {
            correct = false;
            if (!Running || Index != 0 || _stars == null)
            {
                return false;
            }

            correct = picked != null && picked == hot;
            if (!correct)
            {
                _stars.RegisterRetry();
            }

            return true;
        }

        public bool TryGradeBand(int band, out bool correct)
        {
            correct = false;
            if (!Running || Index != 1 || _stars == null)
            {
                return false;
            }

            correct = band == RelativeBandFor(GivenTemperature);
            if (!correct)
            {
                _stars.RegisterRetry();
            }

            return true;
        }

        public static int RelativeBandFor(float temperature)
        {
            if (temperature > ReferenceTemperature + 10f)
            {
                return 0;
            }

            if (temperature < ReferenceTemperature - 10f)
            {
                return 2;
            }

            return 1;
        }

        public bool TryGradeTime(float seconds, out string message)
        {
            message = string.Empty;
            if (!Running || Index != 2 || _stars == null)
            {
                return false;
            }

            if (seconds >= TargetMin && seconds <= TargetMax)
            {
                message = Loc.Format("drift.q.time.ok", seconds.ToString("0.0"));
                return true;
            }

            _stars.RegisterRetry();
            message = seconds > TargetMax ? Loc.Get("drift.q.time.cold") : Loc.Get("drift.q.time.hot");
            return false;
        }

        public void Advance()
        {
            if (!Running)
            {
                return;
            }

            _stars.RegisterSuccess();
            Index++;
            if (Index >= 3)
            {
                Running = false;
                Finished?.Invoke(_stars.EvaluateStars(), _stars.EvaluateReason());
                return;
            }

            Publish();
        }

        void Publish()
        {
            if (Index == 1 && !_q2Rolled)
            {
                _q2Rolled = true;
                float[] temps = { 75f, ReferenceTemperature, 8f };
                GivenTemperature = temps[UnityEngine.Random.Range(0, temps.Length)];
            }

            Presented?.Invoke(Index, Title(Index), Body(Index));
        }

        string Body(int index)
        {
            switch (index)
            {
                case 0:
                    return Loc.Get("drift.q1.body");
                case 1:
                    return Loc.Format("drift.q2.body", GivenTemperature.ToString("0"));
                default:
                    return Loc.Get("drift.q3.body");
            }
        }

        static string Title(int index)
        {
            switch (index)
            {
                case 0: return Loc.Get("drift.q1.title");
                case 1: return Loc.Get("drift.q2.title");
                default: return Loc.Get("drift.q3.title");
            }
        }
    }
}
