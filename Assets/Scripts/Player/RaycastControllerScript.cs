using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RaycastControllerScript : MonoBehaviour
{
    public LayerMask collisionMask;
    [HideInInspector]
    public RaycastOrigins raycastOrigins;
    [HideInInspector]
    public CollisionInfo collisions;
    [HideInInspector]
    public const float SKIN_WIDTH = 0.015f;

    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    [HideInInspector]
    protected float horizontalRaySpacing;
    [HideInInspector]
    protected float verticalRaySpacing;

    private BoxCollider2D boxCollider;

    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool right, left;

        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld;

        public Vector3 velocityOld;

        public void Reset()
        {
            above = below = false;
            right = left = false;
            climbingSlope = false;
            descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }

    public virtual void Start()
    {
        boxCollider = gameObject.GetComponent<BoxCollider2D>();
    }

    public void UpdateRaycastOrigins()
    {
        Bounds colliderBounds = boxCollider.bounds;
        colliderBounds.Expand(SKIN_WIDTH * -2);      //placing the skin inside the collider

        raycastOrigins.bottomLeft = new Vector2(colliderBounds.min.x, colliderBounds.min.y);
        raycastOrigins.bottomRight = new Vector2(colliderBounds.max.x, colliderBounds.min.y);
        raycastOrigins.topLeft = new Vector2(colliderBounds.min.x, colliderBounds.max.y);
        raycastOrigins.topRight = new Vector2(colliderBounds.max.x, colliderBounds.max.y);
    }

    public void CalculateRaySpacing()
    {
        Bounds colliderBounds = boxCollider.bounds;
        colliderBounds.Expand(SKIN_WIDTH * -2);      //placing the skin inside the collider

        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);

        verticalRaySpacing = colliderBounds.size.x / (verticalRayCount - 1);
        horizontalRaySpacing = colliderBounds.size.y / (horizontalRayCount - 1);
    }
}
