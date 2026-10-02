using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ChallengeController : MonoBehaviour
{
    public int QuestionIndex { get; private set; }
    public bool IsRunning { get; private set; }
    public StarRatingController Stars { get; private set; }

    public event Action<int, string> QuestionChanged;
    public event Action<bool, string> Answered;
    public event Action<int, string> Completed;

    Transform _stage;
    readonly List<GameObject> _spawned = new List<GameObject>();
    ShapeType _q1Answer = ShapeType.Cube;
    int _q2Answer = 0;
    int _q3Answer = 3;
    bool _waiting;

    public void Bind(StarRatingController stars, Transform stage)
    {
        Stars = stars;
        _stage = stage;
    }

    public void Begin()
    {
        IsRunning = true;
        QuestionIndex = 0;
        Stars.ResetRound();
        ShowQuestion(0);
    }

    public void Stop()
    {
        IsRunning = false;
        ClearSpawned();
    }

    public void UseHint()
    {
        if (!IsRunning)
        {
            return;
        }

        Stars.RegisterHint();
        string hint;
        switch (QuestionIndex)
        {
            case 0:
                hint = "提示：三个视图都是正方形，只有正方体同时满足。";
                HighlightCorrect();
                break;
            case 1:
                hint = "提示：俯视图是三角形的，对应三棱柱。";
                HighlightCorrect();
                break;
            default:
                hint = "提示：斜切正方体时，截面最多可以是六边形。";
                HighlightCorrect();
                break;
        }

        Answered?.Invoke(false, hint);
    }

    public void SubmitChoice(int index)
    {
        if (!IsRunning || _waiting)
        {
            return;
        }

        bool ok = false;
        switch (QuestionIndex)
        {
            case 0:
                ok = index == (int)_q1Answer;
                break;
            case 1:
                ok = index == _q2Answer;
                break;
            case 2:
                ok = index == _q3Answer;
                break;
        }

        if (ok)
        {
            Stars.RegisterSuccess();
            FeedbackService.Instance?.Success(_stage.position + Vector3.up * 0.2f);
            PlayCorrectFx();
            Answered?.Invoke(true, "回答正确！");
            _waiting = true;
            DOVirtual.DelayedCall(1.05f, NextOrFinish);
        }
        else
        {
            Stars.RegisterRetry();
            FeedbackService.Instance?.Error(_stage.position + Vector3.up * 0.2f, _stage);
            MarkWrong(index);
            Answered?.Invoke(false, "再想一想，错误的视图已用红色标出。");
        }
    }

    void NextOrFinish()
    {
        _waiting = false;
        if (QuestionIndex >= 2)
        {
            IsRunning = false;
            ClearSpawned();
            Completed?.Invoke(Stars.EvaluateStars(), Stars.EvaluateReason());
            return;
        }

        QuestionIndex++;
        ShowQuestion(QuestionIndex);
    }

    void ShowQuestion(int index)
    {
        ClearSpawned();
        switch (index)
        {
            case 0:
                QuestionChanged?.Invoke(1, "第 1 题  根据三视图选择几何体");
                BuildQuestion1();
                break;
            case 1:
                QuestionChanged?.Invoke(2, "第 2 题  根据几何体选择三视图");
                BuildQuestion2();
                break;
            default:
                QuestionChanged?.Invoke(3, "第 3 题  判断截面形状");
                BuildQuestion3();
                break;
        }
    }

    void BuildQuestion1()
    {
        CreateViewBoard(ShapeType.Cube, ViewKind.Front, "主视图", new Vector3(-0.36f, 0.2f, 0.28f));
        CreateViewBoard(ShapeType.Cube, ViewKind.Top, "俯视图", new Vector3(0f, 0.2f, 0.28f));
        CreateViewBoard(ShapeType.Cube, ViewKind.Side, "左视图", new Vector3(0.36f, 0.2f, 0.28f));
        SpawnChoiceShape(ShapeType.Cube, new Vector3(-0.38f, 0f, -0.16f), 0);
        SpawnChoiceShape(ShapeType.Cylinder, new Vector3(0f, 0f, -0.16f), 1);
        SpawnChoiceShape(ShapeType.Cone, new Vector3(0.38f, 0f, -0.16f), 2);
    }

    void BuildQuestion2()
    {
        var shape = GeometryFactory.Create(ShapeType.TriangularPrism, _stage, new Vector3(0f, 0f, -0.16f), 0.34f, false);
        shape.AllowIdleSpin = true;
        _spawned.Add(shape.gameObject);
        AddCaption(shape.transform, "题目几何体", new Vector3(0f, 0.42f, 0f), true);
        CreateViewSet(0, ShapeType.TriangularPrism, "选项 A", new Vector3(-0.42f, 0.2f, 0.3f));
        CreateViewSet(1, ShapeType.Cube, "选项 B", new Vector3(0f, 0.2f, 0.3f));
        CreateViewSet(2, ShapeType.Cylinder, "选项 C", new Vector3(0.42f, 0.2f, 0.3f));
    }

    void BuildQuestion3()
    {
        var cube = GeometryFactory.Create(ShapeType.Cube, _stage, new Vector3(0f, 0f, -0.12f), 0.36f, false);
        cube.AllowIdleSpin = false;
        cube.transform.localRotation = Quaternion.Euler(18f, 30f, 0f);
        _spawned.Add(cube.gameObject);
        AddCaption(cube.transform, "斜切正方体", new Vector3(0f, 0.48f, 0f), true);
        var plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "CutPlane";
        plane.transform.SetParent(_stage, false);
        plane.transform.localPosition = new Vector3(0f, 0.2f, -0.12f);
        plane.transform.localRotation = Quaternion.Euler(28f, 35f, 12f);
        plane.transform.localScale = new Vector3(0.52f, 0.012f, 0.52f);
        plane.GetComponent<MeshRenderer>().sharedMaterial = BoardMat(new Color(1f, 0.82f, 0.28f, 0.85f), true);
        UnityEngine.Object.Destroy(plane.GetComponent<Collider>());
        _spawned.Add(plane);
        string[] labels = { "三角形", "矩形", "五边形", "六边形" };
        Mesh[] shapes =
        {
            SectionMesh(3),
            SectionMesh(4),
            SectionMesh(5),
            SectionMesh(6)
        };
        for (int i = 0; i < labels.Length; i++)
        {
            float x = (i - 1.5f) * 0.3f;
            var card = CreateBoard(labels[i], new Vector3(x, 0.2f, 0.32f), new Vector2(0.26f, 0.34f), true, i);
            AddSilhouette(card.transform, shapes[i], new Vector3(0f, 0.04f, 0.012f), 1.2f, new Color(1f, 0.86f, 0.35f, 1f));
            AddCaption(card.transform, labels[i], new Vector3(0f, -0.11f, 0.02f), false);
        }
    }

    void CreateViewBoard(ShapeType type, ViewKind kind, string caption, Vector3 pos)
    {
        var card = CreateBoard(caption, pos, new Vector2(0.28f, 0.34f), false, -1);
        AddSilhouette(card.transform, ViewSilhouette.Create(type, kind), new Vector3(0f, 0.04f, 0.012f), 1.15f, ColorOf(kind));
        AddCaption(card.transform, caption, new Vector3(0f, -0.11f, 0.02f), false);
    }

    void CreateViewSet(int index, ShapeType type, string caption, Vector3 pos)
    {
        var card = CreateBoard(caption, pos, new Vector2(0.36f, 0.34f), true, index);
        AddSilhouette(card.transform, ViewSilhouette.Create(type, ViewKind.Front), new Vector3(-0.11f, 0.04f, 0.012f), 0.62f, NetFoldTheme.FrontView);
        AddSilhouette(card.transform, ViewSilhouette.Create(type, ViewKind.Top), new Vector3(0f, 0.04f, 0.012f), 0.62f, NetFoldTheme.TopView);
        AddSilhouette(card.transform, ViewSilhouette.Create(type, ViewKind.Side), new Vector3(0.11f, 0.04f, 0.012f), 0.62f, NetFoldTheme.SideView);
        AddCaption(card.transform, caption, new Vector3(0f, -0.11f, 0.02f), false);
    }

    GameObject CreateBoard(string name, Vector3 pos, Vector2 size, bool choice, int index)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_stage, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = FacePlayer();
        var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plate.name = "Plate";
        plate.transform.SetParent(go.transform, false);
        plate.transform.localScale = new Vector3(size.x, size.y, 1f);
        plate.GetComponent<MeshRenderer>().sharedMaterial = BoardMat(new Color(0.07f, 0.13f, 0.24f, 1f), false);
        if (choice)
        {
            var pick = go.AddComponent<ChallengeChoice>();
            pick.Index = index;
            pick.Owner = this;
        }

        _spawned.Add(go);
        return go;
    }

    void AddSilhouette(Transform parent, Mesh mesh, Vector3 localPos, float scale, Color color)
    {
        var go = new GameObject("Picture");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = BoardMat(color, false);
    }

    void SpawnChoiceShape(ShapeType type, Vector3 pos, int index)
    {
        var shape = GeometryFactory.Create(type, _stage, pos, 0.3f);
        shape.AllowIdleSpin = true;
        var choice = shape.gameObject.AddComponent<ChallengeChoice>();
        choice.Index = index;
        choice.Owner = this;
        choice.HostShape = shape;
        _spawned.Add(shape.gameObject);
        AddCaption(shape.transform, ShapeCatalog.DisplayName(type), new Vector3(0f, 0.4f, 0f), true);
    }

    void AddCaption(Transform parent, string text, Vector3 localPos, bool faceCamera)
    {
        var canvas = UiFactory.CreateWorld("Caption", parent, parent.TransformPoint(localPos), new Vector2(260f, 72f), Vector3.zero);
        canvas.transform.localPosition = localPos;
        canvas.transform.localRotation = Quaternion.identity;
        canvas.transform.localScale = Vector3.one * 0.00085f;
        var label = UiFactory.Label(canvas.transform, "Text", text, 40, TMPro.TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        label.color = Color.white;
        if (faceCamera)
        {
            canvas.gameObject.AddComponent<FaceCamera>();
        }
    }

    Quaternion FacePlayer()
    {
        Camera cam = Camera.main;
        if (cam == null || _stage == null)
        {
            return Quaternion.Euler(0f, 180f, 0f);
        }

        Vector3 to = cam.transform.position - _stage.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f)
        {
            return Quaternion.Euler(0f, 180f, 0f);
        }

        return Quaternion.LookRotation(to.normalized, Vector3.up);
    }

    static Color ColorOf(ViewKind kind)
    {
        switch (kind)
        {
            case ViewKind.Front: return NetFoldTheme.FrontView;
            case ViewKind.Top: return NetFoldTheme.TopView;
            default: return NetFoldTheme.SideView;
        }
    }

    static Material BoardMat(Color color, bool transparent)
    {
        Color solid = new Color(color.r, color.g, color.b, transparent ? color.a : 1f);
        var mat = UrpMaterialUtil.CreateLit(solid, transparent, 0.02f, 0.28f, true, solid * 0.45f);
        if (mat.HasProperty("_Cull"))
        {
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        return mat;
    }

    static Mesh SectionMesh(int sides)
    {
        if (sides == 4)
        {
            return RectMesh(0.2f, 0.12f);
        }

        float radius = 0.1f;
        var verts = new Vector3[sides + 1];
        var tris = new int[sides * 6];
        verts[0] = Vector3.zero;
        float start = -Mathf.PI * 0.5f;
        for (int i = 0; i < sides; i++)
        {
            float a = start + i / (float)sides * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }

        for (int i = 0; i < sides; i++)
        {
            int a = i + 1;
            int b = i == sides - 1 ? 1 : i + 2;
            int t = i * 6;
            tris[t] = 0;
            tris[t + 1] = a;
            tris[t + 2] = b;
            tris[t + 3] = 0;
            tris[t + 4] = b;
            tris[t + 5] = a;
        }

        var mesh = new Mesh { name = "Section" + sides, vertices = verts, triangles = tris };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh RectMesh(float w, float h)
    {
        float hw = w * 0.5f;
        float hh = h * 0.5f;
        var mesh = new Mesh { name = "Rect" };
        mesh.vertices = new[]
        {
            new Vector3(-hw, -hh, 0f),
            new Vector3(hw, -hh, 0f),
            new Vector3(hw, hh, 0f),
            new Vector3(-hw, hh, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 2, 1, 0, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void PlayCorrectFx()
    {
        if (QuestionIndex == 0)
        {
            var grow = GeometryFactory.Create(ShapeType.Cube, _stage, new Vector3(0f, 0.02f, 0.48f), 0.08f, false);
            grow.AllowIdleSpin = false;
            _spawned.Add(grow.gameObject);
            grow.transform.DOLocalMove(Vector3.zero, 0.7f).SetEase(Ease.OutCubic);
            grow.transform.DOScale(1.4f, 0.7f).SetEase(Ease.OutBack);
        }
    }

    void MarkWrong(int index)
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            var choice = _spawned[i].GetComponent<ChallengeChoice>();
            if (choice != null && choice.Index == index)
            {
                choice.SetError();
            }
        }
    }

    void HighlightCorrect()
    {
        int correct = QuestionIndex == 0 ? (int)_q1Answer : QuestionIndex == 1 ? _q2Answer : _q3Answer;
        for (int i = 0; i < _spawned.Count; i++)
        {
            var choice = _spawned[i].GetComponent<ChallengeChoice>();
            if (choice != null && choice.Index == correct)
            {
                choice.transform.DOPunchScale(Vector3.one * 0.12f, 0.35f, 6, 0.5f);
            }
        }
    }

    void ClearSpawned()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
            {
                DOTween.Kill(_spawned[i].transform);
                Destroy(_spawned[i]);
            }
        }

        _spawned.Clear();
    }
}

public class ChallengeChoice : MonoBehaviour, IInteractable
{
    public int Index;
    public ChallengeController Owner;
    public ShapeController HostShape;
    public bool IsSelected { get; private set; }
    public bool CanDrag => false;
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;

    public void OnSelect()
    {
        IsSelected = true;
        Owner?.SubmitChoice(Index);
    }

    public void OnDeselect() => IsSelected = false;
    public void OnDragStart(Vector3 worldPoint, Ray pointerRay) { }
    public void OnDrag(Vector3 worldPoint, Ray pointerRay) { }
    public void OnDragEnd() { }
    public void Rotate(Vector2 delta) => HostShape?.Rotate(delta);
    public void Scale(float delta) => HostShape?.Scale(delta);

    public void SetError()
    {
        var rends = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < rends.Length; i++)
        {
            UrpMaterialUtil.SetColor(rends[i].material, NetFoldTheme.Error);
            UrpMaterialUtil.SetEmission(rends[i].material, NetFoldTheme.Error);
        }
    }
}
