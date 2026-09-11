using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class PlaceableObjectController : MonoBehaviour
{
    // whose transform position define the crosshair ray
    [Header("Crosshair Raycast")]
    public Transform headTransform;

    // max raycast distance for selecting
    public float maxRaycastDistance = 100f;

    // layers considered when raycasting to select an object
    public LayerMask placeableLayers;    

    // how far object moves per pixel of movement
    [Header("Movement")]
    public float mouseSensitivity = 0.01f;

    [Header("Hotbar")]
    // puzzle pieces to summon via hotbar
    public List<PlaceableMarkerLock> hotbarPieces = new List<PlaceableMarkerLock>();

    // gamepad button used to summon the currently-selected hotbar piece
    public GamepadButton summonButton = GamepadButton.West;

    // image that shows the sprite of the piece currently selected in the hotbar
    public Image hotbarPreviewImage;

    // how far in front of the player a summoned piece spawns
    public float spawnDistance = 3f;

    int _hotbarIndex = 0;

    PlaceableMarkerLock _hovered;
    PlaceableMarkerLock _selected;

    void Update()
    {
        UpdateHover();
        HandleSelectClick();
        HandleHotbarInput();

        if (_selected != null)
        {
            MoveSelected();
        }
    }

    // interact with SetHovered from PlacableMarkerLock
    void UpdateHover()
    {
        PlaceableMarkerLock newHover = null;

        // building the ray to represent where user is looking
        Ray ray = new Ray(headTransform.position, headTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, placeableLayers))
        {
            newHover = hit.collider.GetComponent<PlaceableMarkerLock>();
        }

        // if what raycast found was different than what was looked at last frame
        if (newHover != _hovered)
        {
            bool somethingHoveredBefore = _hovered != null;
            bool somethingHoveredNow = newHover != null;

             // was looking and one piece but now looking at another piece
            if (somethingHoveredBefore && somethingHoveredNow)
            {
                _hovered.SetHovered(false);
                _hovered = newHover;
                _hovered.SetHovered(true);
            }
            // was looking at a piece and now looking at nothing
            else if (somethingHoveredBefore && !somethingHoveredNow)
            {
                _hovered.SetHovered(false);
                _hovered = newHover;
            }
            // not looking at anything before but now looking at something
            else if (!somethingHoveredBefore && somethingHoveredNow)
            {
                _hovered = newHover;
                _hovered.SetHovered(true);
            }
        }
    }

    // 
    void HandleSelectClick()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // if something is selected but now deselecting
        if (_selected != null)
        {
            _selected.Deselect();
            _selected = null;
        }
        // if an object is in hovered state and not locked, then can move to selected state
        else if (_hovered != null && !_hovered.isLocked)
        {
            _selected = _hovered;
            _selected.Select();
        }
    }

    void MoveSelected()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // mouse.delta: how far the mouse moved since last frame
        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        // current position + changed x and y positions
        Vector3 target = _selected.transform.position + new Vector3(delta.x, delta.y, 0f);
        _selected.MoveTo(target);

        // if the move caused it to lock, release reference
        if (_selected.isLocked) _selected = null;
    }

    // update the pieces pending in hotbar
    List<PlaceableMarkerLock> PendingPieces()
    {
        List<PlaceableMarkerLock> pending = new List<PlaceableMarkerLock>();

        // loop through all pieces that were set in inspector before runtime
        for (int i = 0; i < hotbarPieces.Count; i++)
        {
            PlaceableMarkerLock piece = hotbarPieces[i];

            // if the current piece we are looking at is no in the scene yet
            // add it to the list of pieces that can be summoned later
            if (!piece.isSummoned)
            {
                pending.Add(piece);
            }
        }

        return pending;
    }

    void HandleHotbarInput()
    {
        List<PlaceableMarkerLock> pending = PendingPieces();
        if (pending.Count == 0) return;

        bool cycleLeft = false;
        bool cycleRight = false;
        bool summonPressed = false;

        // Gamepad gp = Gamepad.current;
        // if (gp != null)
        // {
        //     if (gp.dpad.left.wasPressedThisFrame) cycleLeft = true;
        //     if (gp.dpad.right.wasPressedThisFrame) cycleRight = true;
        //     if (gp[summonButton].wasPressedThisFrame) summonPressed = true;
        // }

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftBracketKey.wasPressedThisFrame) cycleLeft = true;
            if (kb.rightBracketKey.wasPressedThisFrame) cycleRight = true;
            if (kb.spaceKey.wasPressedThisFrame) summonPressed = true;
        }

        if (cycleLeft == true)
        {
            _hotbarIndex = (_hotbarIndex - 1 + pending.Count) % pending.Count;
        }
        if (cycleRight == true)
        {
            _hotbarIndex = (_hotbarIndex + 1) % pending.Count;
        }

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

    // spawn fixed distance in front of where player is currently looking 
    // then snap the depth to correct z
     Vector3 ComputeSpawnPoint(PlaceableMarkerLock piece)
    {
        Vector3 spawnPoint = headTransform.position + headTransform.forward * spawnDistance;
        spawnPoint.z = piece.homeZ;
        return spawnPoint;
    }

    void UpdateHotbarPreview()
    {
        List<PlaceableMarkerLock> pending = PendingPieces();

        if (pending.Count == 0)
        {
            hotbarPreviewImage.enabled = false;
            return;
        }

        hotbarPreviewImage.sprite = pending[_hotbarIndex].GetComponent<SpriteRenderer>().sprite;
    }
}