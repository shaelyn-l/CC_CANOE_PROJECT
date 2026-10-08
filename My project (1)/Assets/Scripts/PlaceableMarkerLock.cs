using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaceableMarkerLock : MonoBehaviour
{
    [Header("Sounds")]
    public AudioClip selectedSound;
    public AudioClip droppedSound;
    public AudioClip hoveredSound;
    public AudioClip lockedSound;
    public AudioClip summonedSound;

    private AudioSource pieceAudioSource;

    // the markerthe piece should snap to
    [Header("Pairing")]
    public Transform targetMarker;

    // true -> starts hidden until summoned from the hotbar
    [Header("Hotbar")]
    public bool startHidden = true;

    public bool isSelected = false;
    public bool isLocked = false;
    public bool isHovered = false;
    public bool isHidden = false;
    public bool isSummoned = false;

    // how close in X/Y/Z the piece's center needs to get to marker before it snaps into place
    public float lockThreshold = 0.25f;

    // the flat icon shown in the hotbar preview - assigned manually, independent
    // of whatever this piece actually looks like in 3D (sprite, mesh, primitive, etc.)
    public Sprite previewIcon;

    [Header("Boundary")]
    // if assigned, this piece can never be moved outside this box
    public PlayAreaBounds playAreaBounds;

    [Header("Feedback Colors")]
    public Color hoverColor = new Color(1f, 1f, 0.6f);
    public Color selectedColor = new Color(0.6f, 1f, 0.6f);

    private Renderer[] pieceRenderers;
    private Collider pieceCollider;
    private Color[] originalColors;

    void Awake()
    {
        // get every renderer in this object AND its children (like the
        // sprite child) - GetComponentsInChildren includes the object
        // itself, not just children, so this covers both in one call
        pieceRenderers = GetComponentsInChildren<Renderer>(true);
        pieceCollider = GetComponent<Collider>();
        pieceAudioSource = GetComponent<AudioSource>();

        // remember each renderer's own starting color individually. For a
        // SpriteRenderer, read .color specifically rather than
        // .material.color - this matters for pieces using SpriteExtender,
        // which drives its own shader's tint from .color every frame and
        // would otherwise ignore (and overwrite) anything set on .material
        originalColors = new Color[pieceRenderers.Length];
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            originalColors[i] = GetOriginalColor(pieceRenderers[i]);
        }

        if (startHidden)
        {
            for (int i = 0; i < pieceRenderers.Length; i++)
            {
                pieceRenderers[i].enabled = false;
            }

            pieceCollider.enabled = false;
        }
    }

    // called by PlaceableObjectController when the player summons object out of hotbar
    // worldPoint: where the player is looking (X, Y, Z) - the piece spawns exactly there now
    public void Summon(Vector3 worldPoint)
    {
        isHidden = false;
        isSummoned = true;

        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            pieceRenderers[i].enabled = true;
        }

        pieceCollider.enabled = true;

        transform.position = worldPoint;

        PlaySound(summonedSound);

        // piece automatically picked up
        Select();
    }

    // called by PlaceableObjectController know whether the crosshair is pointing at it
    public void SetHovered(bool hovered)
    {
        if (isLocked || isHidden)
        {
            return;
        }

        isHovered = hovered;
        UpdateVisual();

        // only play a sound when hover actually STARTS, not when it ends -
        // this method is only called on genuine transitions anyway (the
        // controller only calls it when what's being looked at changes),
        // so this doesn't need its own debounce logic
        if (hovered)
        {
            PlaySound(hoveredSound);
        }
    }

    //called by PlaceableObjectController when the player picks the piece up
    public void Select()
    {
        if (isLocked || isHidden)
        {
            return;
        }

        isSelected = true;

        UpdateVisual();
        PlaySound(selectedSound);
    }

    // called by PlaceableObjectController when the player puts this piece back without locking it
    public void Deselect()
    {
        isSelected = false;
        UpdateVisual();
        PlaySound(droppedSound);
    }

    // called every frame by PlaceableObjectController while this piece is selected with a new position to move to.
    // now uses the full X, Y, and Z of worldPoint - the piece can move freely in all three axes.
    public void MoveTo(Vector3 worldPoint)
    {
        if (!isSelected || isLocked)
        {
            return;
        }

        if (playAreaBounds != null)
        {
            worldPoint = playAreaBounds.ClampPosition(worldPoint);
        }

        transform.position = worldPoint;

        // Use the marker's actual visual center, not just its transform
        // position - if the marker's pivot isn't centered on its mesh
        // (a common issue with imported/exported objects), comparing
        // against raw .position would measure to a corner instead of
        // the middle of the cube.
        Vector3 markerCenter = GetMarkerCenter();
        float distanceToMarker = Vector3.Distance(transform.position, markerCenter);

        if (distanceToMarker <= lockThreshold)
        {
            transform.position = markerCenter;

            isLocked = true;
            isSelected = false;
            isHovered = false;
            UpdateVisual();
            PlaySound(lockedSound);
        }
    }

    private Vector3 GetMarkerCenter()
    {
        Renderer markerRenderer = targetMarker.GetComponent<Renderer>();

        if (markerRenderer != null)
        {
            return markerRenderer.bounds.center;
        }

        // fallback for a marker with no renderer at all (e.g. a plain
        // empty object used purely as a position reference)
        return targetMarker.position;
    }

    private void UpdateVisual()
    {
        if (isLocked)
        {
            RestoreOriginalColors();
        }
        else if (isSelected)
        {
            SetAllRenderersColor(selectedColor);
        }
        else if (isHovered)
        {
            SetAllRenderersColor(hoverColor);
        }
        else
        {
            RestoreOriginalColors();
        }
    }

    private void RestoreOriginalColors()
    {
        // restore each renderer to its own original color individually
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            ApplyColor(pieceRenderers[i], originalColors[i]);
        }
    }

    private void SetAllRenderersColor(Color color)
    {
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            ApplyColor(pieceRenderers[i], color);
        }
    }

    private void ApplyColor(Renderer targetRenderer, Color color)
    {
        // SpriteExtender reads .color off the SpriteRenderer every frame to
        // drive its own shader's tint, and ignores .material entirely - so
        // for a SpriteRenderer, .color has to be set directly for the
        // color feedback to actually show up (this also works fine for a
        // plain SpriteRenderer with no SpriteExtender at all)
        SpriteRenderer spriteRenderer = targetRenderer as SpriteRenderer;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            return;
        }

        // Some custom shaders (like the one SpriteExtender's generated
        // mesh uses) don't expose a standard _Color property at all -
        // skip those rather than error trying to read/write a property
        // that doesn't exist on that shader.
        if (targetRenderer.material.HasProperty("_Color"))
        {
            targetRenderer.material.color = color;
        }
    }

    private Color GetOriginalColor(Renderer sourceRenderer)
    {
        SpriteRenderer spriteRenderer = sourceRenderer as SpriteRenderer;
        if (spriteRenderer != null)
        {
            return spriteRenderer.color;
        }

        if (sourceRenderer.material.HasProperty("_Color"))
        {
            return sourceRenderer.material.color;
        }

        // no usable color to read for this renderer's shader - the value
        // stored here will never actually be written back anywhere
        // (ApplyColor skips the same renderers for the same reason), so
        // this is just a harmless placeholder
        return Color.white;
    }

    private void PlaySound(AudioClip clip)
    {
        // if this state's clip was left empty in the Inspector, just skip
        // it silently rather than erroring
        if (clip == null)
        {
            return;
        }

        // PlayOneShot lets sounds overlap (e.g. a quick hover-then-select)
        // instead of cutting each other off the way Play() would
        pieceAudioSource.PlayOneShot(clip);
    }
}