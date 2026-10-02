public static class ShapeCatalog
{
    public static string DisplayName(ShapeType type)
    {
        switch (type)
        {
            case ShapeType.Cube:
                return "正方体";
            case ShapeType.Cylinder:
                return "圆柱";
            case ShapeType.Cone:
                return "圆锥";
            case ShapeType.TriangularPrism:
                return "三棱柱";
            default:
                return type.ToString();
        }
    }

    public static void Stats(ShapeType type, out int faces, out int edges, out int vertices)
    {
        switch (type)
        {
            case ShapeType.Cube:
                faces = 6;
                edges = 12;
                vertices = 8;
                break;
            case ShapeType.Cylinder:
                faces = 3;
                edges = 2;
                vertices = 0;
                break;
            case ShapeType.Cone:
                faces = 2;
                edges = 1;
                vertices = 1;
                break;
            case ShapeType.TriangularPrism:
                faces = 5;
                edges = 9;
                vertices = 6;
                break;
            default:
                faces = edges = vertices = 0;
                break;
        }
    }
}
