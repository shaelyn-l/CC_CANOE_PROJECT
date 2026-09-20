using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PlaceableObjectController : MonoBehaviour
{
    // whose transform position define the crosshair ray
    [Header("Raycast Position")]
    public Transform headTransform;

    // max raycast distance for selecting
    private float maxRaycastDistance = 150f;

    // layers considered when raycasting to select an object
    public LayerMask placeableLayers;    

    [Header("Hotbar")]
    // puzzle pieces to summon via hotbar
    public List<PlaceableMarkerLock> hotbarPieces = new List<PlaceableMarkerLock>();

    // sprite renderer that shows the sprite of the piece currently selected in the hotbar
    public SpriteRenderer hotbarPreviewRenderer;

    // how far in front of the player a summoned piece spawns
    private float spawnDistance = 3f;

    // how many world units per second a piece moves at full input
    public float moveSpeed = 0.5f;

    int hotbarIndex = 0;

    PlaceableMarkerLock hovered;
    PlaceableMarkerLock selected;

    // true while X is toggled on - triggers control Z movement instead of
    // cycling the hotbar
    bool zAxisMode = false;

    void Update()
    {
        UpdateHover();
        HandleSelectClick();
        HandleZAxisModeToggle();
        HandleHotbarInput();

        if (selected != null)
        {
            MoveSelected();
        }
    }

    // interact with SetHovered from PlacableMarkerLock
    void UpdateHover()
    {
        // hover has no purpose while a piece is already selected - the
        // next button press just drops whatever's currently held, it
        // never acts on hover. Skipping this entirely while carrying
        // something also stops stray hover sounds/color changes from
        // firing on pieces the crosshair happens to sweep across mid-drag.
        if (selected != null)
        {
            return;
        }

        PlaceableMarkerLock newHover = null;

        // building the ray to represent where user is looking
        Ray ray = new Ray(headTransform.position, headTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, placeableLayers))
        {
            newHover = hit.collider.GetComponent<PlaceableMarkerLock>();
        }

        // if what raycast found was different than what was looked at last frame
        if (newHover != hovered)
        {
            bool somethingHoveredBefore = hovered != null;
            bool somethingHoveredNow = newHover != null;

             // was looking and one piece but now looking at another piece
            if (somethingHoveredBefore && somethingHoveredNow)
            {
                hovered.SetHovered(false);
                hovered = newHover;
                hovered.SetHovered(true);
            }
            // was looking at a piece and now looking at nothing
            else if (somethingHoveredBefore && !somethingHoveredNow)
            {
                hovered.SetHovered(false);
                hovered = newHover;
            }
            // not looking at anything before but now looking at something
            else if (!somethingHoveredBefore && somethingHoveredNow)
            {
                hovered = newHover;
                hovered.SetHovered(true);
            }
        }
    }

    void HandleSelectClick()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad.buttonEast.wasPressedThisFrame)
        {
            // if something is selected but now deselecting
            if (selected != null)
            {
                selected.Deselect();
                selected = null;
            }
            // if an object is in hovered state and not locked, then can move to selected state
            else if (hovered != null && !hovered.isLocked)
            {
                selected = hovered;
                selected.Select();
            }
        }
        
    }

    // West/X toggles whether the triggers cycle the hotbar or move the
    // selected piece along Z instead
    void HandleZAxisModeToggle()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad.buttonWest.wasPressedThisFrame)
        {
            zAxisMode = !zAxisMode;
        }
    }

    void MoveSelected()
    {
        Gamepad gamepad = Gamepad.current;

        Vector3 direction = Vector3.zero;
        if (gamepad.dpad.up.isPressed) direction.y += 0.25f;
        if (gamepad.dpad.down.isPressed) direction.y -= 0.25f;
        if (gamepad.dpad.left.isPressed) direction.x -= 0.5f;
        if (gamepad.dpad.right.isPressed) direction.x += 0.5f;

        // triggers only move the piece in Z while zAxisMode is active -
        // otherwise they're busy cycling the hotbar instead
        if (zAxisMode)
        {
            if (gamepad.rightTrigger.isPressed) direction.z -= 0.5f; // back
            if (gamepad.leftTrigger.isPressed) direction.z += 0.5f;  // forward
        }

        Vector3 delta = direction * moveSpeed * Time.deltaTime;
        // current position + changed x, y, and z positions
        Vector3 target = selected.transform.position + delta;
        selected.MoveTo(target);
 
        // if the move caused it to lock, release reference
        if (selected.isLocked) selected = null;
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

        Gamepad gamepad = Gamepad.current;

        // while zAxisMode is active, the triggers are busy moving the
        // selected piece in Z instead of cycling the hotbar
        if (!zAxisMode)
        {
            if (gamepad.leftTrigger.wasPressedThisFrame) cycleLeft = true;
            if (gamepad.rightTrigger.wasPressedThisFrame) cycleRight = true;
        }

        if (gamepad.buttonNorth.wasPressedThisFrame) summonPressed = true;

        // Keyboard kb = Keyboard.current;
        // if (kb != null)
        // {
        //     if (kb.leftBracketKey.wasPressedThisFrame) cycleLeft = true;
        //     if (kb.rightBracketKey.wasPressedThisFrame) cycleRight = true;
        //     if (kb.spaceKey.wasPressedThisFrame) summonPressed = true;
        // }

        if (cycleLeft == true)
        {
            hotbarIndex = (hotbarIndex - 1 + pending.Count) % pending.Count;
        }
        if (cycleRight == true)
        {
            hotbarIndex = (hotbarIndex + 1) % pending.Count;
        }

        if (summonPressed && selected == null)
        {
            PlaceableMarkerLock piece = pending[hotbarIndex];

            // compute spawn point of object - now used directly in all
            // three axes, no depth override
            Vector3 spawnPoint = headTransform.position + headTransform.forward * spawnDistance;

            piece.Summon(spawnPoint);
            selected = piece;
            hotbarIndex = 0;
        }
        UpdateHotbarPreview();
    }

    void UpdateHotbarPreview()
    {
        List<PlaceableMarkerLock> pending = PendingPieces();

        if (pending.Count == 0)
        {
            hotbarPreviewRenderer.enabled = false;
            return;
        }

        hotbarPreviewRenderer.enabled = true;
        hotbarPreviewRenderer.sprite = pending[hotbarIndex].previewIcon;
    }
}