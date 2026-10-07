using System.Globalization;
using UnityEngine;

public class LensQuizController : MonoBehaviour
{
    OpticalBenchController _bench;
    StarRatingController _stars;
    int _index;
    bool _running;
    bool _waiting;
    float _hold;
    int _correct;
    float _answerCm;
    bool _notedBlur;
    Coroutine _advance;

    public bool Running => _running;

    public void Begin(OpticalBenchController bench, StarRatingController stars)
    {
        CancelAdvance();
        _bench = bench;
        _stars = stars;
        _running = true;
        _waiting = false;
        _index = 0;
        _stars.ResetRound();
        Show(0);
    }

    public void Stop()
    {
        CancelAdvance();
        _running = false;
        if (_bench != null && _bench.UI != null)
        {
            _bench.UI.ClearChoices();
            _bench.UI.SetAnswerVisible(false);
        }
    }

    public void Tick()
    {
        if (!_running || _waiting || _index != 0)
        {
            return;
        }

        if (_bench.Sharpness > 0.78f)
        {
            _hold += Time.deltaTime;
            _bench.Status = "光屏接近清晰位置";
            if (_hold > 0.45f)
            {
                _bench.ScreenPlate.Flash();
                FeedbackService.Instance?.Success(_bench.ScreenPlate.transform.position);
                _bench.Status = "光屏上出现清晰的倒立实像";
                Advance();
            }
        }
        else
        {
            _hold = 0f;
            if (_bench.Sharpness < 0.35f && !_notedBlur)
            {
                _notedBlur = true;
                _bench.Status = "光屏位置不对，像不清晰";
            }
        }
    }

    public void Hint()
    {
        if (!_running || _waiting)
        {
            return;
        }

        _stars.RegisterHint();
        switch (_index)
        {
            case 0:
                _bench.Status = "清晰位置 v = uf/(u-f)，约 " + LensMath.Centimeters(_bench.Imaging.ImageDistance);
                break;
            case 1:
                _bench.Status = "提示：" + _bench.Imaging.Rule;
                break;
            default:
                _bench.Status = "1/v = 1/f - 1/u，v = uf/(u-f) ≈ " + _answerCm.ToString("0.0") + " cm";
                break;
        }
    }

    public void Submit()
    {
        if (!_running || _waiting || _index != 2 || _bench.UI == null)
        {
            return;
        }

        string text = _bench.UI.AnswerText.Trim();
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            && !float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            _bench.Status = "请输入像距，单位是厘米";
            return;
        }

        if (Mathf.Abs(value - _answerCm) <= Mathf.Max(1.2f, _answerCm * 0.08f))
        {
            FeedbackService.Instance?.Success(_bench.ScreenPlate.transform.position);
            _bench.Status = "计算正确，v = " + _answerCm.ToString("0.0") + " cm";
            _stars.RegisterSuccess();
            Finish();
        }
        else
        {
            _stars.RegisterRetry();
            FeedbackService.Instance?.Error(_bench.ScreenPlate.transform.position, _bench.ScreenPlate.transform);
            _bench.Status = "再算一次。公式：1/f = 1/u + 1/v，即 v = uf/(u-f)";
        }
    }

    public void Pick(int index)
    {
        if (!_running || _waiting || _index != 1)
        {
            return;
        }

        if (index == _correct)
        {
            FeedbackService.Instance?.Success(_bench.transform.position + Vector3.up * 0.2f);
            _bench.Status = "正确：" + _bench.Imaging.Nature;
            _bench.UI.ClearChoices();
            Advance();
        }
        else
        {
            _stars.RegisterRetry();
            _bench.UI.MarkChoice(index);
            FeedbackService.Instance?.Error(_bench.transform.position + Vector3.up * 0.2f, _bench.transform);
            _bench.Status = "不对。" + _bench.Imaging.Rule;
        }
    }

    void Show(int index)
    {
        _index = index;
        _waiting = false;
        _hold = 0f;
        _notedBlur = false;
        _bench.UI.ClearChoices();
        _bench.UI.SetAnswerVisible(false);
        _bench.LightOn = true;
        _bench.ShowRays = true;
        _bench.AutoScreen = false;
        _bench.LockFocal = true;
        switch (index)
        {
            case 0:
                _bench.UI.SetChallenge("挑战 1 / 3    找清晰像", "拖动光屏，让倒立实像变清晰。");
                _bench.ObjectDistance = 0.42f;
                _bench.Focal = 0.16f;
                _bench.ScreenDistance = 0.55f;
                _bench.LockCandle = true;
                _bench.LockScreen = false;
                _bench.Status = "拖动光屏，直到像清晰";
                break;
            case 1:
                _bench.UI.SetChallenge("挑战 2 / 3    判断像的性质", "根据物距和焦距选择像的性质。");
                _bench.ObjectDistance = 0.22f;
                _bench.Focal = 0.15f;
                _bench.LockCandle = true;
                _bench.LockScreen = true;
                _bench.AutoScreen = true;
                _correct = 2;
                _bench.UI.ShowChoices(new[] { "倒立缩小实像", "倒立等大实像", "倒立放大实像", "正立放大虚像" }, Pick);
                _bench.Status = "u = 22 cm，f = 15 cm";
                break;
            default:
                _bench.UI.SetChallenge("挑战 3 / 3    计算像距", "u = 40 cm，f = 16 cm。求像距 v（厘米），回车提交。");
                _bench.ObjectDistance = 0.4f;
                _bench.Focal = 0.16f;
                _bench.LockCandle = true;
                _bench.LockScreen = true;
                _bench.AutoScreen = true;
                _answerCm = 100f * (0.4f * 0.16f) / (0.4f - 0.16f);
                _bench.UI.SetAnswerVisible(true);
                _bench.Status = "输入 v 的厘米数";
                break;
        }

        _bench.ApplyPlacement();
    }

    void Advance()
    {
        _waiting = true;
        _advance = StartCoroutine(NextSoon());
    }

    System.Collections.IEnumerator NextSoon()
    {
        yield return new WaitForSeconds(0.9f);
        _advance = null;
        if (!_running)
        {
            yield break;
        }

        if (_index >= 2)
        {
            Finish();
            yield break;
        }

        Show(_index + 1);
    }

    void CancelAdvance()
    {
        if (_advance != null) StopCoroutine(_advance);
        _advance = null;
        _waiting = false;
    }

    void Finish()
    {
        _running = false;
        _waiting = false;
        _bench.UI.ClearChoices();
        _bench.UI.SetAnswerVisible(false);
        _stars.RegisterSuccess();
        if (_bench.UI.Result != null)
        {
            _bench.UI.Result.Show(_stars.EvaluateStars(), _stars.EvaluateReason());
        }
        else
        {
            _bench.UI.SetChallenge("挑战完成", _stars.EvaluateReason());
        }
    }
}
