using System.Collections;
using UnityEngine;

// Kinematic movement for the enemy: custom gravity, capsule sweeps against the ground layer,
// wall detection and ledge detection. Mirrors how the player's MovementController works.
[RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    public const float CollisionPadding = 0.015f;

    [Header("Collision")]
    public LayerMask groundLayer;

    [Header("Gravity")]
    public float gravity = -60f;
    public float maxFallSpeed = 30f;

    [Header("Turning")]
    [Tooltip("Seconds the quick turn-around animation takes")]
    public float turnAnimationDuration = 0.05f;

    [Header("Ledge Detection")]
    [Tooltip("How far in front of the capsule edge to look for ground")]
    public float ledgeCheckForwardOffset = 0.2f;
    public float ledgeCheckDistance = 1f;

    [HideInInspector] public bool bounceOffSurfaces;
    [HideInInspector] public float bounciness = 0.8f;

    private static readonly Vector3 FacingLeft = new Vector3(0f, 180f, 0f);    private static readonly Vector3 FacingRight = Vector3.zero;

    private CapsuleCollider capsule;
    private Rigidbody rb;
    private Coroutine turnRoutine;
    private int enemyMask;

    public Vector2 velocity;
    public bool IsFacingRight { get; private set; } = true;
    public bool IsGrounded { get; private set; }
    public bool HitWall { get; private set; }
    public float FacingSign => IsFacingRight ? 1f : -1f;

    private void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        enemyMask = LayerMask.GetMask("Enemy");
        AssignEnemyLayer();
        rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
        SetFacing(true);
    }

    // Enemies live on the "Enemy" layer so the player treats them as solid ground
    private void AssignEnemyLayer()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0)
        {
            Debug.LogWarning("EnemyController: no 'Enemy' layer exists in Project Settings > Tags and Layers", this);
            return;
        }

        gameObject.layer = enemyLayer;
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (!c.isTrigger) c.gameObject.layer = enemyLayer;
        }
    }

    private void ConfigureRigidbody()
    {
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.automaticCenterOfMass = true;
        rb.automaticInertiaTensor = true;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        // Y is free so the enemy can turn around
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void SetFacing(bool faceRight)
    {
        CancelTurnAnimation();
        IsFacingRight = faceRight;
        transform.rotation = Quaternion.Euler(faceRight ? FacingRight : FacingLeft);
    }

    // Faces the given direction with the quick turn animation (facing state changes immediately)
    public void TurnTo(bool faceRight)
    {
        if (IsFacingRight == faceRight) return;
        IsFacingRight = faceRight;
        CancelTurnAnimation();
        turnRoutine = StartCoroutine(TurningAnimation(faceRight ? FacingRight : FacingLeft, turnAnimationDuration));
    }

    public void Turn() => TurnTo(!IsFacingRight);

    public void CancelTurnAnimation()
    {
        if (turnRoutine != null)
        {
            StopCoroutine(turnRoutine);
            turnRoutine = null;
        }
    }

    private IEnumerator TurningAnimation(Vector3 targetEuler, float duration)
    {
        Vector3 start = transform.rotation.eulerAngles;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            transform.eulerAngles = Vector3.Lerp(start, targetEuler, t);
            yield return null;
        }
        transform.eulerAngles = targetEuler;
        turnRoutine = null;
    }

    // Applies gravity, resolves collisions and moves the body. Call once per FixedUpdate.
    public void Tick(float dt)
    {
        if (bounceOffSurfaces)
        {
            TickBouncing(dt);
            return;
        }

        if (!IsGrounded || velocity.y > 0f)
            velocity.y = Mathf.Max(velocity.y + gravity * dt, -maxFallSpeed);
        Vector3 displacement = new Vector3(velocity.x, velocity.y, 0f) * dt;
        HitWall = false;

        // Horizontal
        if (!Mathf.Approximately(displacement.x, 0f))
        {
            float dir = Mathf.Sign(displacement.x);
            float dist = Mathf.Abs(displacement.x);
            if (Cast(Vector3.right * dir, dist + CollisionPadding, out RaycastHit hit))
            {
                displacement.x = Mathf.Max(0f, hit.distance - CollisionPadding) * dir;
                velocity.x = 0f;
                HitWall = true;
            }
        }

        // Vertical (from post-horizontal position)
        Vector3 offset = new Vector3(displacement.x, 0f, 0f);
        float vy = displacement.y;
        float probe = vy < 0f ? Mathf.Abs(vy) + CollisionPadding * 2f : CollisionPadding * 2f;
        Vector3 vDir = vy > 0f ? Vector3.up : Vector3.down;
        float vDist = vy > 0f ? vy + CollisionPadding : probe;

        IsGrounded = false;
        if (Cast(vDir, vDist, out RaycastHit vHit, offset))
        {
            if (vy > 0f)
            {
                displacement.y = Mathf.Max(0f, vHit.distance - CollisionPadding);
                velocity.y = 0f;
            }
            else
            {
                displacement.y = -Mathf.Max(0f, vHit.distance - CollisionPadding);
                velocity.y = 0f;
                IsGrounded = true;
            }
        }

        rb.MovePosition(rb.position + displacement);
    }

    // Death flight: gravity plus reflection off any ground/wall hit
    private void TickBouncing(float dt)
    {
        velocity.y = Mathf.Max(velocity.y + gravity * dt, -maxFallSpeed);
        Vector3 move = new Vector3(velocity.x, velocity.y, 0f) * dt;
        float dist = move.magnitude;
        if (dist > 0f && Cast(move.normalized, dist + CollisionPadding, out RaycastHit hit))
        {
            move = move.normalized * Mathf.Max(0f, hit.distance - CollisionPadding);
            Vector3 n = new Vector3(hit.normal.x, hit.normal.y, 0f).normalized;
            Vector3 v = Vector3.Reflect(new Vector3(velocity.x, velocity.y, 0f), n) * bounciness;
            velocity = new Vector2(v.x, v.y);
        }
        IsGrounded = false;
        rb.MovePosition(rb.position + move);
    }

    // True when there is ground just ahead
    public bool HasGroundAhead()
    {
        float radius = capsule.radius * Mathf.Abs(transform.lossyScale.x);
        Vector3 center = transform.TransformPoint(capsule.center);
        float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(transform.lossyScale.y) * 0.5f, radius);
        Vector3 origin = center + Vector3.right * FacingSign * (radius + ledgeCheckForwardOffset);
        float rayLength = halfHeight + ledgeCheckDistance;
        return Physics.Raycast(origin, Vector3.down, rayLength, groundLayer, QueryTriggerInteraction.Ignore);
    }

    public bool WallAhead(float distance = 0.1f)
    {
        return Cast(Vector3.right * FacingSign, distance + CollisionPadding, out _);
    }

    // True when another enemy (Enemy layer) is right in front of this one
    public bool EnemyAhead(float distance = 0.1f)
    {
        if (enemyMask == 0) return false;
        GetCapsulePoints(out Vector3 p1, out Vector3 p2, out float radius);
        RaycastHit[] hits = Physics.CapsuleCastAll(p1, p2, radius - CollisionPadding,
            Vector3.right * FacingSign, distance + CollisionPadding, enemyMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            EnemyController other = h.collider.GetComponentInParent<EnemyController>();
            if (other != null && other != this) return true;
        }
        return false;
    }

    private bool Cast(Vector3 direction, float distance, out RaycastHit hit, Vector3 originOffset = default)
    {
        GetCapsulePoints(out Vector3 p1, out Vector3 p2, out float radius);
        // Shrink slightly so the sweep never starts inside touching geometry
        return Physics.CapsuleCast(p1 + originOffset, p2 + originOffset, radius - CollisionPadding,
            direction, out hit, distance, groundLayer | enemyMask, QueryTriggerInteraction.Ignore);
    }

    private void GetCapsulePoints(out Vector3 p1, out Vector3 p2, out float radius)
    {
        Vector3 scale = transform.lossyScale;
        radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2f);
        Vector3 center = transform.TransformPoint(capsule.center);
        float half = height * 0.5f - radius;
        p1 = center + Vector3.up * half;
        p2 = center - Vector3.up * half;
    }
}
