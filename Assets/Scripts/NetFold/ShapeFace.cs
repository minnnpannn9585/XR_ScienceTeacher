using UnityEngine;

public class ShapeFace : MonoBehaviour, ISelectable
{
    public int FaceIndex;
    public string FaceName;
    public Vector3 FoldedLocalPos;
    public Quaternion FoldedLocalRot;
    public Vector3 UnfoldedLocalPos;
    public Quaternion UnfoldedLocalRot;
    public bool HasHinge;
    public int HingeParent = -1;
    public Vector3 HingePoint;
    public Vector3 HingeAxis = Vector3.right;
    public float UnfoldAngle;
    public int Depth;
    public MeshRenderer MeshRenderer;
    public MeshFilter MeshFilter;
    public bool IsSelected { get; private set; }

    public void SetHinge(int parent, Vector3 point, Vector3 axis, float angle, int depth)
    {
        HasHinge = true;
        HingeParent = parent;
        HingePoint = point;
        HingeAxis = axis.sqrMagnitude < 1e-8f ? Vector3.right : axis.normalized;
        UnfoldAngle = angle;
        Depth = depth;
    }

    Material _base;
    Material _highlight;

    public void CaptureFolded()
    {
        FoldedLocalPos = transform.localPosition;
        FoldedLocalRot = transform.localRotation;
    }

    public void ApplyFoldedImmediate()
    {
        transform.localPosition = FoldedLocalPos;
        transform.localRotation = FoldedLocalRot;
    }

    public void ApplyUnfoldedImmediate()
    {
        transform.localPosition = UnfoldedLocalPos;
        transform.localRotation = UnfoldedLocalRot;
    }

    public void BindMaterials(Material baseMat, Material highlightMat)
    {
        _base = baseMat;
        _highlight = highlightMat;
        MeshRenderer = GetComponent<MeshRenderer>();
        MeshFilter = GetComponent<MeshFilter>();
        if (MeshRenderer != null)
        {
            MeshRenderer.sharedMaterial = _base;
        }
    }

    public void OnSelect()
    {
        IsSelected = true;
        if (MeshRenderer != null && _highlight != null)
        {
            MeshRenderer.sharedMaterial = _highlight;
        }

        transform.localScale = Vector3.one * 1.03f;
    }

    public void OnDeselect()
    {
        IsSelected = false;
        if (MeshRenderer != null && _base != null)
        {
            MeshRenderer.sharedMaterial = _base;
        }

        transform.localScale = Vector3.one;
    }
}
