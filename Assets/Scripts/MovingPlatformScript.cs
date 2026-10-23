using System.Collections.Generic;
using UnityEngine;

// Runs before the player so DeltaPosition is already up to date when the player's FixedUpdate reads it
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatformScript : MonoBehaviour
{
    private const float MinSegmentLength = 0.0001f;

    private static readonly List<MovingPlatformScript> ActivePlatforms = new List<MovingPlatformScript>();

    // Largest distance any platform rises this physics step (0 when none are rising)
    public static float MaxUpwardDelta
    {
        get
        {
            float max = 0f;
            foreach (MovingPlatformScript platform in ActivePlatforms)
            {
                max = Mathf.Max(max, platform.DeltaPosition.y);
            }
            return max;
        }
    }

    [Header("Movement Settings")]
    [SerializeField] private Transform[] points;
    [SerializeField] private float speed = 4f;

    private Rigidbody rb;
    private Vector3 currentPosition;
    private Vector3 segmentStart;
    private int targetIndex = 1;
    private float segmentProgress;

    // How far the platform moved this physics step. The player adds this to its own movement while riding.
    public Vector3 DeltaPosition { get; private set; }

    private void OnEnable() => ActivePlatforms.Add(this);

    private void OnDisable() => ActivePlatforms.Remove(this);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (points == null || points.Length < 2)
        {
            enabled = false;
            return;
        }

        currentPosition = points[0].position;
        segmentStart = currentPosition;
        rb.position = currentPosition;
        transform.position = currentPosition;
    }

    private void FixedUpdate()
    {
        float timeStep = Time.fixedDeltaTime * GameManager.timeScale * GameManager.nonPlayerTimeScale;

        Vector3 segmentEnd = points[targetIndex].position;
        AdvanceSegmentProgress(Vector3.Distance(segmentStart, segmentEnd), timeStep);

        // SmoothStep eases the platform into and out of every point
        float easedProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(segmentProgress));
        Vector3 nextPosition = Vector3.Lerp(segmentStart, segmentEnd, easedProgress);

        if (segmentProgress >= 1f)
        {
            StartNextSegment(segmentEnd);
        }

        DeltaPosition = nextPosition - currentPosition;
        currentPosition = nextPosition;

        rb.MovePosition(nextPosition);
    }

    private void AdvanceSegmentProgress(float segmentLength, float timeStep)
    {
        segmentProgress = segmentLength > MinSegmentLength
            ? segmentProgress + speed * timeStep / segmentLength
            : 1f;
    }

    private void StartNextSegment(Vector3 reachedPoint)
    {
        segmentStart = reachedPoint;
        segmentProgress = 0f;
        targetIndex = (targetIndex + 1) % points.Length;
    }

    // Optional: Visualize the path in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (points != null && points.Length >= 2)
        {
            for (int j = 0; j < points.Length; j++)
            {
                Transform next = points[(j + 1) % points.Length];
                if (points[j] == null || next == null) continue;
                Gizmos.DrawLine(points[j].position, next.position);
            }
        }
    }
}
