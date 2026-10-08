using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class PlaceableObjectController : MonoBehaviour
{
    // whose transform position define the crosshair ray
    [Header("Raycast Position")]
    public Transform headTransform;

    // max raycast distance for selecting
    private float maxRaycastDistance = 250f;

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

    // sound played whenever the left or right trigger cycles the hotbar
    public AudioClip cycleSound;

    [Header("Puzzle / Reference Toggle")]
    // the puzzle the player actively moves pieces around to complete
    public GameObject activePuzzle;
    // the completed puzzle the player can reference, shown in its place
    public GameObject referencePuzzle;

    // true while the reference puzzle is the one currently showing
    bool showingReference = false;

    // true while a toggle is already in progress, so a second press
    // can't interrupt it partway through
    bool isTransitioning = false;

    [Header("Transition Message")]
    // shown briefly while switching between the active and reference puzzle
    public TextMeshPro transitionText;
    public string transitionMessage = "One moment \u2014 bringing up the reference puzzle...";

    [Header("Puzzle Completion")]
    // played once, the moment every piece in Hotbar Pieces reports isLocked
    public AudioClip completionSound;
    // shown once the puzzle is complete, until the player presses A
    public TextMeshPro completionText;
    public string completionMessage = "Congratulations, you've put all the pieces into place.\nNow press A to see the final product.";
    // the player flies back to this object's reset position/rotation once
    // the final A is pressed (this is DPadFlyer, which already stores
    // CC_HUB's starting position/rotation as its own reset point)
    public DPadFlyer playerFlyer;
    // placeholder objects (fog, flowers, clouds, etc.) revealed once the
    // final A is pressed - just made active for now, no animation yet
    public GameObject[] revealObjects;

    // true once the completion sound/text have already fired, so this
    // never triggers a second time
    bool puzzleComplete = false;
    // true from the moment the puzzle completes until the player presses
    // A to dismiss the message and see the final product
    bool awaitingFinalPress = false;

    int hotbarIndex = 0;

    PlaceableMarkerLock hovered;
    PlaceableMarkerLock selected;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!GameStartGate.HasStarted) return;

        CheckPuzzleCompletion();
        HandleFinalPress();

        UpdateHover();
        HandleSelectClick();
        HandleActiveReferenceToggle();
        HandleHotbarInput();

        if (selected != null)
        {
            MoveSelected();
        }
    }

    // once every piece in hotbarPieces reports isLocked, play the
    // completion sound and show the message - only ever fires once
    void CheckPuzzleCompletion()
    {
        if (puzzleComplete) return;

        for (int i = 0; i < hotbarPieces.Count; i++)
        {
            if (!hotbarPieces[i].isLocked)
            {
                return;
            }
        }

        // every piece is locked
        puzzleComplete = true;
        awaitingFinalPress = true;

        if (completionSound != null)
        {
            audioSource.PlayOneShot(completionSound);
        }

        if (completionText != null)
        {
            completionText.text = completionMessage;
            completionText.gameObject.SetActive(true);
        }
    }

    // waits for a single press of A once the puzzle is complete, then
    // hides the message, sends the player back to the origin, and
    // reveals the placeholder objects for the (not yet implemented)
    // animation
    void HandleFinalPress()
    {
        if (!awaitingFinalPress) return;

        Gamepad gamepad = Gamepad.current;
        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            awaitingFinalPress = false;

            if (completionText != null)
            {
                completionText.gameObject.SetActive(false);
            }

            if (playerFlyer != null)
            {
                playerFlyer.ResetToOrigin();
            }

            for (int i = 0; i < revealObjects.Length; i++)
            {
                if (revealObjects[i] != null)
                {
                    revealObjects[i].SetActive(true);
                }
            }
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

    // West/X toggles between the active puzzle (which the player
    // interacts with) and the reference puzzle (the completed version
    // the player can look at)
    void HandleActiveReferenceToggle()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad.buttonWest.wasPressedThisFrame && !isTransitioning)
        {
            StartCoroutine(ToggleActiveReferenceRoutine());
        }
    }

    IEnumerator ToggleActiveReferenceRoutine()
    {
        isTransitioning = true;

        // show the message, then wait exactly one frame so Unity actually
        // draws and presents it to the screen BEFORE the potentially slow
        // switch below runs - without this wait, the message would be set
        // and then immediately overwritten by the freeze, never actually
        // becoming visible
        if (transitionText != null)
        {
            transitionText.text = transitionMessage;
            transitionText.gameObject.SetActive(true);
        }

        yield return null;

        showingReference = !showingReference;

        // if we're switching TO the reference view while carrying a
        // piece, drop it first - otherwise it would stay selected
        // while invisible and frozen behind the deactivated active puzzle
        if (showingReference && selected != null)
        {
            selected.Deselect();
            selected = null;
        }

        activePuzzle.SetActive(!showingReference);
        referencePuzzle.SetActive(showingReference);

        if (transitionText != null)
        {
            transitionText.gameObject.SetActive(false);
        }

        isTransitioning = false;
    }

    void MoveSelected()
    {
        Gamepad gamepad = Gamepad.current;

        Vector3 direction = Vector3.zero;
        if (gamepad.dpad.up.isPressed) direction.y += 0.25f;
        if (gamepad.dpad.down.isPressed) direction.y -= 0.25f;
        if (gamepad.dpad.left.isPressed) direction.x -= 0.5f;
        if (gamepad.dpad.right.isPressed) direction.x += 0.5f;

        // bumpers move the selected piece in Z
        if (gamepad.rightShoulder.isPressed) direction.z -= 0.5f; // back
        if (gamepad.leftShoulder.isPressed) direction.z += 0.5f;  // forward

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
            PlayCycleSound();
        }
        if (cycleRight == true)
        {
            hotbarIndex = (hotbarIndex + 1) % pending.Count;
            PlayCycleSound();
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

    void PlayCycleSound()
    {
        if (cycleSound == null)
        {
            return;
        }

        // PlayOneShot lets overlapping cycle presses layer naturally
        // instead of cutting each other off
        audioSource.PlayOneShot(cycleSound);
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
