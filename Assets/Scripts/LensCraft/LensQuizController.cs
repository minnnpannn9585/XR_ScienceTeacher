using UnityEngine;

public class LensQuizController : MonoBehaviour
{
    OpticalBenchController _bench;
    StarRatingController _stars;
    int _index;
    bool _running;
    bool _waiting;
    int _correct;
    float _answerCm;
    readonly float[] _screenChoices = { 0.18f, 0.26f, 0.42f };
    readonly float[] _answerChoices = { 20f, 26.7f, 40f, 64f };
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

    public void Pick(int choice)
    {
        if (!_running || _waiting)
        {
            return;
        }

        if (choice == _correct)
        {
            _stars.RegisterSuccess();
            FeedbackService.Instance?.Success(_bench.transform.position + Vector3.up * 0.2f);
            if (_index == 0)
            {
                _bench.ScreenDistance = _screenChoices[choice];
                _bench.ApplyPlacement();
                _bench.ScreenPlate.Flash();
                _bench.Status = Loc.Get("lens.sharp");
            }
            else if (_index == 1)
            {
                _bench.Status = Loc.Format("lens.correct.nature", _bench.Imaging.Nature);
            }
            else
            {
                _bench.Status = Loc.Format("lens.calc.ok", _answerCm.ToString("0.0"));
                Finish();
                return;
            }

            _bench.UI.ClearChoices();
            Advance();
        }
        else
        {
            _stars.RegisterRetry();
            _bench.UI.MarkChoice(choice);
            FeedbackService.Instance?.Error(_bench.transform.position + Vector3.up * 0.2f, _bench.transform);
            _bench.Status = _index == 0
                ? Loc.Get("lens.q1.again")
                : _index == 1
                    ? Loc.Format("lens.wrong.rule", _bench.Imaging.Rule)
                    : Loc.Get("lens.calc.again");
        }
    }

    void Show(int index)
    {
        _index = index;
        _waiting = false;
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
                _bench.LockScreen = true;
                _correct = 1;
                _bench.UI.ShowChoices(new[]
                {
                    Loc.Format("lens.choice.distance", 18),
                    Loc.Format("lens.choice.distance", 26),
                    Loc.Format("lens.choice.distance", 42)
                }, Pick);
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
                _correct = 1;
                _bench.UI.ShowChoices(new[]
                {
                    Loc.Format("lens.choice.distance", _answerChoices[0].ToString("0")),
                    Loc.Format("lens.choice.distance", _answerChoices[1].ToString("0.0")),
                    Loc.Format("lens.choice.distance", _answerChoices[2].ToString("0")),
                    Loc.Format("lens.choice.distance", _answerChoices[3].ToString("0"))
                }, Pick);
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
