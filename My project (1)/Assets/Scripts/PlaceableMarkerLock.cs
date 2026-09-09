// /* PlaceableMarkerLock
//    Attach this to any GameObject that should be movable by the player and
//    snap-lock in place when its center aligns (in X/Y) with a designated
//    marker elsewhere in the scene.

//    - Assign "Target Marker" to the specific marker object THIS object should
//      pair with. Each movable object has its own independent marker, so
//      multiple movable/marker pairs can coexist in the same scene.
//    - Movement is restricted to world X/Y. Z is frozen the moment the object
//      is picked up (selected) and never changes.
//    - Once locked, the object stops responding to selection/movement entirely.

//    Requires a Collider (so PlaceableObjectController's raycast can find it).
//    A Renderer is optional but enables the hover/selected/locked color cues.

//    This script does not read any input itself - PlaceableObjectController
//    drives it by calling SetHovered() / Select() / Deselect() / MoveTo().
// */

// using UnityEngine;

// [RequireComponent(typeof(Collider))]
// public class PlaceableMarkerLock : MonoBehaviour
// {
//     [Header("Pairing")]
//     [Tooltip("The marker in the scene this object should snap to when its center aligns with it.")]
//     public Transform targetMarker;

//     [Header("Locking")]
//     [Tooltip("How close (in world X/Y) the object's center must get to the marker before it snaps and locks.")]
//     public float lockThreshold = 0.25f;

//     [Header("Hover / Selected / Locked Feedback (optional)")]
//     [Tooltip("If true, tints this object's material to show hover/selected/locked state.")]
//     public bool useColorFeedback = true;
//     public Color hoverColor = Color.yellow;
//     public Color selectedColor = Color.green;
//     public Color lockedColor = Color.cyan;

//     public bool IsSelected { get; private set; }
//     public bool IsLocked { get; private set; }
//     public bool IsHovered { get; private set; }

//     /// <summary>Current position with Z frozen at whatever it was when last selected.</summary>
//     public Vector3 FrozenPoint => new Vector3(transform.position.x, transform.position.y, _frozenZ);

//     Renderer _renderer;
//     Color _originalColor;
//     float _frozenZ;

//     void Awake()
//     {
//         _renderer = GetComponentInChildren<Renderer>();
//         if (_renderer != null)
//         {
//             // Instance the material so we don't tint every object sharing it.
//             _originalColor = _renderer.material.color;
//         }
//         _frozenZ = transform.position.z;
//     }

//     /// <summary>Called by PlaceableObjectController every frame with the current crosshair hover state.</summary>
//     public void SetHovered(bool hovered)
//     {
//         if (IsLocked) return;
//         IsHovered = hovered;
//         UpdateVisual();
//     }

//     /// <summary>Called by PlaceableObjectController when this object is picked up.</summary>
//     public void Select()
//     {
//         if (IsLocked) return;
//         IsSelected = true;
//         _frozenZ = transform.position.z; // lock in whatever Z it currently has
//         UpdateVisual();
//     }

//     /// <summary>Called by PlaceableObjectController when the player clicks again to drop it.</summary>
//     public void Deselect()
//     {
//         IsSelected = false;
//         UpdateVisual();
//     }

//     /// <summary>Called by PlaceableObjectController every frame with a world-space point to move toward (X/Y used, Z ignored).</summary>
//     public void MoveTo(Vector3 worldPoint)
//     {
//         if (!IsSelected || IsLocked) return;

//         Vector3 pos = transform.position;
//         pos.x = worldPoint.x;
//         pos.y = worldPoint.y;
//         pos.z = _frozenZ;
//         transform.position = pos;

//         CheckForLock();
//     }

//     void CheckForLock()
//     {
//         if (targetMarker == null) return;

//         Vector2 a = new Vector2(transform.position.x, transform.position.y);
//         Vector2 b = new Vector2(targetMarker.position.x, targetMarker.position.y);

//         if (Vector2.Distance(a, b) <= lockThreshold)
//         {
//             Vector3 pos = transform.position;
//             pos.x = targetMarker.position.x;
//             pos.y = targetMarker.position.y;
//             transform.position = pos;

//             IsLocked = true;
//             IsSelected = false;
//             IsHovered = false;
//             UpdateVisual();
//         }
//     }

//     void UpdateVisual()
//     {
//         if (!useColorFeedback || _renderer == null) return;

//         if (IsLocked) _renderer.material.color = lockedColor;
//         else if (IsSelected) _renderer.material.color = selectedColor;
//         else if (IsHovered) _renderer.material.color = hoverColor;
//         else _renderer.material.color = _originalColor;
//     }

// #if UNITY_EDITOR
//     void OnDrawGizmosSelected()
//     {
//         if (targetMarker == null) return;
//         Gizmos.color = Color.magenta;
//         Gizmos.DrawWireSphere(targetMarker.position, lockThreshold);
//         Gizmos.DrawLine(transform.position, targetMarker.position);
//     }
// #endif
// }

/* PlaceableMarkerLock
   Attach this to any GameObject that should be movable by the player and
   snap-lock in place when its center aligns (in X/Y) with a designated
   marker elsewhere in the scene.

   - Assign "Target Marker" to the specific marker object THIS object should
     pair with. Each movable object has its own independent marker, so
     multiple movable/marker pairs can coexist in the same scene.
   - Movement is restricted to world X/Y. Z is frozen the moment the object
     is picked up (selected) and never changes.
   - Once locked, the object stops responding to selection/movement entirely.

   Hotbar support:
   - If "Start Hidden" is checked, the object begins invisible and
     un-clickable (renderer + collider disabled) - effectively "in the
     player's inventory" - until PlaceableObjectController calls Summon().
   - "Home Z" (captured automatically at Awake, before hiding) is this
     piece's permanent, correct depth in the final puzzle image. Summoning
     always places the piece at this depth, regardless of where the player
     currently is, so each piece keeps the depth it was authored with.

   Requires a Collider (so PlaceableObjectController's raycast can find it).
   A Renderer is optional but enables the hover/selected/locked color cues.

   This script does not read any input itself - PlaceableObjectController
   drives it by calling SetHovered() / Select() / Deselect() / MoveTo() /
   Summon().
*/

/* PlaceableMarkerLock
   Attach this to any GameObject that should be movable by the player and
   snap-lock in place when its center aligns (in X/Y) with a designated
   marker elsewhere in the scene.

   - Assign "Target Marker" to the specific marker object THIS object should
     pair with. Each movable object has its own independent marker, so
     multiple movable/marker pairs can coexist in the same scene.
   - Movement is restricted to world X/Y. Z is frozen the moment the object
     is picked up (selected) and never changes.
   - Once locked, the object stops responding to selection/movement entirely.

   Hotbar support:
   - If "Start Hidden" is checked, the object begins invisible and
     un-clickable (renderer + collider disabled) - effectively "in the
     player's inventory" - until PlaceableObjectController calls Summon().
   - "Home Z" (captured automatically at Awake, before hiding) is this
     piece's permanent, correct depth in the final puzzle image. Summoning
     always places the piece at this depth, regardless of where the player
     currently is, so each piece keeps the depth it was authored with.

   Requires a Collider (so PlaceableObjectController's raycast can find it).
   A Renderer is optional but enables the hover/selected/locked color cues.

   This script does not read any input itself - PlaceableObjectController
   drives it by calling SetHovered() / Select() / Deselect() / MoveTo() /
   Summon().
*/

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlaceableMarkerLock : MonoBehaviour
{
    [Header("Pairing")]
    [Tooltip("The marker in the scene this object should snap to when its center aligns with it.")]
    public Transform targetMarker;

    [Header("Locking")]
    [Tooltip("How close (in world X/Y) the object's center must get to the marker before it snaps and locks.")]
    public float lockThreshold = 0.25f;

    [Header("Hotbar")]
    [Tooltip("If true, this piece starts invisible/unclickable until summoned from the hotbar.")]
    public bool startHidden = true;

    [Header("Hover / Selected / Locked Feedback (optional)")]
    [Tooltip("If true, tints this object's material to show hover/selected/locked state.")]
    public bool useColorFeedback = true;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;
    public Color lockedColor = Color.cyan;

    public bool IsSelected { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsHovered { get; private set; }
    public bool IsHidden { get; private set; }
    public bool IsSummoned { get; private set; }

    /// <summary>This piece's permanent, correct depth in the finished puzzle image.</summary>
    public float HomeZ { get; private set; }

    /// <summary>Current position with Z frozen at whatever it was when last selected.</summary>
    public Vector3 FrozenPoint => new Vector3(transform.position.x, transform.position.y, _frozenZ);

    /// <summary>This piece's existing world sprite, reused for the hotbar preview UI. Null if no SpriteRenderer is present.</summary>
    public Sprite PreviewSprite
    {
        get
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }
    }

    Renderer _renderer;
    Collider _collider;
    Color _originalColor;
    float _frozenZ;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _collider = GetComponent<Collider>();

        if (_renderer != null)
        {
            // Instance the material so we don't tint every object sharing it.
            _originalColor = _renderer.material.color;
        }

        // Capture the authored depth BEFORE any hiding happens.
        HomeZ = transform.position.z;
        _frozenZ = HomeZ;

        if (startHidden) HideImmediate();
    }

    void HideImmediate()
    {
        IsHidden = true;
        if (_renderer != null) _renderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
    }

    /// <summary>Called by PlaceableObjectController to bring this piece out of the hotbar at a world point (X/Y used; Z snaps to HomeZ). Immediately selects it.</summary>
    public void Summon(Vector3 worldPoint)
    {
        if (IsLocked) return;

        IsHidden = false;
        IsSummoned = true;
        if (_renderer != null) _renderer.enabled = true;
        if (_collider != null) _collider.enabled = true;

        Vector3 pos = worldPoint;
        pos.z = HomeZ;
        transform.position = pos;

        Select();
    }

    /// <summary>Called by PlaceableObjectController every frame with the current crosshair hover state.</summary>
    public void SetHovered(bool hovered)
    {
        if (IsLocked || IsHidden) return;
        IsHovered = hovered;
        UpdateVisual();
    }

    /// <summary>Called by PlaceableObjectController when this object is picked up.</summary>
    public void Select()
    {
        if (IsLocked || IsHidden) return;
        IsSelected = true;
        _frozenZ = transform.position.z; // lock in whatever Z it currently has
        UpdateVisual();
    }

    /// <summary>Called by PlaceableObjectController when the player clicks again to drop it.</summary>
    public void Deselect()
    {
        IsSelected = false;
        UpdateVisual();
    }

    /// <summary>Called by PlaceableObjectController every frame with a world-space point to move toward (X/Y used, Z ignored).</summary>
    public void MoveTo(Vector3 worldPoint)
    {
        if (!IsSelected || IsLocked) return;

        Vector3 pos = transform.position;
        pos.x = worldPoint.x;
        pos.y = worldPoint.y;
        pos.z = _frozenZ;
        transform.position = pos;

        CheckForLock();
    }

    void CheckForLock()
    {
        if (targetMarker == null) return;

        Vector2 a = new Vector2(transform.position.x, transform.position.y);
        Vector2 b = new Vector2(targetMarker.position.x, targetMarker.position.y);

        if (Vector2.Distance(a, b) <= lockThreshold)
        {
            Vector3 pos = transform.position;
            pos.x = targetMarker.position.x;
            pos.y = targetMarker.position.y;
            transform.position = pos;

            IsLocked = true;
            IsSelected = false;
            IsHovered = false;
            UpdateVisual();
        }
    }

    void UpdateVisual()
    {
        if (!useColorFeedback || _renderer == null) return;

        if (IsLocked) _renderer.material.color = lockedColor;
        else if (IsSelected) _renderer.material.color = selectedColor;
        else if (IsHovered) _renderer.material.color = hoverColor;
        else _renderer.material.color = _originalColor;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (targetMarker == null) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(targetMarker.position, lockThreshold);
        Gizmos.DrawLine(transform.position, targetMarker.position);
    }
#endif
}