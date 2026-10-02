using UnityEngine;

public class ShapeFace : MonoBehaviour, ISelectable
{
    public int FaceIndex;
    public string FaceName;
    public Vector3 FoldedLocalPos;
    public Quaternion FoldedLocalRot;
    public Vector3 UnfoldedLocalPos;
    public Quaternion UnfoldedLocalRot;
    public MeshRenderer MeshRenderer;
    public MeshFilter MeshFilter;
    public bool IsSelected { get; private set; }

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
