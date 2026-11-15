using UnityEngine;
using UnityEngine.UI;

public class MissEdgeGraphic : MaskableGraphic
{
    public float width = 0.1f;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        float x = rect.width * width;
        float y = rect.height * width;
        Vector2[] outer = { new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin) };
        Vector2[] inner = { outer[0] + new Vector2(x, y), outer[1] + new Vector2(x, -y),
            outer[2] + new Vector2(-x, -y), outer[3] + new Vector2(-x, y) };
        Color transparent = color;
        transparent.a = 0f;
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            int start = mesh.currentVertCount;
            mesh.AddVert(outer[i], color, Vector2.zero);
            mesh.AddVert(outer[next], color, Vector2.zero);
            mesh.AddVert(inner[next], transparent, Vector2.zero);
            mesh.AddVert(inner[i], transparent, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
