using UnityEngine;

/// <summary>
/// Spawns a flock of birds and steers them with boids rules (separation, alignment,
/// cohesion) inside a box. A slowly wandering target keeps the whole flock circling
/// around the area instead of settling in one spot.
///
/// Attach to an empty GameObject placed where the flock should fly. The object's
/// position is the CENTER of the flight box. Start it inactive and add it to
/// PlaceableObjectController's revealObjects[] so it appears after the puzzle is done.
/// </summary>
public class BirdFlock : MonoBehaviour
{
    [Header("Birds")]
    [Tooltip("The single-bird prefab (01Bird). Must have an Animator if you want flapping.")]
    public GameObject birdPrefab;
    public int count = 25;

    [Tooltip("Fix for models that don't face +Z. Try (0,90,0) or (0,-90,0) if birds fly sideways.")]
    public Vector3 modelRotationOffset = Vector3.zero;

    [Tooltip("Optional: exact Animator state name of the flap/fly loop. Lets each bird start at a random point so they don't flap in sync.")]
    public string flapStateName = "";
    public Vector2 animSpeedRange = new Vector2(0.85f, 1.15f);
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    [Header("Flight Area (centered on this object)")]
    public Vector3 areaSize = new Vector3(30f, 10f, 20f);

    [Header("Speed")]
    public float minSpeed = 3f;
    public float maxSpeed = 6f;
    [Tooltip("How quickly birds rotate to face their direction of travel.")]
    public float turnSpeed = 4f;

    [Header("Flocking")]
    public float neighborRadius = 6f;
    public float separationRadius = 2f;
    public float separationWeight = 3f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 0.4f;

    [Header("Wandering Target")]
    public float targetWeight = 0.8f;
    public float wanderSpeed = 0.08f;

    [Header("Bounds")]
    [Tooltip("How hard birds are pushed back when they leave the box.")]
    public float boundsWeight = 4f;

    Transform[] birds;
    Vector3[] velocities;
    float noiseSeed;

    void OnEnable()
    {
        // Spawn on first activation (flock starts inactive, so Awake/Start are too early).
        if (birds == null) Spawn();
    }

    void Spawn()
    {
        if (birdPrefab == null)
        {
            Debug.LogWarning("BirdFlock: no birdPrefab assigned.", this);
            birds = new Transform[0];
            velocities = new Vector3[0];
            return;
        }

        noiseSeed = Random.value * 100f;
        birds = new Transform[count];
        velocities = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f) * areaSize.x,
                Random.Range(-0.5f, 0.5f) * areaSize.y,
                Random.Range(-0.5f, 0.5f) * areaSize.z);

            GameObject b = Instantiate(birdPrefab, transform.position + offset, Quaternion.identity, transform);
            b.transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);
            birds[i] = b.transform;

            Vector3 dir = Random.onUnitSphere;
            dir.y *= 0.3f; // mostly level flight
            velocities[i] = dir.normalized * Random.Range(minSpeed, maxSpeed);
            b.transform.rotation = Quaternion.LookRotation(velocities[i]) * Quaternion.Euler(modelRotationOffset);

            Animator anim = b.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.speed = Random.Range(animSpeedRange.x, animSpeedRange.y);
                if (!string.IsNullOrEmpty(flapStateName))
                    anim.Play(flapStateName, 0, Random.value);
            }
        }
    }

    Vector3 GetWanderTarget()
    {
        float t = Time.time * wanderSpeed + noiseSeed;
        Vector3 n = new Vector3(
            Mathf.PerlinNoise(t, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, t + 10f) - 0.5f,
            Mathf.PerlinNoise(t + 20f, t) - 0.5f);
        // n is roughly -0.5..0.5, so * size * 0.8 keeps the target well inside the box
        return transform.position + Vector3.Scale(n, areaSize) * 0.8f;
    }

    void Update()
    {
        if (birds == null || birds.Length == 0) return;

        float dt = Time.deltaTime;
        Vector3 center = transform.position;
        Vector3 half = areaSize * 0.5f;
        Vector3 target = GetWanderTarget();
        float nr2 = neighborRadius * neighborRadius;
        float sr2 = separationRadius * separationRadius;

        for (int i = 0; i < birds.Length; i++)
        {
            Vector3 pos = birds[i].position;
            Vector3 separation = Vector3.zero;
            Vector3 avgVel = Vector3.zero;
            Vector3 avgPos = Vector3.zero;
            int neighbors = 0;

            for (int j = 0; j < birds.Length; j++)
            {
                if (j == i) continue;
                Vector3 off = birds[j].position - pos;
                float d2 = off.sqrMagnitude;
                if (d2 > nr2) continue;

                neighbors++;
                avgVel += velocities[j];
                avgPos += birds[j].position;

                if (d2 < sr2 && d2 > 0.0001f)
                    separation -= off / d2; // pushes away harder when closer
            }

            Vector3 accel = separation * separationWeight;

            if (neighbors > 0)
            {
                avgVel /= neighbors;
                avgPos /= neighbors;
                accel += (avgVel - velocities[i]) * alignmentWeight; // allignment
                accel += (avgPos - pos) * cohesionWeight; // cohesion
            }

            accel += (target - pos).normalized * targetWeight * maxSpeed;

            // Steer back inside the box
            Vector3 local = pos - center;
            if (Mathf.Abs(local.x) > half.x) accel.x -= Mathf.Sign(local.x) * boundsWeight * (Mathf.Abs(local.x) - half.x + 1f);
            if (Mathf.Abs(local.y) > half.y) accel.y -= Mathf.Sign(local.y) * boundsWeight * (Mathf.Abs(local.y) - half.y + 1f);
            if (Mathf.Abs(local.z) > half.z) accel.z -= Mathf.Sign(local.z) * boundsWeight * (Mathf.Abs(local.z) - half.z + 1f);

            Vector3 v = velocities[i] + accel * dt;
            float speed = Mathf.Clamp(v.magnitude, minSpeed, maxSpeed);
            v = v.normalized * speed;
            velocities[i] = v;

            birds[i].position = pos + v * dt;

            Quaternion goal = Quaternion.LookRotation(v) * Quaternion.Euler(modelRotationOffset);
            birds[i].rotation = Quaternion.Slerp(birds[i].rotation, goal, turnSpeed * dt);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}