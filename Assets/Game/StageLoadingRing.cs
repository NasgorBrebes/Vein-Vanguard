using UnityEngine;
using UnityEngine.UI;

namespace VeinVanguard
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StageLoadingRing : MaskableGraphic
    {
        StageLoadingRing() => useLegacyMeshGeneration = false;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * .46f;
            float thickness = radius * .23f;
            var track = new Color32(205, 221, 231, 255);
            Arc(mesh, rect.center, radius, thickness * .63f, 0, 360, track);
            Segment(mesh, rect.center, radius, thickness, -78, 106, color);
            Segment(mesh, rect.center, radius, thickness, 45, 90, color);
            Segment(mesh, rect.center, radius, thickness, 152, 51, color);
            Segment(mesh, rect.center, radius, thickness, 220, 47, color);
        }

        static void Segment(VertexHelper mesh, Vector2 center, float radius, float thickness, float start, float sweep, Color32 tint)
        {
            Arc(mesh, center, radius, thickness, start, sweep, tint);
            float middle = radius - thickness * .5f;
            Arc(mesh, center + Direction(start) * middle, thickness * .5f, thickness * .5f, 0, 360, tint);
            Arc(mesh, center + Direction(start + sweep) * middle, thickness * .5f, thickness * .5f, 0, 360, tint);
        }

        static void Arc(VertexHelper mesh, Vector2 center, float radius, float thickness, float start, float sweep, Color32 tint)
        {
            float inner = radius - thickness;
            int steps = Mathf.CeilToInt(sweep / (inner == 0 ? 30 : 4));
            int first = mesh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                var direction = Direction(start + sweep * i / steps);
                mesh.AddVert(center + direction * radius, tint, Vector2.zero);
                mesh.AddVert(center + direction * inner, tint, Vector2.zero);
                if (i == 0) continue;
                int current = first + i * 2;
                mesh.AddTriangle(current - 2, current - 1, current);
                mesh.AddTriangle(current, current - 1, current + 1);
            }
        }

        static Vector2 Direction(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
