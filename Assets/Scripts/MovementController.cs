using UnityEngine;

// Collision resolution for the player's kinematic Rigidbody.
// Instead of a few thin rays, the whole collider is swept with a BoxCast,
// so there are no gaps for the player to slip through (fast falls, dashes, slopes).
public class MovementController : MonoBehaviour
{
    // Gap kept between the collider and surfaces so we never start a sweep inside geometry
    public const float CollisionPadding = 0.015f;

    // Surfaces whose normal points down by more than this are ceilings (including sloped undersides), never walls
    private const float CeilingNormalY = -0.1f;

    // Steepest surface (in degrees) the player can walk up. Anything steeper is treated as a wall.
    [SerializeField, Range(0f, 80f)] private float maxSlopeAngle = 55f;

    // Layer of one-way platforms: solid only when landing on them from above
    private const string OneWayLayerName = "OneWayGround";
    private int oneWayMask;

    // Reused buffer for sweeps that need every surface along the path (avoids per-frame allocations)
    private readonly RaycastHit[] sweepHits = new RaycastHit[16];

    private BoxCollider boxCollider;
    private Rigidbody rb;
    private PlayerMovement playerMovement;
    private PlayerMovementStats moveStats;

    // Platform the player was standing on / wall sliding on during the last Move
    private MovingPlatformScript ridingPlatform;
    private MovingPlatformScript groundPlatform;
    private MovingPlatformScript wallPlatform;

    // Platform movement applied this step. Sweeps start from the carried position so they line up
    // with the platform's new position and the platform's motion is never mistaken for player movement.
    private Vector3 carryOffset;

    public bool isCollidingAbove { get; private set; }
    public bool isCollidingBelow { get; private set; }
    public bool isCollidingLeft { get; private set; }
    public bool isCollidingRight { get; private set; }

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        moveStats = playerMovement.playerMovementStats;
        oneWayMask = LayerMask.GetMask(OneWayLayerName);
    }

    // Resolves the desired per-frame displacement against the ground layer, then moves the body.
    // Order matters: horizontal first (may convert movement into a slope climb), then vertical
    // from the post-horizontal position.
    public void Move(Vector3 displacement)
    {
        ResetCollisionStates();
        groundPlatform = null;
        wallPlatform = null;

        // Carry the player along with the platform they were riding last step.
        // The platform updates before the player (execution order), so its delta is for this step.
        carryOffset = Vector3.zero;
        if (ridingPlatform != null)
        {
            Vector3 carry = ridingPlatform.DeltaPosition;
            carryOffset = new Vector3(carry.x, carry.y, 0f);
        }

        DetectAdjacentWalls();

        ResolveHorizontal(ref displacement);
        ResolveVertical(ref displacement);
        ResolveRisingPlatform(ref displacement);

        rb.MovePosition(rb.position + carryOffset + displacement);

        // Ride only while grounded on a platform, or wall sliding against one
        if (isCollidingBelow && groundPlatform != null)
            ridingPlatform = groundPlatform;
        else if (playerMovement.isWallSliding && wallPlatform != null)
            ridingPlatform = wallPlatform;
        else
            ridingPlatform = null;
    }

    // Sweeps the collider sideways. Walls stop the player; walkable slopes redirect the
    // horizontal movement up along the slope surface.
    private void ResolveHorizontal(ref Vector3 displacement)
    {
        if (Mathf.Approximately(displacement.x, 0f))
            return;

        float direction = Mathf.Sign(displacement.x);
        float distance = Mathf.Abs(displacement.x);

        if (!SweepCollider(Vector3.zero, Vector3.right * direction, distance + CollisionPadding * 2f, out RaycastHit hit))
            return;

        float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

        if (slopeAngle > 0.01f && slopeAngle <= maxSlopeAngle)
        {
            // Walkable slope: keep the same travel distance, but along the slope surface
            float slopeRadians = slopeAngle * Mathf.Deg2Rad;
            float climbY = Mathf.Sin(slopeRadians) * distance;

            // Only override vertical speed when we aren't already rising faster (e.g. jumping)
            if (displacement.y <= climbY)
            {
                Vector3 climb = new Vector3(Mathf.Cos(slopeRadians) * distance * direction, climbY, 0f);
                isCollidingBelow = true;

                // The slope itself is the nearest hit, so it can hide a wall further along the climb.
                // Sweep along the climb path and stop at the nearest surface that is too steep to walk on.
                if (SweepForBlockingSurface(climb.normalized, climb.magnitude + CollisionPadding * 2f, out RaycastHit blockingHit))
                {
                    float allowed = Mathf.Max(0f, blockingHit.distance - CollisionPadding);
                    climb *= Mathf.Clamp01(allowed / climb.magnitude);

                    if (direction < 0f) isCollidingLeft = true;
                    else isCollidingRight = true;
                }

                displacement.x = climb.x;
                displacement.y = climb.y;
            }
            return;
        }

        // Wall (or too steep): stop flush against it
        displacement.x = Mathf.Max(0f, hit.distance - CollisionPadding) * direction;

        // The underside of a sloped ceiling blocks movement but is not a wall: flagging it would
        // start a wall slide and slow the fall after a head bump.
        // While rising into it, it is a head bump instead. The sideways sweep can hit it before the
        // vertical sweep does, so without this the player stays pinned against it while still rising.
        if (hit.normal.y < CeilingNormalY)
        {
            if (displacement.y > 0f)
                isCollidingAbove = true;
            return;
        }

        if (direction < 0f) isCollidingLeft = true;
        else isCollidingRight = true;
    }

    // Sweeps the collider up/down from its post-horizontal position.
    // When not rising, we always probe a little below so the grounded flag stays stable while resting.
    private void ResolveVertical(ref Vector3 displacement)
    {
        float direction = displacement.y > 0f ? 1f : -1f;
        float distance = Mathf.Abs(displacement.y);

        Vector3 offset = Vector3.right * displacement.x;

        bool hitGround = SweepCollider(offset, Vector3.up * direction, distance + CollisionPadding * 2f, out RaycastHit hit);
        bool hitIsOneWay = false;

        // One-way platforms only matter when moving down onto them; the nearest surface wins
        if (direction < 0f && SweepOneWay(offset, distance + CollisionPadding * 2f, out RaycastHit oneWayHit)
            && (!hitGround || oneWayHit.distance < hit.distance))
        {
            hit = oneWayHit;
            hitGround = true;
            hitIsOneWay = true;
        }

        if (!hitGround)
            return;

        // Landing on a surface too steep to stand on (an angled wall): treat it as a wall, not ground,
        // and slide down along its surface so wall sliding can begin instead of getting stuck
        if (direction < 0f && Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
        {
            float allowed = Mathf.Max(0f, hit.distance - CollisionPadding);
            float remaining = Mathf.Max(0f, distance - allowed);

            Vector3 slide = Vector3.ProjectOnPlane(Vector3.down * remaining, hit.normal);
            slide.z = 0f;

            displacement.x += slide.x;
            displacement.y = -allowed + slide.y;

            // Surface normal pointing right means the wall is on the left, and vice versa
            if (hit.normal.x > 0f) isCollidingLeft = true;
            else isCollidingRight = true;
            return;
        }

        // A one-way slope can leave the player slightly inside it after moving sideways: a negative
        // distance means push up onto the surface instead of clamping to zero
        displacement.y = hitIsOneWay
            ? -(hit.distance - CollisionPadding)
            : Mathf.Max(0f, hit.distance - CollisionPadding) * direction;

        if (direction < 0f)
        {
            isCollidingBelow = true;
            groundPlatform = hit.collider.GetComponentInParent<MovingPlatformScript>();
        }
        else isCollidingAbove = true;
    }

    // A platform's collider only moves after this FixedUpdate, so the vertical sweep sees it at last step's position.
    // If a platform rises into the player this step, the player would end up overlapping it and every later
    // cast would ignore it (falling through). Look at where the player ends up and lift them with the platform.
    private void ResolveRisingPlatform(ref Vector3 displacement)
    {
        float rise = MovingPlatformScript.MaxUpwardDelta;
        if (rise <= 0f)
            return;

        Vector3 offset = Vector3.right * displacement.x + Vector3.up * displacement.y;
        if (!SweepCollider(offset, Vector3.down, rise + CollisionPadding * 2f, out RaycastHit hit))
            return;

        // Surfaces too steep to stand on are handled as walls elsewhere
        if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
            return;

        MovingPlatformScript platform = hit.collider.GetComponentInParent<MovingPlatformScript>();

        // The player's carry already moves them with the platform they were riding
        if (platform == null || platform == ridingPlatform || platform.DeltaPosition.y <= 0f)
            return;

        float gap = hit.distance - CollisionPadding;
        float overlap = platform.DeltaPosition.y - gap;
        if (overlap <= 0f)
            return;

        displacement.y += overlap;
        isCollidingBelow = true;
        groundPlatform = platform;
    }

    // Flags walls touching either side of the collider every frame, regardless of movement input.
    // Wall slide / wall jump need stable contact even when horizontal speed is zero,
    // otherwise the flags flicker and the slide state toggles each frame (jitter).
    private void DetectAdjacentWalls()
    {
        // The collider rests CollisionPadding away from a wall, so probe a little beyond that
        float probeDistance = CollisionPadding * 3f;

        if (SweepForBlockingSurface(Vector3.left, probeDistance, out RaycastHit leftHit, ignoreCeilings: true))
        {
            isCollidingLeft = true;
            wallPlatform = leftHit.collider.GetComponentInParent<MovingPlatformScript>();
        }

        if (SweepForBlockingSurface(Vector3.right, probeDistance, out RaycastHit rightHit, ignoreCeilings: true))
        {
            isCollidingRight = true;
            if (wallPlatform == null)
                wallPlatform = rightHit.collider.GetComponentInParent<MovingPlatformScript>();
        }
    }

    // Downward sweep against one-way platforms. A platform only counts if the player's feet are already
    // above its top surface, so the player passes up through it from below and from the side, but lands on top.
    // Casts ignore colliders they start inside of, which also lets the player drop through while overlapping.
    private bool SweepOneWay(Vector3 offset, float distance, out RaycastHit result)
    {
        result = default;
        if (oneWayMask == 0)
            return false;

        GetSweepBox(out Vector3 center, out Vector3 extents);
        Vector3 halfExtents = extents - Vector3.one * CollisionPadding;
        float feetY = center.y - extents.y;

        // Moving sideways onto a sloped platform leaves the box slightly inside it (casts ignore overlaps),
        // so start the cast higher by the most a walkable slope could rise over that horizontal step
        float lift = CollisionPadding * 2f + Mathf.Abs(offset.x) * Mathf.Tan(maxSlopeAngle * Mathf.Deg2Rad);

        int count = Physics.BoxCastNonAlloc(
            center + offset + Vector3.up * lift,
            halfExtents,
            Vector3.down,
            sweepHits,
            Quaternion.identity,
            distance + lift,
            oneWayMask,
            QueryTriggerInteraction.Ignore);

        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = sweepHits[i];

            // Only surfaces facing up that are walkable, hits that start inside a collider have no valid data
            if (candidate.distance <= 0f || Vector3.Angle(candidate.normal, Vector3.up) > maxSlopeAngle)
                continue;

            // Feet must be above the surface at the contact point (works for sloped platforms too).
            // The tolerance covers resting padding and the rise of the slope over this step's horizontal movement.
            if (feetY + lift < candidate.point.y)
                continue;

            // Convert distance back to be relative to the real (un-lifted) start position
            candidate.distance -= lift;

            if (!found || candidate.distance < result.distance)
            {
                result = candidate;
                found = true;
            }
        }

        return found;
    }

    // Sweeps a slightly shrunken copy of the collider along a direction.
    // Shrinking by the padding means the sweep never begins overlapping a surface we are resting on.
    private bool SweepCollider(Vector3 offset, Vector3 direction, float distance, out RaycastHit hit)
    {
        GetSweepBox(out Vector3 center, out Vector3 extents);
        Vector3 halfExtents = extents - Vector3.one * CollisionPadding;

        bool didHit = Physics.BoxCast(
            center + offset,
            halfExtents,
            direction,
            out hit,
            Quaternion.identity,
            distance,
            moveStats.groundLayer,
            QueryTriggerInteraction.Ignore);

        DrawDebug(center + offset, direction, distance, didHit);
        return didHit;
    }

    // Collider box in world space that ignores the transform's rotation.
    // boxCollider.bounds grows while the player is mid-turn, which makes sweeps start inside
    // walls/ground (casts ignore overlapping colliders) and lets the player pass through.
    private void GetSweepBox(out Vector3 center, out Vector3 extents)
    {
        Vector3 scale = transform.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        center = boxCollider.bounds.center;
        extents = Vector3.Scale(boxCollider.size, scale) * 0.5f;
    }

    // Sweeps the collider and returns the closest surface steeper than maxSlopeAngle
    // (walls, ceilings), ignoring walkable ground such as the slope being climbed.
    // With ignoreCeilings only wall-like surfaces count, so sloped undersides are skipped.
    private bool SweepForBlockingSurface(Vector3 direction, float distance, out RaycastHit blockingHit, bool ignoreCeilings = false)
    {
        GetSweepBox(out Vector3 center, out Vector3 extents);
        Vector3 halfExtents = extents - Vector3.one * CollisionPadding;

        int count = Physics.BoxCastNonAlloc(
            center,
            halfExtents,
            direction,
            sweepHits,
            Quaternion.identity,
            distance,
            moveStats.groundLayer,
            QueryTriggerInteraction.Ignore);

        blockingHit = default;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = sweepHits[i];

            // Skip walkable ground and sweeps that started inside a collider (no usable distance/normal)
            if (candidate.distance <= 0f || Vector3.Angle(candidate.normal, Vector3.up) <= maxSlopeAngle)
                continue;

            if (ignoreCeilings && candidate.normal.y < CeilingNormalY)
                continue;

            if (!found || candidate.distance < blockingHit.distance)
            {
                blockingHit = candidate;
                found = true;
            }
        }

        return found;
    }

    private void DrawDebug(Vector3 origin, Vector3 direction, float distance, bool didHit)
    {
        bool isVertical = Mathf.Abs(direction.y) > 0.5f;
        bool show = isVertical
            ? (direction.y < 0f ? moveStats.debugShowIsGrounded : moveStats.debugShowHeadRays)
            : moveStats.debugShowWallHit;

        if (show)
        {
            Debug.DrawRay(origin, direction * distance, didHit ? Color.cyan : Color.red);
        }
    }

    private void ResetCollisionStates()
    {
        isCollidingAbove = false;
        isCollidingBelow = false;
        isCollidingLeft = false;
        isCollidingRight = false;
    }

    #region Public Helper Methods

    // True while standing on a surface (including walkable slopes).
    public bool IsGrounded() => isCollidingBelow;

    // True when the head is touching a ceiling.
    public bool BumpedHead() => isCollidingAbove;

    // True when touching a wall on the side the player is facing.
    public bool IsTouchingWall(bool isFacingRight) => (isFacingRight && isCollidingRight) || (!isFacingRight && isCollidingLeft);

    // -1 = wall on left, 1 = wall on right, 0 = none.
    public int GetWallDirection()
    {
        if (isCollidingLeft) return -1;
        if (isCollidingRight) return 1;
        return 0;
    }

    #endregion
}
