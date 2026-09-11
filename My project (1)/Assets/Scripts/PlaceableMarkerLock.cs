using UnityEngine;

public class PlaceableMarkerLock : MonoBehaviour
{
    // the markerthe piece should snap to
    [Header("Pairing")]
    public Transform targetMarker;

    // true -> starts hidden until summoned from the hotbar
    [Header("Hotbar")]
    public bool startHidden = true;

    // tue -> color changes based on state
    [Header("Interaction Feedback Colors")]
    public bool useColorFeedback = true;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.green;
    public Color lockedColor = Color.cyan;

    public bool isSelected = false;
    public bool isLocked = false;
    public bool isHovered = false;
    public bool isHidden = false;
    public bool isSummoned = false;

    // how close in X/Y the piece's center needs to get to marker before it snaps into place
    public float lockThreshold = 0.25f;

    // permanent depth in the finished puzzle
    //captured before the piece is hidden so summoning later puts it back at the right depth
    public float homeZ;

    private Renderer pieceRenderer;
    private Collider pieceCollider;
    private Color originalColor;

    void Awake()
    {
        // get references to the renderer and collider of gameobject
        pieceRenderer = GetComponent<Renderer>();
        pieceCollider = GetComponent<Collider>();

        originalColor = pieceRenderer.material.color;

        // Remember where this piece was placed in the editor - that is its
        // correct final depth for the puzzle, and it should never change.
        homeZ = transform.position.z;

        if (startHidden)
        {
            HideImmediate();
        }
    }

    private void HideImmediate()
    {
        isHidden = true;

        if (pieceRenderer != null)
        {
            pieceRenderer.enabled = false;
        }

        if (pieceCollider != null)
        {
            pieceCollider.enabled = false;
        }
    }

    // called by PlaceableObjectController when the player summons object out of hotbar
    // worldPoint: where the player is looking (X, Y)
    public void Summon(Vector3 worldPoint)
    {
        // if piece is already locked, cannot be summoned again
        // CHECK THIS LOGIC
        if (isLocked)
        {
            return;
        }

        isHidden = false;
        isSummoned = true;

        pieceRenderer.enabled = true;

        pieceCollider.enabled = true;

        Vector3 spawnPosition = worldPoint;
        spawnPosition.z = homeZ;
        transform.position = spawnPosition;

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

    // called every frame by PlaceableObjectController while this piece is selected with a new position to move to
    public void MoveTo(Vector3 worldPoint)
    {
        if (!isSelected || isLocked)
        {
            return;
        }

        Vector3 newPosition = transform.position;
        newPosition.x = worldPoint.x;
        newPosition.y = worldPoint.y;
        newPosition.z = homeZ;
        transform.position = newPosition;

        CheckForLock();
    }

    private void CheckForLock()
    {

        Vector2 piecePositionXY = new Vector2(transform.position.x, transform.position.y);
        Vector2 markerPositionXY = new Vector2(targetMarker.position.x, targetMarker.position.y);
        float distanceToMarker = Vector2.Distance(piecePositionXY, markerPositionXY);

        if (distanceToMarker <= lockThreshold)
        {
            Vector3 lockedPosition = transform.position;
            lockedPosition.x = targetMarker.position.x;
            lockedPosition.y = targetMarker.position.y;
            transform.position = lockedPosition;

            isLocked = true;
            isSelected = false;
            isHovered = false;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (isLocked)
        {
            pieceRenderer.material.color = lockedColor;
        }
        else if (isSelected)
        {
            pieceRenderer.material.color = selectedColor;
        }
        else if (isHovered)
        {
            pieceRenderer.material.color = hoverColor;
        }
        else
        {
            pieceRenderer.material.color = originalColor;
        }
    }
}