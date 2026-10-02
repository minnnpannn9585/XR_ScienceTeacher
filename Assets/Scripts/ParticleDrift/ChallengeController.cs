using System;
using UnityEngine;

namespace ParticleDrift
{
    public class ChallengeController : MonoBehaviour
    {
        public int Index { get; private set; }
        public bool Running { get; private set; }
        public float GivenTemperature { get; private set; }
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

        public void Hint()
        {
            if (!Running || _stars == null)
            {
                return;
            }

            _stars.RegisterHint();
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

            correct = band == TemperatureController.BandFor(GivenTemperature);
            if (!correct)
            {
                _stars.RegisterRetry();
            }

            return true;
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
                message = "扩散时间 " + seconds.ToString("0.0") + " 秒，落在目标范围内";
                return true;
            }

            _stars.RegisterRetry();
            message = seconds > TargetMax ? "温度偏低，分子运动太慢" : "温度偏高，分子运动太快";
            return false;
        }

        public void Advance()
        {
            if (!Running)
            {
                return;
            }

            Index++;
            if (Index >= 3)
            {
                Running = false;
                _stars.RegisterSuccess();
                Finished?.Invoke(_stars.EvaluateStars(), _stars.EvaluateReason());
                return;
            }

            Publish();
        }

        public string HintText()
        {
            switch (Index)
            {
                case 0:
                    return "比较两杯变色速度。温度越高，扩散越快。";
                case 1:
                    return "水温很高选很快，中等水温选中等，接近冰水选很慢。";
                default:
                    return "目标是 5.5–8.5 秒。大约 45℃ 到 65℃ 会落在这个范围。";
            }
        }

        void Publish()
        {
            if (Index == 1 && !_q2Rolled)
            {
                _q2Rolled = true;
                float[] temps = { 90f, 48f, 8f };
                GivenTemperature = temps[UnityEngine.Random.Range(0, temps.Length)];
            }

            Presented?.Invoke(Index, Title(Index), Body(Index));
        }

        string Body(int index)
        {
            switch (index)
            {
                case 0:
                    return "冷水和热水已同时滴入品红。选出扩散更快的烧杯。";
                case 1:
                    return "当前水温 " + GivenTemperature.ToString("0") + " ℃。预测品红扩散到均匀的快慢。";
                default:
                    return "调节温度，使品红在 5.5–8.5 秒内扩散均匀，然后开始扩散。";
            }
        }

        static string Title(int index)
        {
            switch (index)
            {
                case 0: return "挑战 1 / 3    温度与扩散速度";
                case 1: return "挑战 2 / 3    预测扩散时间";
                default: return "挑战 3 / 3    调温达标";
            }
        }
    }
}
