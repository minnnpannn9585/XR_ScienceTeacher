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
        Set(FacesText, Loc.Format("netfold.faces", shape.Faces));
        Set(EdgesText, Loc.Format("netfold.edges", shape.Edges));
        Set(VerticesText, Loc.Format("netfold.verts", shape.Vertices));
    }

    public void SetView(string view)
    {
        Set(ViewText, Loc.Format("netfold.view", view));
    }

    public void SetSectionEdges(int count)
    {
        Set(SectionText, count > 0 ? Loc.Format("netfold.section", count) : Loc.Get("netfold.section.empty"));
    }

    public void SetProjection(bool front, bool top, bool side)
    {
        Set(ProjectionText, Loc.Format("netfold.rays", On(front), On(top), On(side)));
    }

    public void Clear()
    {
        Set(TitleText, Loc.Get("netfold.none"));
        Set(FacesText, Loc.Get("netfold.faces.empty"));
        Set(EdgesText, Loc.Get("netfold.edges.empty"));
        Set(VerticesText, Loc.Get("netfold.verts.empty"));
        Set(ViewText, Loc.Format("netfold.view", Loc.Get("view.learn")));
        Set(SectionText, Loc.Get("netfold.section.empty"));
        Set(ProjectionText, Loc.Get("netfold.rays.off"));
    }

    static string On(bool v) => v ? Loc.Get("common.on") : Loc.Get("common.off");

    static void Set(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
