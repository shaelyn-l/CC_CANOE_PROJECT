using UnityEngine;

// Attach this to a parent object (like "cloudsmoving" or "fog") whose
// children are the sprites that should drift continuously and loop.
//
// Each child slides along this object's local X axis. When a child
// passes the far edge, it wraps back by exactly the full width between
// the two edges, so as one sprite drifts out of view another is already
// coming in - it reads as one endless rolling layer.
//
// This only runs while the object is active, so leaving it inactive
// until the puzzle is finished (PlaceableObjectController turns it on
// through its Reveal Objects list) is all it takes to start the motion.
public class ScrollingLayer : MonoBehaviour
{
    [Tooltip("Units per second along this object's local X axis. Positive moves right, negative moves left.")]
    public float speed = -0.5f;

    [Tooltip("Left edge of the loop, in this object's local X coordinates. Set it past the visible left edge of the screen (by at least half the widest sprite) so wrapping happens out of sight.")]
    public float leftEdge = -10f;

    [Tooltip("Right edge of the loop, in this object's local X coordinates. Set it past the visible right edge of the screen (by at least half the widest sprite) so wrapping happens out of sight.")]
    public float rightEdge = 10f;

    [Header("Spin (optional)")]
    [Tooltip("If checked, each child also spins around its own Z axis (the one facing the viewer), each at its own random speed and direction.")]
    public bool spinChildren = false;

    [Tooltip("Slowest a child will spin, in degrees per second.")]
    public float minSpinSpeed = 10f;

    [Tooltip("Fastest a child will spin, in degrees per second.")]
    public float maxSpinSpeed = 60f;

    // one signed spin speed per child (negative = one way, positive = the
    // other), picked once at random rather than every frame
    float[] spinSpeeds;

    void Update()
    {
        // the full width of the loop - children wrap by exactly this much
        // so their spacing relative to each other never changes
        float span = rightEdge - leftEdge;

        // pick spin speeds the first time through (or again if children
        // were added or removed since they were last picked)
        if (spinChildren && (spinSpeeds == null || spinSpeeds.Length != transform.childCount))
        {
            PickSpinSpeeds();
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            Vector3 position = child.localPosition;
            position.x += speed * Time.deltaTime;

            if (position.x > rightEdge)
            {
                position.x -= span;
            }
            else if (position.x < leftEdge)
            {
                position.x += span;
            }

            child.localPosition = position;

            if (spinChildren)
            {
                child.Rotate(0f, 0f, spinSpeeds[i] * Time.deltaTime, Space.Self);
            }
        }
    }

    // gives every child its own random spin speed and direction
    void PickSpinSpeeds()
    {
        spinSpeeds = new float[transform.childCount];

        for (int i = 0; i < spinSpeeds.Length; i++)
        {
            float magnitude = Random.Range(minSpinSpeed, maxSpinSpeed);

            // randomly spin one way or the other
            float direction = Random.value < 0.5f ? -1f : 1f;

            spinSpeeds[i] = magnitude * direction;
        }
    }

    // Draws the loop range in the Scene view while this object is
    // selected, so you can see where children will wrap while you're
    // tuning the edges.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 leftBottom = transform.TransformPoint(new Vector3(leftEdge, -5f, 0f));
        Vector3 leftTop = transform.TransformPoint(new Vector3(leftEdge, 5f, 0f));
        Vector3 rightBottom = transform.TransformPoint(new Vector3(rightEdge, -5f, 0f));
        Vector3 rightTop = transform.TransformPoint(new Vector3(rightEdge, 5f, 0f));

        Gizmos.DrawLine(leftBottom, leftTop);
        Gizmos.DrawLine(rightBottom, rightTop);
    }
}