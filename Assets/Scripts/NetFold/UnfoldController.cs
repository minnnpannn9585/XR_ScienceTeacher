using DG.Tweening;
using UnityEngine;

public class UnfoldController : MonoBehaviour
{
    public bool IsUnfolded { get; private set; }
    public ShapeController Target { get; private set; }

    Sequence _seq;

    public void Bind(ShapeController shape)
    {
        if (Target == shape)
        {
            return;
        }

        if (Target != null && IsUnfolded)
        {
            Fold(true);
        }

        Target = shape;
        IsUnfolded = false;
    }

    public void Unfold(bool instant = false)
    {
        if (Target == null)
        {
            return;
        }

        Kill();
        IsUnfolded = true;
        Target.SetIdleSpin(false);
        if (instant)
        {
            for (int i = 0; i < Target.FaceList.Count; i++)
            {
                Target.FaceList[i].ApplyUnfoldedImmediate();
            }

            return;
        }

        _seq = DOTween.Sequence();
        for (int i = 0; i < Target.FaceList.Count; i++)
        {
            ShapeFace face = Target.FaceList[i];
            float delay = i * 0.045f;
            var move = face.transform.DOLocalMove(face.UnfoldedLocalPos, 0.55f).SetEase(Ease.InOutCubic).SetDelay(delay);
            var rot = face.transform.DOLocalRotate(face.UnfoldedLocalRot.eulerAngles, 0.55f).SetEase(Ease.InOutCubic).SetDelay(delay);
            if (i == 0)
            {
                _seq.Append(move);
                _seq.Join(rot);
            }
            else
            {
                _seq.Join(move);
                _seq.Join(rot);
            }
        }

        _seq.OnComplete(() =>
        {
            if (FeedbackService.Instance != null)
            {
                FeedbackService.Instance.Burst(Target.transform.position + Vector3.up * 0.12f, NetFoldTheme.Accent, 40);
            }
        });
    }

    public void Fold(bool instant = false)
    {
        if (Target == null)
        {
            return;
        }

        Kill();
        IsUnfolded = false;
        if (instant)
        {
            for (int i = 0; i < Target.FaceList.Count; i++)
            {
                Target.FaceList[i].ApplyFoldedImmediate();
            }

            Target.SetIdleSpin(true);
            return;
        }

        _seq = DOTween.Sequence();
        for (int i = 0; i < Target.FaceList.Count; i++)
        {
            ShapeFace face = Target.FaceList[i];
            float delay = i * 0.03f;
            _seq.Join(face.transform.DOLocalMove(face.FoldedLocalPos, 0.5f).SetEase(Ease.InOutCubic).SetDelay(delay));
            _seq.Join(face.transform.DOLocalRotate(face.FoldedLocalRot.eulerAngles, 0.5f).SetEase(Ease.InOutCubic).SetDelay(delay));
        }

        _seq.OnComplete(() =>
        {
            Target.SetIdleSpin(true);
            FeedbackService.Instance?.Burst(Target.transform.position + Vector3.up * 0.2f, NetFoldTheme.Success, 28);
        });
    }

    public void Toggle()
    {
        if (IsUnfolded)
        {
            Fold();
        }
        else
        {
            Unfold();
        }
    }

    public bool TrySelectFace(Ray ray)
    {
        if (Target == null)
        {
            return false;
        }

        if (!Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            return false;
        }

        var face = hit.collider.GetComponentInParent<ShapeFace>();
        if (face == null || face.transform.parent != Target.transform)
        {
            return false;
        }

        Target.HighlightFace(face.FaceIndex);
        FeedbackService.Instance?.Click();
        return true;
    }

    void Kill()
    {
        if (_seq != null)
        {
            _seq.Kill();
            _seq = null;
        }
    }
}
