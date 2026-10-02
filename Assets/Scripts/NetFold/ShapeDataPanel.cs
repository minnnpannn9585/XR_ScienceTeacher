using TMPro;
using UnityEngine;

public class ShapeDataPanel : MonoBehaviour
{
    public TMP_Text FacesText;
    public TMP_Text EdgesText;
    public TMP_Text VerticesText;
    public TMP_Text ViewText;
    public TMP_Text SectionText;
    public TMP_Text ProjectionText;
    public TMP_Text TitleText;

    public void Bind(TMP_Text faces, TMP_Text edges, TMP_Text vertices, TMP_Text view, TMP_Text section, TMP_Text projection, TMP_Text title)
    {
        FacesText = faces;
        EdgesText = edges;
        VerticesText = vertices;
        ViewText = view;
        SectionText = section;
        ProjectionText = projection;
        TitleText = title;
        Clear();
    }

    public void Show(ShapeController shape)
    {
        if (shape == null)
        {
            Clear();
            return;
        }

        Set(TitleText, ShapeCatalog.DisplayName(shape.Type));
        Set(FacesText, "面数  " + shape.Faces);
        Set(EdgesText, "棱数  " + shape.Edges);
        Set(VerticesText, "顶点数  " + shape.Vertices);
    }

    public void SetView(string view)
    {
        Set(ViewText, "当前视图  " + view);
    }

    public void SetSectionEdges(int count)
    {
        Set(SectionText, "截面边数  " + (count > 0 ? count.ToString() : "--"));
    }

    public void SetProjection(bool front, bool top, bool side)
    {
        Set(ProjectionText, "投影线  主" + On(front) + "  俯" + On(top) + "  左" + On(side));
    }

    public void Clear()
    {
        Set(TitleText, "未选择几何体");
        Set(FacesText, "面数  --");
        Set(EdgesText, "棱数  --");
        Set(VerticesText, "顶点数  --");
        Set(ViewText, "当前视图  学习");
        Set(SectionText, "截面边数  --");
        Set(ProjectionText, "投影线  关");
    }

    static string On(bool v) => v ? "开" : "关";

    static void Set(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
