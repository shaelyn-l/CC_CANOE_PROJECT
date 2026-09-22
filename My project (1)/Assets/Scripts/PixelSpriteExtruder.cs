using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PixelSpriteExtruder : MonoBehaviour
{
    public Texture2D texture;
    public float pixelSize = 0.1f;
    public float depth = 0.2f;
    public float alphaThreshold = 0.1f;

    void Start()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        if (texture == null)
            return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = texture.GetPixel(x, y);

                if (pixel.a <= alphaThreshold)
                    continue;

                AddCubeForPixel(
                    x,
                    y,
                    width,
                    height,
                    vertices,
                    triangles,
                    uvs
                );
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Extruded Pixel Sprite";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void AddCubeForPixel(
        int x,
        int y,
        int textureWidth,
        int textureHeight,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs
    )
    {
        float x0 = (x - textureWidth / 2f) * pixelSize;
        float x1 = x0 + pixelSize;

        float y0 = (y - textureHeight / 2f) * pixelSize;
        float y1 = y0 + pixelSize;

        float z0 = -depth / 2f;
        float z1 = depth / 2f;

        Vector3[] cubeVerts =
        {
            new Vector3(x0, y0, z0),
            new Vector3(x1, y0, z0),
            new Vector3(x1, y1, z0),
            new Vector3(x0, y1, z0),

            new Vector3(x0, y0, z1),
            new Vector3(x1, y0, z1),
            new Vector3(x1, y1, z1),
            new Vector3(x0, y1, z1)
        };

        int start = vertices.Count;
        vertices.AddRange(cubeVerts);

        int[] cubeTriangles =
        {
            // Back
            0, 2, 1,
            0, 3, 2,

            // Front
            4, 5, 6,
            4, 6, 7,

            // Left
            0, 4, 7,
            0, 7, 3,

            // Right
            1, 2, 6,
            1, 6, 5,

            // Bottom
            0, 1, 5,
            0, 5, 4,

            // Top
            3, 7, 6,
            3, 6, 2
        };

        foreach (int t in cubeTriangles)
            triangles.Add(start + t);

        for (int i = 0; i < 8; i++)
        {
            float u = x / (float)textureWidth;
            float v = y / (float)textureHeight;
            uvs.Add(new Vector2(u, v));
        }
    }
}