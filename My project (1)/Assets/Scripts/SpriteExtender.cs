using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>Extrudes the visible sprite silhouette along local Z. Uses Simple draw mode.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteExtender : MonoBehaviour
{
    [Min(0.001f)]
    [Tooltip("Thickness in local units. Object scale also affects the final thickness.")]
    public float depth = 0.3f;


    [Tooltip("Warm wood color for the exposed jigsaw-style edge.")]
    public Color woodEdgeColor = new Color(0.76f, 0.57f, 0.36f, 1f);


    [Range(0f, 1f)]
    [Tooltip("Amount of subtle wood grain on the sides. Zero gives a plain wood color.")]
    public float woodGrainStrength = 0.18f;


    [Range(0f, 1f)]
    [Tooltip("Discards transparent pixels on the front and back faces.")]
    public float alphaCutoff = 0.1f;


    // Used by SpriteSplitter. Serialized so split pieces survive scene saves.
    [HideInInspector] public Vector2 horizontalSlice = new Vector2(0f, 1f);
    [HideInInspector] public Vector2 geometryOffset;


    private Vector2 previousSlice, previousOffset;
    private int sliceStart, sliceEnd;
    private SpriteRenderer source;
    private GameObject generatedObject;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private Material material;
    private MaterialPropertyBlock properties;
    private Sprite previousSprite;
    private float previousDepth;
    private float previousCutoff;
    private Sprite maskSprite;
    private float maskCutoff;
    private bool[] opaquePixels;
    private int maskWidth, maskHeight;
    private bool previousFlipX, previousFlipY;
    private bool originalForceRenderingOff;
    private bool ownsVisibility;
    private bool dirty = true;
    private bool shaderMissing;


    private void OnEnable()
    {
        source = GetComponent<SpriteRenderer>();
        // Only mark dirty here if nothing has been built yet - don't force
        // a rebuild just because the object was temporarily hidden and is
        // becoming active again (see Cleanup()/OnDisable notes below).
        if (mesh == null) dirty = true;
        shaderMissing = false;
        Refresh();
    }


    // Validation can happen on a loading thread; defer Unity object changes.
    private void OnValidate() { dirty = true; }
    private void LateUpdate() { Refresh(); }


    [ContextMenu("Rebuild Extruded Sprite")]
    public void Rebuild() { dirty = true; maskSprite = null; }


    private void Refresh()
    {
        if (source == null) source = GetComponent<SpriteRenderer>();
        if (source == null) return;


        if (source.sprite == null || source.drawMode != SpriteDrawMode.Simple)
        {
            if (generatedObject != null) generatedObject.SetActive(false);
            RestoreSprite();
            return;
        }


        if (!EnsureResources()) return;
        float thickness = Mathf.Max(0.001f, depth);
        if (dirty || previousSprite != source.sprite || previousDepth != thickness || previousCutoff != alphaCutoff ||
            previousFlipX != source.flipX || previousFlipY != source.flipY ||
            previousSlice != horizontalSlice || previousOffset != geometryOffset)
        {
            BuildMesh(source.sprite, thickness);
            previousSprite = source.sprite;
            previousDepth = thickness;
            previousCutoff = alphaCutoff;
            previousFlipX = source.flipX;
            previousFlipY = source.flipY;
            previousSlice = horizontalSlice;
            previousOffset = geometryOffset;
            dirty = false;
        }


        if (!ownsVisibility)
        {
            originalForceRenderingOff = source.forceRenderingOff;
            ownsVisibility = true;
        }
        source.forceRenderingOff = true;
        generatedObject.SetActive(source.enabled && !originalForceRenderingOff);
        generatedObject.layer = gameObject.layer;
        meshRenderer.sortingLayerID = source.sortingLayerID;
        meshRenderer.sortingOrder = source.sortingOrder;


        properties.SetTexture("_MainTex", source.sprite.texture);
        properties.SetColor("_Tint", source.color);
        properties.SetColor("_WoodColor", woodEdgeColor);
        properties.SetFloat("_GrainStrength", Mathf.Clamp01(woodGrainStrength));
        properties.SetFloat("_Depth", thickness);
        properties.SetFloat("_Cutoff", Mathf.Clamp01(alphaCutoff));
        meshRenderer.SetPropertyBlock(properties);
    }


    private bool EnsureResources()
    {
        if (shaderMissing) return false;

        // Treat all the generated pieces as one unit. If ANY of them is
        // missing or has been destroyed (Unity's == null catches that
        // case, not just a plain null C# reference), rebuild all of them
        // together from scratch - this avoids ever ending up with, say, a
        // surviving mesh object still pointing at an already-destroyed
        // material, which is exactly the kind of mismatch that produces a
        // confusing null reference later in Refresh().
        bool needsRebuild = material == null || generatedObject == null ||
            mesh == null || meshRenderer == null || properties == null;

        if (!needsRebuild) return true;

        Release(generatedObject);
        Release(mesh);
        Release(material);
        generatedObject = null;
        meshRenderer = null;
        mesh = null;
        material = null;
        properties = null;

        Shader shader = Resources.Load<Shader>("SpriteExtender");
        if (shader == null)
        {
            Debug.LogError("SpriteExtender requires Assets/Resources/SpriteExtender.shader.", this);
            shaderMissing = true;
            return false;
        }
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

        generatedObject = new GameObject("Sprite Depth (Generated)");
        generatedObject.hideFlags = HideFlags.HideAndDontSave;
        generatedObject.transform.SetParent(transform, false);
        mesh = new Mesh { name = "Extruded Sprite", hideFlags = HideFlags.HideAndDontSave };
        generatedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = generatedObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        properties = new MaterialPropertyBlock();
        dirty = true;

        return true;
    }


    private void BuildMesh(Sprite sprite, float thickness)
    {
        Vector2[] points = sprite.vertices;
        Vector2[] spriteUV = sprite.uv;
        ushort[] spriteTriangles = sprite.triangles;
        int count = points.Length;
        var vertices = new List<Vector3>(count * 2);
        var uv = new List<Vector2>(count * 2);
        // UV2.x distinguishes faces (0) from sides (1).
        var surface = new List<Vector2>(count * 2);
        var triangles = new List<int>();




        BuildAlphaMask(sprite);
        sliceStart = Mathf.Clamp(Mathf.RoundToInt(horizontalSlice.x * maskWidth), 0, maskWidth);
        sliceEnd = Mathf.Clamp(Mathf.RoundToInt(horizontalSlice.y * maskWidth), sliceStart, maskWidth);
        float left = (sliceStart * sprite.rect.width / maskWidth - sprite.pivot.x) / sprite.pixelsPerUnit;
        float right = (sliceEnd * sprite.rect.width / maskWidth - sprite.pivot.x) / sprite.pixelsPerUnit;
        var polygon = new List<FaceVertex>(5);
        var clipped = new List<FaceVertex>(5);
        for (int t = 0; t < spriteTriangles.Length; t += 3)
        {
            polygon.Clear();
            for (int j = 0; j < 3; j++)
            {
                int index = spriteTriangles[t + j];
                polygon.Add(new FaceVertex { position = points[index], uv = spriteUV[index] });
            }
            ClipFace(polygon, clipped, left, true);
            ClipFace(clipped, polygon, right, false);
            if (polygon.Count < 3) continue;
            for (int face = 0; face < 2; face++)
            {
                int start = vertices.Count;
                foreach (FaceVertex vertex in polygon)
                {
                    Vector2 p = vertex.position;
                    vertices.Add(new Vector3((source.flipX ? -p.x : p.x) - geometryOffset.x,
                        (source.flipY ? -p.y : p.y) - geometryOffset.y,
                        (face == 0 ? -0.5f : 0.5f) * thickness));
                    uv.Add(vertex.uv);
                    surface.Add(Vector2.zero);
                }
                for (int j = 1; j < polygon.Count - 1; j++)
                {
                    int a = start, b = start + j, c = start + j + 1;
                    float winding = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).z;
                    if ((face == 0 && winding > 0f) || (face == 1 && winding < 0f))
                    { int swap = b; b = c; c = swap; }
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                }
            }
        }


        Vector3 Position(float x, float y, float z)
        {
            Vector2 p = (new Vector2(x * sprite.rect.width / maskWidth,
                y * sprite.rect.height / maskHeight) - sprite.pivot) / sprite.pixelsPerUnit;
            return new Vector3((source.flipX ? -p.x : p.x) - geometryOffset.x,
                (source.flipY ? -p.y : p.y) - geometryOffset.y, z);
        }


        void Wall(float ax, float ay, float bx, float by)
        {
            Vector3 a = Position(ax, ay, -thickness * 0.5f);
            Vector3 b = Position(bx, by, -thickness * 0.5f);
            if (source.flipX != source.flipY) { Vector3 swap = a; a = b; b = swap; }
            Vector3 backA = a + Vector3.forward * thickness;
            Vector3 backB = b + Vector3.forward * thickness;
            Vector3 normal = Vector3.Cross(backA - a, b - a).normalized;
            float shade = 0.75f + 0.25f * Vector3.Dot(normal, new Vector3(-0.6f, 0.8f, 0f));
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(backA); vertices.Add(backB); vertices.Add(b);
            for (int j = 0; j < 4; j++)
            {
                uv.Add(Vector2.zero);
                surface.Add(new Vector2(1f, shade));
            }
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }


        // Merge straight runs of exposed pixel edges. Internal transparent holes
        // get walls too; transparent padding never contributes to the silhouette.
        for (int y = 0; y <= maskHeight; y++)
        {
            int x = 0;
            while (x < maskWidth)
            {
                int direction = HorizontalBoundary(x, y);
                if (direction == 0) { x++; continue; }
                int start = x++;
                while (x < maskWidth && HorizontalBoundary(x, y) == direction) x++;
                if (direction > 0) Wall(x, y, start, y);
                else Wall(start, y, x, y);
            }
        }
        for (int x = 0; x <= maskWidth; x++)
        {
            int y = 0;
            while (y < maskHeight)
            {
                int direction = VerticalBoundary(x, y);
                if (direction == 0) { y++; continue; }
                int start = y++;
                while (y < maskHeight && VerticalBoundary(x, y) == direction) y++;
                if (direction > 0) Wall(x, start, x, y);
                else Wall(x, y, x, start);
            }
        }


        mesh.Clear();
        mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetUVs(1, surface);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }


    private struct FaceVertex
    {
        public Vector2 position, uv;
    }


    private static void ClipFace(List<FaceVertex> input, List<FaceVertex> output, float boundary, bool keepRight)
    {
        output.Clear();
        if (input.Count == 0) return;
        FaceVertex previous = input[input.Count - 1];
        bool previousInside = keepRight ? previous.position.x >= boundary : previous.position.x <= boundary;
        foreach (FaceVertex current in input)
        {
            bool inside = keepRight ? current.position.x >= boundary : current.position.x <= boundary;
            if (inside != previousInside)
            {
                float t = (boundary - previous.position.x) / (current.position.x - previous.position.x);
                output.Add(new FaceVertex { position = Vector2.Lerp(previous.position, current.position, t),
                    uv = Vector2.Lerp(previous.uv, current.uv, t) });
            }
            if (inside) output.Add(current);
            previous = current;
            previousInside = inside;
        }
    }


    private bool IsOpaque(int x, int y)
    {
        return x >= sliceStart && x < sliceEnd && y >= 0 && x < maskWidth && y < maskHeight && opaquePixels[y * maskWidth + x];
    }


    private int HorizontalBoundary(int x, int y)
    {
        return (IsOpaque(x, y) ? 1 : 0) - (IsOpaque(x, y - 1) ? 1 : 0);
    }


    private int VerticalBoundary(int x, int y)
    {
        return (IsOpaque(x, y) ? 1 : 0) - (IsOpaque(x - 1, y) ? 1 : 0);
    }


    private void BuildAlphaMask(Sprite sprite)
    {
        float cutoff = Mathf.Max(0.001f, Mathf.Clamp01(alphaCutoff));
        if (maskSprite == sprite && maskCutoff == cutoff && opaquePixels != null) return;


        // Read back a temporary GPU copy so source textures do not need Read/Write
        // enabled. Cache the mask: changing depth, tint, or flips does not read again.
        Texture2D texture = sprite.texture;
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D readable = null;
        Color32[] pixels;
        try
        {
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;
            readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
            readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            pixels = readable.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            Release(readable);
        }


        maskWidth = Mathf.Max(1, Mathf.CeilToInt(sprite.rect.width));
        maskHeight = Mathf.Max(1, Mathf.CeilToInt(sprite.rect.height));
        opaquePixels = new bool[maskWidth * maskHeight];
        Vector2[] positions = sprite.vertices;
        Vector2[] textureUV = sprite.uv;
        ushort[] indices = sprite.triangles;
        // Rasterize only inside the imported mesh. This also avoids neighboring
        // sprites in tightly packed atlases, including rotated atlas entries.
        for (int t = 0; t < indices.Length; t += 3)
        {
            int ia = indices[t], ib = indices[t + 1], ic = indices[t + 2];
            Vector2 a = positions[ia] * sprite.pixelsPerUnit + sprite.pivot;
            Vector2 b = positions[ib] * sprite.pixelsPerUnit + sprite.pivot;
            Vector2 c = positions[ic] * sprite.pixelsPerUnit + sprite.pivot;
            Vector2 ab = b - a, ac = c - a;
            float determinant = ab.x * ac.y - ab.y * ac.x;
            if (Mathf.Abs(determinant) < 0.000001f) continue;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(maskWidth - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(maskHeight - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 delta = new Vector2((x + 0.5f) * sprite.rect.width / maskWidth,
                    (y + 0.5f) * sprite.rect.height / maskHeight) - a;
                float u = (delta.x * ac.y - delta.y * ac.x) / determinant;
                float v = (ab.x * delta.y - ab.y * delta.x) / determinant;
                if (u < -0.00001f || v < -0.00001f || u + v > 1.00001f) continue;
                Vector2 sampleUV = textureUV[ia] + u * (textureUV[ib] - textureUV[ia])
                    + v * (textureUV[ic] - textureUV[ia]);
                int px = Mathf.Clamp(Mathf.FloorToInt(sampleUV.x * texture.width), 0, texture.width - 1);
                int py = Mathf.Clamp(Mathf.FloorToInt(sampleUV.y * texture.height), 0, texture.height - 1);
                opaquePixels[y * maskWidth + x] = pixels[py * texture.width + px].a / 255f >= cutoff;
            }
        }
        maskSprite = sprite;
        maskCutoff = cutoff;
    }


    private void RestoreSprite()
    {
        if (ownsVisibility && source != null) source.forceRenderingOff = originalForceRenderingOff;
        ownsVisibility = false;
    }


    // Deliberately NOT called from OnDisable anymore - see OnDestroy below.
    // Deactivating this object (e.g. toggling the whole puzzle group off
    // via SetActive) already stops it from rendering, since a child's
    // effective visibility depends on every ancestor being active too.
    // There's no need to destroy and later fully rebuild the generated
    // mesh (including the expensive GPU readback in BuildAlphaMask) just
    // to temporarily hide it - that rebuild should only happen when this
    // component is genuinely being destroyed for good.
    private void OnDestroy() { Cleanup(); }


    private void Cleanup()
    {
        RestoreSprite();
        if (generatedObject != null) generatedObject.SetActive(false);
        Release(generatedObject);
        Release(mesh);
        Release(material);
        generatedObject = null;
        meshRenderer = null;
        mesh = null;
        material = null;
        previousSprite = null;
        maskSprite = null;
        opaquePixels = null;
        dirty = true;
    }


    private static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}