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
            _bench.Status = Loc.Get("lens.near.sharp");
            if (_hold > 0.45f)
            {
                _bench.ScreenPlate.Flash();
                FeedbackService.Instance?.Success(_bench.ScreenPlate.transform.position);
                _bench.Status = Loc.Get("lens.sharp");
                Advance();
            }
        }
        else
        {
            _hold = 0f;
            if (_bench.Sharpness < 0.35f && !_notedBlur)
            {
                _notedBlur = true;
                _bench.Status = Loc.Get("lens.blur");
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
                _bench.Status = Loc.Format("lens.hint.v", LensMath.Centimeters(_bench.Imaging.ImageDistance));
                break;
            case 1:
                _bench.Status = Loc.Format("lens.hint.rule", _bench.Imaging.Rule);
                break;
            default:
                _bench.Status = Loc.Format("lens.hint.calc", _answerCm.ToString("0.0"));
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
            _bench.Status = Loc.Get("lens.need.number");
            return;
        }

        if (Mathf.Abs(value - _answerCm) <= Mathf.Max(1.2f, _answerCm * 0.08f))
        {
            FeedbackService.Instance?.Success(_bench.ScreenPlate.transform.position);
            _bench.Status = Loc.Format("lens.calc.ok", _answerCm.ToString("0.0"));
            _stars.RegisterSuccess();
            Finish();
        }
        else
        {
            _stars.RegisterRetry();
            FeedbackService.Instance?.Error(_bench.ScreenPlate.transform.position, _bench.ScreenPlate.transform);
            _bench.Status = Loc.Get("lens.calc.again");
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
            _bench.Status = Loc.Format("lens.correct.nature", _bench.Imaging.Nature);
            _bench.UI.ClearChoices();
            Advance();
        }
        else
        {
            _stars.RegisterRetry();
            _bench.UI.MarkChoice(index);
            FeedbackService.Instance?.Error(_bench.transform.position + Vector3.up * 0.2f, _bench.transform);
            _bench.Status = Loc.Format("lens.wrong.rule", _bench.Imaging.Rule);
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
                _bench.UI.SetChallenge(Loc.Get("lens.q1.title"), Loc.Get("lens.q1.body"));
                _bench.ObjectDistance = 0.42f;
                _bench.Focal = 0.16f;
                _bench.ScreenDistance = 0.55f;
                _bench.LockCandle = true;
                _bench.LockScreen = false;
                _bench.Status = Loc.Get("lens.q1.status");
                break;
            case 1:
                _bench.UI.SetChallenge(Loc.Get("lens.q2.title"), Loc.Get("lens.q2.body"));
                _bench.ObjectDistance = 0.22f;
                _bench.Focal = 0.15f;
                _bench.LockCandle = true;
                _bench.LockScreen = true;
                _bench.AutoScreen = true;
                _correct = 2;
                _bench.UI.ShowChoices(new[] { Loc.Get("lens.choice.reduced"), Loc.Get("lens.choice.equal"), Loc.Get("lens.choice.magnified"), Loc.Get("lens.choice.virtual") }, Pick);
                _bench.Status = Loc.Get("lens.q2.status");
                break;
            default:
                _bench.UI.SetChallenge(Loc.Get("lens.q3.title"), Loc.Get("lens.q3.body"));
                _bench.ObjectDistance = 0.4f;
                _bench.Focal = 0.16f;
                _bench.LockCandle = true;
                _bench.LockScreen = true;
                _bench.AutoScreen = true;
                _answerCm = 100f * (0.4f * 0.16f) / (0.4f - 0.16f);
                _bench.UI.SetAnswerVisible(true);
                _bench.Status = Loc.Get("lens.q3.status");
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
            _bench.UI.SetChallenge(Loc.Get("common.done"), _stars.EvaluateReason());
        }
    }
}
