// /* PlaceableObjectController
//    Attach this ONCE anywhere in the scene (e.g. an empty GameObject).

//    Crosshair-based control scheme (works correctly with CC_CANOE's stereo
//    split-screen rendering, since it never relies on mouse screen position
//    mapping to a camera's viewport):

//    - Every frame, casts a ray from "Head Transform" (assign CC_HEAD) along
//      its forward direction to determine which PlaceableMarkerLock, if any,
//      is directly in front of the viewer.
//    - Left-click toggles select/deselect on whatever is currently hovered:
//        * Nothing selected + something hovered -> select it.
//        * Something selected -> deselect it (regardless of current hover).
//    - While something is selected, the mouse's raw movement delta (not its
//      absolute screen position) is used to nudge the object's X/Y each frame.
//      This sidesteps the stereo canvas entirely and will translate directly
//      to a gamepad stick later (same delta-based movement pattern).
//    - If that movement brings the object within its own lockThreshold of its
//      paired marker, PlaceableMarkerLock snaps and locks it automatically.

//    Requires the new Input System package (Mouse.current). Movable objects
//    need a Collider so the crosshair raycast can find them.
// */

// using UnityEngine;
// using UnityEngine.InputSystem;

// public class PlaceableObjectController : MonoBehaviour
// {
//     [Header("Crosshair Raycast")]
//     [Tooltip("Transform whose position and forward direction define the crosshair ray. Assign CC_HEAD here.")]
//     public Transform headTransform;

//     [Tooltip("Max raycast distance for selecting an object.")]
//     public float maxRaycastDistance = 100f;

//     [Tooltip("Layers considered when raycasting to select an object.")]
//     public LayerMask placeableLayers = ~0;

//     [Header("Movement")]
//     [Tooltip("How far the object moves per pixel of raw mouse movement.")]
//     public float mouseSensitivity = 0.01f;

//     PlaceableMarkerLock _hovered;
//     PlaceableMarkerLock _selected;

//     void Update()
//     {
//         Mouse mouse = Mouse.current;
//         if (mouse == null || headTransform == null) return;

//         UpdateHover();
//         HandleSelectClick(mouse);

//         if (_selected != null)
//         {
//             MoveSelected(mouse);
//         }
//     }

//     void UpdateHover()
//     {
//         PlaceableMarkerLock newHover = null;

//         Ray ray = new Ray(headTransform.position, headTransform.forward);
//         if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, placeableLayers, QueryTriggerInteraction.Collide))
//         {
//             newHover = hit.collider.GetComponentInParent<PlaceableMarkerLock>();
//         }

//         if (newHover != _hovered)
//         {
//             if (_hovered != null) _hovered.SetHovered(false);
//             _hovered = newHover;
//             if (_hovered != null) _hovered.SetHovered(true);
//         }
//     }

//     void HandleSelectClick(Mouse mouse)
//     {
//         if (!mouse.leftButton.wasPressedThisFrame) return;

//         if (_selected != null)
//         {
//             _selected.Deselect();
//             _selected = null;
//         }
//         else if (_hovered != null && !_hovered.IsLocked)
//         {
//             _selected = _hovered;
//             _selected.Select();
//         }
//     }

//     void MoveSelected(Mouse mouse)
//     {
//         Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
//         Vector3 target = _selected.transform.position + new Vector3(delta.x, delta.y, 0f);
//         _selected.MoveTo(target);

//         // If that move caused it to snap and lock this frame, release our reference.
//         if (_selected.IsLocked) _selected = null;
//     }
// }

/* PlaceableObjectController
   Attach this ONCE anywhere in the scene (e.g. an empty GameObject).

   Crosshair-based control scheme (works correctly with CC_CANOE's stereo
   split-screen rendering, since it never relies on mouse screen position
   mapping to a camera's viewport):

   - Every frame, casts a ray from "Head Transform" (assign CC_HEAD) along
     its forward direction to determine which PlaceableMarkerLock, if any,
     is directly in front of the viewer.
   - Left-click toggles select/deselect on whatever is currently hovered:
       * Nothing selected + something hovered -> select it.
       * Something selected -> deselect it (regardless of current hover).
   - While something is selected, the mouse's raw movement delta (not its
     absolute screen position) is used to nudge the object's X/Y each frame.
   - If that movement brings the object within its own lockThreshold of its
     paired marker, PlaceableMarkerLock snaps and locks it automatically.

   Hotbar:
   - Assign every hideable puzzle piece to "Hotbar Pieces".
   - Cycle through not-yet-summoned pieces with D-Pad Left/Right (gamepad)
     or [ and ] (keyboard).
   - Summon the currently-selected hotbar piece with the West/X gamepad
     button, or Enter/Space on keyboard. It spawns wherever the player is
     currently looking, at that piece's own correct fixed depth, and is
     immediately picked up.
   - Only one piece can be held at a time; summon is ignored while already
     carrying something.

   Requires the new Input System package. Movable objects need a Collider
   so the crosshair raycast can find them.
*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class PlaceableObjectController : MonoBehaviour
{
    [Header("Crosshair Raycast")]
    [Tooltip("Transform whose position and forward direction define the crosshair ray. Assign CC_HEAD here.")]
    public Transform headTransform;

    [Tooltip("Max raycast distance for selecting an object.")]
    public float maxRaycastDistance = 100f;

    [Tooltip("Layers considered when raycasting to select an object.")]
    public LayerMask placeableLayers = ~0;

    [Header("Movement")]
    [Tooltip("How far the object moves per pixel of raw mouse movement.")]
    public float mouseSensitivity = 0.01f;

    [Header("Hotbar")]
    [Tooltip("All puzzle pieces that start hidden and can be summoned one at a time.")]
    public List<PlaceableMarkerLock> hotbarPieces = new List<PlaceableMarkerLock>();

    [Tooltip("Gamepad button used to summon the currently-selected hotbar piece.")]
    public GamepadButton summonButton = GamepadButton.West;

    [Tooltip("UI Image (on a Screen Space - Overlay canvas) that always shows the sprite of the piece currently selected in the hotbar, about to be summoned. Leave empty to skip this feature.")]
    public Image hotbarPreviewImage;

    int _hotbarIndex = 0;

    PlaceableMarkerLock _hovered;
    PlaceableMarkerLock _selected;

    void Update()
    {
        if (headTransform == null) return;

        UpdateHover();
        HandleSelectClick();
        HandleHotbarInput();

        if (_selected != null)
        {
            MoveSelected();
        }
    }

    void UpdateHover()
    {
        PlaceableMarkerLock newHover = null;

        Ray ray = new Ray(headTransform.position, headTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, placeableLayers, QueryTriggerInteraction.Collide))
        {
            newHover = hit.collider.GetComponentInParent<PlaceableMarkerLock>();
        }

        if (newHover != _hovered)
        {
            if (_hovered != null) _hovered.SetHovered(false);
            _hovered = newHover;
            if (_hovered != null) _hovered.SetHovered(true);
        }
    }

    void HandleSelectClick()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        if (_selected != null)
        {
            _selected.Deselect();
            _selected = null;
        }
        else if (_hovered != null && !_hovered.IsLocked)
        {
            _selected = _hovered;
            _selected.Select();
        }
    }

    void MoveSelected()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        Vector3 target = _selected.transform.position + new Vector3(delta.x, delta.y, 0f);
        _selected.MoveTo(target);

        // If that move caused it to snap and lock this frame, release our reference.
        if (_selected.IsLocked) _selected = null;
    }

    List<PlaceableMarkerLock> PendingPieces()
    {
        return hotbarPieces.Where(p => p != null && !p.IsSummoned).ToList();
    }

    void HandleHotbarInput()
    {
        List<PlaceableMarkerLock> pending = PendingPieces();
        if (pending.Count == 0) return;

        if (_hotbarIndex >= pending.Count) _hotbarIndex = 0;

        bool cycleLeft = false, cycleRight = false, summonPressed = false;

        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            if (gp.dpad.left.wasPressedThisFrame) cycleLeft = true;
            if (gp.dpad.right.wasPressedThisFrame) cycleRight = true;
            if (gp[summonButton].wasPressedThisFrame) summonPressed = true;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftBracketKey.wasPressedThisFrame) cycleLeft = true;
            if (kb.rightBracketKey.wasPressedThisFrame) cycleRight = true;
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) summonPressed = true;
        }

        if (cycleLeft) _hotbarIndex = (_hotbarIndex - 1 + pending.Count) % pending.Count;
        if (cycleRight) _hotbarIndex = (_hotbarIndex + 1) % pending.Count;

        if (summonPressed && _selected == null)
        {
            PlaceableMarkerLock piece = pending[_hotbarIndex];
            Vector3 spawnPoint = ComputeSpawnPoint(piece);
            piece.Summon(spawnPoint);
            _selected = piece;
            _hotbarIndex = 0; // list just shrank; keep index valid
        }

        UpdateHotbarPreview();
    }

    void UpdateHotbarPreview()
    {
        if (hotbarPreviewImage == null) return;

        List<PlaceableMarkerLock> pending = PendingPieces();

        if (pending.Count == 0)
        {
            hotbarPreviewImage.enabled = false;
            return;
        }

        if (_hotbarIndex >= pending.Count) _hotbarIndex = 0;

        hotbarPreviewImage.sprite = pending[_hotbarIndex].PreviewSprite;
        hotbarPreviewImage.enabled = hotbarPreviewImage.sprite != null;
    }

    Vector3 ComputeSpawnPoint(PlaceableMarkerLock piece)
    {
        // Intersect the player's current view direction with a plane at the
        // piece's own fixed depth, so it always appears where they're looking.
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, piece.HomeZ));
        Ray ray = new Ray(headTransform.position, headTransform.forward);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        // Fallback (e.g. player already past this depth, looking parallel to
        // the plane): place it at the player's own X/Y at this piece's depth.
        Vector3 fallback = headTransform.position;
        fallback.z = piece.HomeZ;
        return fallback;
    }
}