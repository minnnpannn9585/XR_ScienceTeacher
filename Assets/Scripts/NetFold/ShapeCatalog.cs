public static class ShapeCatalog
{
    public static string DisplayName(ShapeType type)
    {
        switch (type)
        {
            case ShapeType.Cube:
                return Loc.Get("shape.cube");
            case ShapeType.Cylinder:
                return Loc.Get("shape.cylinder");
            case ShapeType.Cone:
                return Loc.Get("shape.cone");
            case ShapeType.TriangularPrism:
                return Loc.Get("shape.prism");
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
