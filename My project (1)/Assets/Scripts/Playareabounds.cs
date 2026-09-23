using UnityEngine;

// Attach this to a single object that defines the edges of the playable
// space (top, bottom, left, right, back - no front wall, since that's
// where the player is looking through toward CC_FRONT). Size and position
// it using the Box Collider like any other box in the scene.
//
// The Box Collider here is NEVER used for physics collision - since
// DPadFlyer, KeyboardTestFlyer, and PlaceableMarkerLock all move things
// by directly setting transform.position, Unity's physics system never
// gets involved, so a normal collider wouldn't stop anything on its own.
// Instead, this script is used purely as a data source: other scripts
// call ClampPosition() to cap a position to stay within this box before
// actually applying it.
[RequireComponent(typeof(BoxCollider))]
public class PlayAreaBounds : MonoBehaviour
{
    private BoxCollider boundsCollider;

    void Awake()
    {
        boundsCollider = GetComponent<BoxCollider>();

        // make sure this can never actually cause a physics collision -
        // it's only ever read for its size and position
        boundsCollider.isTrigger = true;
    }

    // Takes a position, and returns the closest position to it that's
    // still inside this box on all three axes.
    public Vector3 ClampPosition(Vector3 position)
    {
        Bounds bounds = boundsCollider.bounds;

        Vector3 clamped = position;
        clamped.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        clamped.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
        clamped.z = Mathf.Clamp(position.z, bounds.min.z, bounds.max.z);
        return clamped;
    }
}