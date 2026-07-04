using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HelixVortex.Rendering;

public static class MeshBuilder
{
    public static void CreateSphere(float radius, int latSegments, int lonSegments, Color color, out VertexPositionNormalColor[] vertices, out short[] indices)
    {
        List<VertexPositionNormalColor> vertexList = new List<VertexPositionNormalColor>();
        List<short> indexList = new List<short>();

        for (int lat = 0; lat <= latSegments; lat++)
         {
             float theta = lat * MathHelper.Pi / latSegments;
             float sinTheta = (float)Math.Sin(theta);
             float cosTheta = (float)Math.Cos(theta);

             for (int lon = 0; lon <= lonSegments; lon++)
             {
                 float phi = lon * MathHelper.TwoPi / lonSegments;
                 float sinPhi = (float)Math.Sin(phi);
                 float cosPhi = (float)Math.Cos(phi);

                 Vector3 normal = new Vector3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
                 Vector3 position = normal * radius;

                 vertexList.Add(new VertexPositionNormalColor(position, normal, color));
             }
         }

         for (int lat = 0; lat < latSegments; lat++)
         {
             for (int lon = 0; lon < lonSegments; lon++)
             {
                 int current = lat * (lonSegments + 1) + lon;
                 int next = current + 1;
                 int bottom = current + (lonSegments + 1);
                 int bottomNext = bottom + 1;

                 // Triangle 1
                 indexList.Add((short)current);
                 indexList.Add((short)bottom);
                 indexList.Add((short)next);

                 // Triangle 2
                 indexList.Add((short)next);
                 indexList.Add((short)bottom);
                 indexList.Add((short)bottomNext);
             }
         }

         vertices = vertexList.ToArray();
         indices = indexList.ToArray();
    }

    public static void CreateCylinder(float radius, float height, int segments, Color color, out VertexPositionNormalColor[] vertices, out short[] indices)
    {
        List<VertexPositionNormalColor> vertexList = new List<VertexPositionNormalColor>();
        List<short> indexList = new List<short>();

        float halfHeight = height / 2f;

        // Build cylinder sides
        for (int i = 0; i <= segments; i++)
        {
            float theta = i * MathHelper.TwoPi / segments;
            float cosTheta = (float)Math.Cos(theta);
            float sinTheta = (float)Math.Sin(theta);

            Vector3 normal = new Vector3(cosTheta, 0, sinTheta);
            Vector3 topPos = new Vector3(cosTheta * radius, halfHeight, sinTheta * radius);
            Vector3 bottomPos = new Vector3(cosTheta * radius, -halfHeight, sinTheta * radius);

            vertexList.Add(new VertexPositionNormalColor(topPos, normal, color));
            vertexList.Add(new VertexPositionNormalColor(bottomPos, normal, color));
        }

        for (int i = 0; i < segments; i++)
        {
            int topCurrent = i * 2;
            int bottomCurrent = topCurrent + 1;
            int topNext = (i + 1) * 2;
            int bottomNext = topNext + 1;

            // Triangle 1
            indexList.Add((short)topCurrent);
            indexList.Add((short)bottomCurrent);
            indexList.Add((short)topNext);

            // Triangle 2
            indexList.Add((short)topNext);
            indexList.Add((short)bottomCurrent);
            indexList.Add((short)bottomNext);
        }

        vertices = vertexList.ToArray();
        indices = indexList.ToArray();
    }

    public static void CreateSlice(float innerRadius, float outerRadius, float thickness, float startAngleRad, float endAngleRad, Color color, out VertexPositionNormalColor[] vertices, out short[] indices)
    {
        List<VertexPositionNormalColor> vertexList = new List<VertexPositionNormalColor>();
        List<short> indexList = new List<short>();

        float halfY = thickness / 2f;

        void AddQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 normal)
        {
            short baseIdx = (short)vertexList.Count;

            vertexList.Add(new VertexPositionNormalColor(p1, normal, color));
            vertexList.Add(new VertexPositionNormalColor(p2, normal, color));
            vertexList.Add(new VertexPositionNormalColor(p3, normal, color));
            vertexList.Add(new VertexPositionNormalColor(p4, normal, color));

            // Triangle 1
            indexList.Add(baseIdx);
            indexList.Add((short)(baseIdx + 1));
            indexList.Add((short)(baseIdx + 2));

            // Triangle 2
            indexList.Add(baseIdx);
            indexList.Add((short)(baseIdx + 2));
            indexList.Add((short)(baseIdx + 3));
        }

        float c0 = (float)Math.Cos(startAngleRad);
        float s0 = (float)Math.Sin(startAngleRad);
        float c1 = (float)Math.Cos(endAngleRad);
        float s1 = (float)Math.Sin(endAngleRad);

        // Corners
        Vector3 til = new Vector3(c0 * innerRadius, halfY, s0 * innerRadius);
        Vector3 tir = new Vector3(c1 * innerRadius, halfY, s1 * innerRadius);
        Vector3 tol = new Vector3(c0 * outerRadius, halfY, s0 * outerRadius);
        Vector3 tor = new Vector3(c1 * outerRadius, halfY, s1 * outerRadius);

        Vector3 bil = new Vector3(c0 * innerRadius, -halfY, s0 * innerRadius);
        Vector3 bir = new Vector3(c1 * innerRadius, -halfY, s1 * innerRadius);
        Vector3 bol = new Vector3(c0 * outerRadius, -halfY, s0 * outerRadius);
        Vector3 bor = new Vector3(c1 * outerRadius, -halfY, s1 * outerRadius);

        // 1. Bottom Face (note order changes to keep triangles facing outward/downward)
        // Wait, standard culling in MonoGame uses counter-clockwise.
        // Top Face (looking down from above): CCW is til -> tol -> tor -> tir -> til
        // Let's verify til, tol, tor, tir:
        // Left is startAngle (0), Right is endAngle (1). Outer is further, Inner is closer.
        // Looking from above:
        // til (inner, start) -> tir (inner, end) -> tor (outer, end) -> tol (outer, start)
        // This is a loop. Let's make sure our triangle vertices are CCW.
        // In AddQuad:
        // Triangle 1: p1 -> p2 -> p3
        // Triangle 2: p1 -> p3 -> p4
        // If we pass (til, tol, tor, tir, Vector3.Up) to AddQuad:
        // p1 = til (inner left)
        // p2 = tol (outer left)
        // p3 = tor (outer right)
        // p4 = tir (inner right)
        // Let's draw this:
        // outer left (tol) ----- outer right (tor)
        //     |                      |
        //     |                      |
        // inner left (til) ----- inner right (tir)
        // CCW path: til -> tol -> tor -> tir.
        // Triangle 1: til -> tol -> tor (CCW)
        // Triangle 2: til -> tor -> tir (CCW)
        // This is correct! So `AddQuad(til, tol, tor, tir, Vector3.Up)` is counter-clockwise and will render correctly with Back-Face Culling.
        
        // Let's check Bottom Face (looking up from below):
        // p1 = bil (inner left)
        // p2 = bir (inner right)
        // p3 = bor (outer right)
        // p4 = bol (outer left)
        // Looking from below, the vertices are:
        // inner left (bil) ----- inner right (bir)
        //     |                      |
        //     |                      |
        // outer left (bol) ----- outer right (bor)
        // CCW path: bil -> bir -> bor -> bol.
        // Triangle 1: bil -> bir -> bor (CCW)
        // Triangle 2: bil -> bor -> bol (CCW)
        // Yes, looking from below, bil -> bir is to the right, bir -> bor is down, bor -> bol is left. This is CCW!
        // So `AddQuad(bil, bir, bor, bol, Vector3.Down)` is perfect!

        float cm = (float)Math.Cos((startAngleRad + endAngleRad) / 2f);
        float sm = (float)Math.Sin((startAngleRad + endAngleRad) / 2f);

        // Let's write the Top/Bottom quads:
        AddQuad(til, tol, tor, tir, Vector3.Up);
        AddQuad(bil, bir, bor, bol, Vector3.Down);

        // 3. Left Wall (along startAngle c0, s0)
        Vector3 leftNormal = new Vector3(s0, 0, -c0);
        AddQuad(bil, til, tol, bol, leftNormal);

        // 4. Right Wall (along endAngle c1, s1)
        Vector3 rightNormal = new Vector3(-s1, 0, c1);
        AddQuad(bor, tor, tir, bir, rightNormal);

        // 5. Inner Curve (inner radius)
        Vector3 innerNormal = new Vector3(-cm, 0, -sm);
        AddQuad(bir, bil, til, tir, innerNormal);

        // 6. Outer Curve (outer radius)
        Vector3 outerNormal = new Vector3(cm, 0, sm);
        AddQuad(bol, bor, tor, tol, outerNormal);

        vertices = vertexList.ToArray();
        indices = indexList.ToArray();
    }
}
