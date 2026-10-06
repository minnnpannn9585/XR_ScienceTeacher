using DG.Tweening;
using UnityEngine;

public class UnfoldController : MonoBehaviour
{
    public bool IsUnfolded { get; private set; }
    public ShapeController Target { get; private set; }

    Tween _tween;
    float _amount;

    public void Bind(ShapeController shape)
    {
        if (Target == shape)
        {
            return;
        }

        Kill();
        Target = shape;
        IsUnfolded = false;
        _amount = 0f;
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
            Apply(1f);
            return;
        }

        float from = _amount;
        _tween = DOVirtual.Float(from, 1f, 1.15f, Apply).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            FeedbackService.Instance?.Burst(Target.transform.position + Vector3.up * 0.12f, NetFoldTheme.Accent, 40);
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
            Apply(0f);
            Target.SetIdleSpin(Target.CanDrag);
            return;
        }

        float from = _amount;
        _tween = DOVirtual.Float(from, 0f, 0.95f, Apply).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            if (Target != null)
            {
                Target.SetIdleSpin(Target.CanDrag);
                FeedbackService.Instance?.Burst(Target.transform.position + Vector3.up * 0.2f, NetFoldTheme.Success, 28);
            }
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

    void Apply(float amount)
    {
        _amount = amount;
        if (Target == null)
        {
            return;
        }

        int count = Target.FaceList.Count;
        var pos = new Vector3[count];
        var rot = new Quaternion[count];
        var done = new bool[count];
        for (int i = 0; i < count; i++)
        {
            Eval(i, amount, pos, rot, done);
        }

        for (int i = 0; i < count; i++)
        {
            ShapeFace face = Target.FaceList[i];
            if (face == null)
            {
                continue;
            }

            face.transform.localPosition = pos[i];
            face.transform.localRotation = rot[i];
        }
    }

    void Eval(int index, float amount, Vector3[] pos, Quaternion[] rot, bool[] done)
    {
        if (index < 0 || index >= done.Length || done[index])
        {
            return;
        }

        done[index] = true;
        ShapeFace face = Target.FaceList[index];
        if (face == null || !face.HasHinge)
        {
            pos[index] = face != null ? face.FoldedLocalPos : Vector3.zero;
            rot[index] = face != null ? face.FoldedLocalRot : Quaternion.identity;
            return;
        }

        float t = HingeAmount(amount, face.Depth);
        Quaternion swingLocal = Quaternion.AngleAxis(face.UnfoldAngle * t, face.HingeAxis);
        if (face.HingeParent < 0)
        {
            Vector3 offset = face.FoldedLocalPos - face.HingePoint;
            pos[index] = face.HingePoint + swingLocal * offset;
            rot[index] = swingLocal * face.FoldedLocalRot;
            return;
        }

        int parentIndex = Find(face.HingeParent);
        if (parentIndex < 0)
        {
            pos[index] = face.FoldedLocalPos;
            rot[index] = face.FoldedLocalRot;
            return;
        }

        Eval(parentIndex, amount, pos, rot, done);
        ShapeFace parent = Target.FaceList[parentIndex];
        Quaternion delta = rot[parentIndex] * Quaternion.Inverse(parent.FoldedLocalRot);
        Vector3 hingeNow = pos[parentIndex] + delta * (face.HingePoint - parent.FoldedLocalPos);
        Vector3 axisNow = delta * face.HingeAxis;
        Vector3 attachedPos = pos[parentIndex] + delta * (face.FoldedLocalPos - parent.FoldedLocalPos);
        Quaternion attachedRot = delta * face.FoldedLocalRot;
        Quaternion swing = Quaternion.AngleAxis(face.UnfoldAngle * t, axisNow.sqrMagnitude < 1e-8f ? Vector3.right : axisNow.normalized);
        pos[index] = hingeNow + swing * (attachedPos - hingeNow);
        rot[index] = swing * attachedRot;
    }

    int Find(int faceIndex)
    {
        for (int i = 0; i < Target.FaceList.Count; i++)
        {
            if (Target.FaceList[i] != null && Target.FaceList[i].FaceIndex == faceIndex)
            {
                return i;
            }
        }

        return -1;
    }

    static float HingeAmount(float amount, int depth)
    {
        float delay = Mathf.Min(depth * 0.07f, 0.42f);
        float x = Mathf.Clamp01((amount - delay) / Mathf.Max(0.2f, 1f - delay));
        return x * x * (3f - 2f * x);
    }

    void Kill()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
    }
}
