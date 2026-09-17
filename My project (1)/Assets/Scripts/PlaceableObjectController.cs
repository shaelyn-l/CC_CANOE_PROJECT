using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

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

    // image that shows the sprite of the piece currently selected in the hotbar
    public Image hotbarPreviewImage;

    // how far in front of the player a summoned piece spawns
    private float spawnDistance = 3f;

    int hotbarIndex = 0;

    PlaceableMarkerLock hovered;
    PlaceableMarkerLock selected;

    void Update()
    {
        UpdateHover();
        HandleSelectClick();
        HandleHotbarInput();

        if (selected != null)
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

    void MoveSelected()
    {
        Gamepad gamepad = Gamepad.current;
 
        Vector2 direction = Vector2.zero;
        if (gamepad.dpad.up.isPressed) direction.y += 1f;
        if (gamepad.dpad.down.isPressed) direction.y -= 1f;
        if (gamepad.dpad.left.isPressed) direction.x -= 1f;
        if (gamepad.dpad.right.isPressed) direction.x += 1f;

        Debug.Log(direction);
 
        Vector2 delta = direction * Time.deltaTime;
        // current position + changed x and y positions
        Vector3 target = selected.transform.position + new Vector3(delta.x, delta.y, 0f);
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
        if (gamepad.leftTrigger.wasPressedThisFrame) cycleLeft = true;
        if (gamepad.rightTrigger.wasPressedThisFrame) cycleRight = true;
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

            // compute spawn point of object 
            Vector3 spawnPoint = headTransform.position + headTransform.forward * spawnDistance;
            spawnPoint.z = piece.homeZ;

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
            hotbarPreviewImage.enabled = false;
            return;
        }

        hotbarPreviewImage.sprite = pending[hotbarIndex].GetComponent<SpriteRenderer>().sprite;
    }
}