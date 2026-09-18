using UnityEngine;

public class PlaceableMarkerLock : MonoBehaviour
{
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

        // remember each renderer's own starting color individually
        originalColors = new Color[pieceRenderers.Length];
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            originalColors[i] = pieceRenderers[i].material.color;
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
    }

    // called by PlaceableObjectController when the player puts this piece back without locking it
    public void Deselect()
    {
        isSelected = false;
        UpdateVisual();
    }

    // called every frame by PlaceableObjectController while this piece is selected with a new position to move to.
    // now uses the full X, Y, and Z of worldPoint - the piece can move freely in all three axes.
    public void MoveTo(Vector3 worldPoint)
    {
        if (!isSelected || isLocked)
        {
            return;
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
            SetAllRenderersColor(Color.cyan);
        }
        else if (isSelected)
        {
            SetAllRenderersColor(Color.green);
        }
        else if (isHovered)
        {
            SetAllRenderersColor(Color.yellow);
        }
        else
        {
            // restore each renderer to its own original color individually
            for (int i = 0; i < pieceRenderers.Length; i++)
            {
                pieceRenderers[i].material.color = originalColors[i];
            }
        }
    }

    private void SetAllRenderersColor(Color color)
    {
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            pieceRenderers[i].material.color = color;
        }
    }
}