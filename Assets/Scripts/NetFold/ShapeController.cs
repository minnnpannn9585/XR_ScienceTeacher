using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ShapeController : MonoBehaviour, IInteractable
{
    public ShapeType Type { get; private set; }
    public bool IsSelected { get; private set; }
    public bool CanDrag { get; set; } = true;
    public bool AllowIdleSpin = true;
    public float IdleSpinSpeed = 18f;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    public int Faces { get; private set; }
    public int Edges { get; private set; }
    public int Vertices { get; private set; }
    public readonly List<ShapeFace> FaceList = new List<ShapeFace>();

    Vector3 _baseScale = Vector3.one;
    Quaternion _baseRotation = Quaternion.identity;
    Vector3 _basePosition;
    bool _userOrbit;
    Material _shared;
    Color _baseColor;

    public void Initialize(ShapeType type, Material shared, Material highlight)
    {
        Type = type;
        ShapeCatalog.Stats(type, out int f, out int e, out int v);
        Faces = f;
        Edges = e;
        Vertices = v;
        _shared = shared;
        _baseColor = shared.HasProperty("_BaseColor") ? shared.GetColor("_BaseColor") : shared.color;
        _baseScale = transform.localScale;
        _baseRotation = transform.localRotation;
        _basePosition = transform.localPosition;
        FaceList.Clear();
        GetComponentsInChildren(true, FaceList);
    }

    void Update()
    {
        if (AllowIdleSpin && !IsSelected && !_userOrbit)
        {
            transform.Rotate(Vector3.up, IdleSpinSpeed * Time.deltaTime, Space.World);
        }
    }

    public void OnSelect()
    {
        IsSelected = true;
        DOTween.Kill(transform);
        transform.DOScale(_baseScale * 1.05f, 0.18f).SetEase(Ease.OutBack);
        PulseEmission(true);
        FeedbackService.Instance?.Click();
    }

    public void OnDeselect()
    {
        IsSelected = false;
        DOTween.Kill(transform);
        transform.DOScale(_baseScale, 0.16f).SetEase(Ease.OutCubic);
        PulseEmission(false);
        ClearFaceHighlight();
    }

    public void OnDragStart(Vector3 worldPoint, Ray pointerRay)
    {
        _userOrbit = true;
    }

    public void OnDrag(Vector3 worldPoint, Ray pointerRay)
    {
        Vector2 delta = Vector2.zero;
        if (InputAdapter.Instance != null && InputAdapter.Instance.Provider != null)
        {
            delta = InputAdapter.Instance.Provider.LookDelta;
            if (delta.sqrMagnitude < 0.01f)
            {
                delta = new Vector2(pointerRay.direction.x, pointerRay.direction.y) * 8f;
            }
        }

        Rotate(delta * 0.18f);
    }

    public void OnDragEnd()
    {
        _userOrbit = false;
    }

    public void Rotate(Vector2 delta)
    {
        transform.Rotate(Vector3.up, -delta.x, Space.World);
        transform.Rotate(Vector3.right, delta.y, Space.World);
    }

    public void Scale(float delta)
    {
        float current = transform.localScale.x / Mathf.Max(0.0001f, _baseScale.x);
        float next = Mathf.Clamp(current + delta, 0.45f, 2.4f);
        transform.localScale = _baseScale * next;
    }

    public void ResetPose()
    {
        DOTween.Kill(transform);
        transform.DOLocalMove(_basePosition, 0.35f).SetEase(Ease.OutCubic);
        transform.DOLocalRotate(_baseRotation.eulerAngles, 0.35f).SetEase(Ease.OutCubic);
        transform.DOScale(_baseScale, 0.35f).SetEase(Ease.OutCubic);
        ClearFaceHighlight();
    }

    public void SetIdleSpin(bool on)
    {
        AllowIdleSpin = on;
    }

    public void HighlightFace(int index)
    {
        for (int i = 0; i < FaceList.Count; i++)
        {
            if (FaceList[i] == null)
            {
                continue;
            }

            if (FaceList[i].FaceIndex == index)
            {
                FaceList[i].OnSelect();
            }
            else
            {
                FaceList[i].OnDeselect();
            }
        }
    }

    public void ClearFaceHighlight()
    {
        for (int i = 0; i < FaceList.Count; i++)
        {
            if (FaceList[i] != null)
            {
                FaceList[i].OnDeselect();
            }
        }
    }

    public ShapeFace FaceAt(Transform t)
    {
        return t != null ? t.GetComponentInParent<ShapeFace>() : null;
    }

    void PulseEmission(bool on)
    {
        if (_shared == null)
        {
            return;
        }

        Color e = on ? NetFoldTheme.Accent * 1.4f : NetFoldTheme.EdgeGlow * 0.35f;
        UrpMaterialUtil.SetEmission(_shared, e);
        UrpMaterialUtil.SetColor(_shared, on ? Color.Lerp(_baseColor, Color.white, 0.18f) : _baseColor);
    }
}
