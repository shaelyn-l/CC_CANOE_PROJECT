using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Creates three movable vertical thirds, with wood around every cut.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(SpriteExtender))]
public sealed class SpriteSplitter : MonoBehaviour
{
    [Tooltip("Split automatically when entering Play mode. Otherwise use the Inspector split button or call Split().")]
    public bool splitOnStart;

    [Min(0f)]
    [Tooltip("Initial spacing between pieces in local units. Zero preserves the assembled image.")]
    public float separation = 0.1f;

    [SerializeField, HideInInspector] private Transform[] pieces;
    [SerializeField, HideInInspector] private bool isSplit;
    [SerializeField, HideInInspector] private bool originalRendererEnabled;
    [SerializeField, HideInInspector] private bool originalExtenderEnabled;

    /// <summary>Each piece has its own transform and SpriteExtender; left to right in local space.</summary>
    public System.Collections.Generic.IReadOnlyList<Transform> Pieces => pieces;
    public bool IsSplit => isSplit;

    private void Start()
    {
        if (splitOnStart && !isSplit) Split();
    }

    [ContextMenu("Split Into Three Vertical Pieces")]
    public void Split()
    {
        if (isSplit) return;
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        SpriteExtender original = GetComponent<SpriteExtender>();
        Sprite sprite = source.sprite;
        if (sprite == null || source.drawMode != SpriteDrawMode.Simple || sprite.rect.width < 3f)
        {
            Debug.LogWarning("Assign a sprite at least three pixels wide and use Simple draw mode before splitting.", this);
            return;
        }
        if (original.horizontalSlice != new Vector2(0f, 1f))
        {
            Debug.LogWarning("Split the original whole sprite, rather than an already sliced piece.", this);
            return;
        }

#if UNITY_EDITOR
        int undoGroup = BeginUndo("Split Sprite", source, original);
#endif
        originalRendererEnabled = source.enabled;
        originalExtenderEnabled = original.enabled;
        pieces = new Transform[3];
        int width = Mathf.CeilToInt(sprite.rect.width);
        string[] labels = { "Left", "Middle", "Right" };
        for (int i = 0; i < 3; i++)
        {
            // Flip changes which image third belongs on the visual left.
            int imageThird = source.flipX ? 2 - i : i;
            int start = Mathf.RoundToInt(imageThird * width / 3f);
            int end = Mathf.RoundToInt((imageThird + 1) * width / 3f);
            float centerX = ((start + end) * 0.5f * sprite.rect.width / width - sprite.pivot.x) / sprite.pixelsPerUnit;
            float centerY = (sprite.rect.height * 0.5f - sprite.pivot.y) / sprite.pixelsPerUnit;
            Vector2 center = new Vector2(source.flipX ? -centerX : centerX, source.flipY ? -centerY : centerY);

            GameObject piece = new GameObject(name + " - " + labels[i]);
            piece.SetActive(false); // Configure before SpriteExtender's OnEnable builds its mesh.
            piece.layer = gameObject.layer;
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = new Vector3(center.x + (i - 1) * Mathf.Max(0f, separation), center.y, 0f);
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = source.color;
            renderer.flipX = source.flipX;
            renderer.flipY = source.flipY;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            SpriteExtender extender = piece.AddComponent<SpriteExtender>();
            extender.depth = original.depth;
            extender.woodEdgeColor = original.woodEdgeColor;
            extender.woodGrainStrength = original.woodGrainStrength;
            extender.alphaCutoff = original.alphaCutoff;
            extender.horizontalSlice = new Vector2(imageThird / 3f, (imageThird + 1) / 3f);
            extender.geometryOffset = center;
            pieces[i] = piece.transform;
            piece.SetActive(true);
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(piece, "Create Sprite Piece");
#endif
        }
        original.enabled = false;
        source.enabled = false;
        isSplit = true;
#if UNITY_EDITOR
        EndUndo(undoGroup);
#endif
    }

    [ContextMenu("Restore Whole Sprite")]
    public void Restore()
    {
        if (!isSplit) return;
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        SpriteExtender original = GetComponent<SpriteExtender>();
#if UNITY_EDITOR
        int undoGroup = BeginUndo("Restore Whole Sprite", source, original);
#endif
        if (pieces != null)
        {
            foreach (Transform piece in pieces)
            {
                if (piece == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying) { Undo.DestroyObjectImmediate(piece.gameObject); continue; }
#endif
                piece.gameObject.SetActive(false);
                Destroy(piece.gameObject);
            }
        }
        pieces = null;
        isSplit = false;
        source.enabled = originalRendererEnabled;
        original.enabled = originalExtenderEnabled;
#if UNITY_EDITOR
        EndUndo(undoGroup);
#endif
    }

#if UNITY_EDITOR
    private int BeginUndo(string label, SpriteRenderer source, SpriteExtender extender)
    {
        if (Application.isPlaying) return -1;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(label);
        Undo.RecordObjects(new Object[] { this, source, extender }, label);
        return group;
    }

    private void EndUndo(int group)
    {
        if (group < 0) return;
        EditorUtility.SetDirty(this);
        PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        PrefabUtility.RecordPrefabInstancePropertyModifications(GetComponent<SpriteRenderer>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(GetComponent<SpriteExtender>());
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Undo.CollapseUndoOperations(group);
    }
#endif
}
