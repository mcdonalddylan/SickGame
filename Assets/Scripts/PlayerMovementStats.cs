using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement")]
public class PlayerMovementStats : ScriptableObject
{
    [Header("Run")]
    [Range(0f, 1f)] public float moveThreshold = 0.25f;
    [Range(1f, 100f)] public float maxRunSpeed = 12.5f;
    [Range(0.25f, 50f)] public float groundAcceleration = 5f;
    [Range(0.25f, 50f)] public float groundDeceleration = 20f;
    [Range(0.25f, 50f)] public float airAcceleration = 5f;
    [Range(0.25f, 50f)] public float airDeceleration = 5f;
    [Range(0f, 2f)] public float turningSpeed = 0.05f;

    [Header("Wall Jump and Wall Slide")]
    [Range(0.25f, 50f)] public float wallJumpMoveAcceleration = 5f;
    [Range(0.25f, 50f)] public float wallJumpMoveDeceleration = 5f;
    public Vector2 wallJumpDirection = new Vector2(-20f, 6.5f);
    [Range(0f, 1f)] public float postWallJumpBufferTime = 0.125f;
    [Range(0.01f, 5f)] public float wallJumpGravityOnReleaseMultiplier = 1f;
    public bool resetJumpsOnWallSlide = true;
    [Min(0.01f)] public float wallSlideSpeed = 5f;
    [Range(0.25f, 50f)] public float wallSlideDecelerationSpeed = 50f;

    [Header("Dash")]
    [Range(0f, 1f)] public float dashTime = 0.11f;
    [Range(1f, 200f)] public float dashSpeed = 40f;
    [Range(0f, 1f)] public float timeBtwDashesOnGround = 0.225f;
    public bool resetDashOnWallSlide = true;
    [Range(0, 5)] public int numberOfDashesAllowed = 2;
    [Range(0f, 1f)] public float dashDiagonallyBias = 0.4f;

    [Header("Dash Cancel Time")]
    [Range(0.01f, 5f)] public float dashGravityOnReleaseMultiplier = 1f;
    [Range(0.02f, 0.3f)] public float dashTimeForUpwardsCancel = 0.027f;

    [Header("Grounded/Collision Checks")]
    public LayerMask groundLayer;
    public float groundDetectionRayLength = 0.02f;
    public float headDetectionRayLength = 0.02f;
    [Range(0f, 1f)] public float headWidth = 0.75f;
    public float wallDetectionRayLength = 0.125f;
    [Range(0.01f, 2f)] public float wallDetectionRayHeightMultiplier = 0.9f;

    [Header("Jump")]
    [Range(2f, 10f)] public float jumpHeight = 6.5f;
    [Range(1f, 1.2f)] public float jumpHeightCompensationFactor = 1.054f;
    [Range(0.05f,1.5f)] public float timeTillJumpApex = 0.35f;
    [Range(0.1f, 5f)] public float gravityOnReleaseMultiplier = 2f;
    [Range(1f, 50f)] public float maxFallSpeed = 26f;
    [Range(1, 5)] public int numberOfJumpsAllowed = 2;

    [Header("Jump Cut")]
    [Range(0.02f, 0.3f)] public float timeForUpwardsCancel = 0.027f;

    [Header("Jump Apex")]
    [Range(0.5f, 1f)] public float apexThreshold = 0.97f;
    [Range(0.01f, 1f)] public float apexHangTime = 0.075f;

    [Header("Jump Buffer")]
    [Range(0f, 1f)] public float jumpBufferTime = 0.125f;

    [Header("Jump Coyote Time")]
    [Range(0f, 1f)] public float jumpCoyoteTime = 0.1f;

    [Header("Debug")]
    public bool debugShowIsGroundedBox;
    public bool debugShowHeadBumpBox;
    public bool debugShowWallClingBox;

    [Header("Forced Fast Fall")]
    [Range(0f, 1f)] public float forcedFastFallMovementTriggerRange = 0.9f;
    [Range(0.01f, 50f)] public float forcedFastFallSpeed = 30f;

    public readonly Vector2[] DashDirections = new Vector2[]
    {
        new Vector2(0, 0), // Nothing
        new Vector2(1, 0), // Right
        new Vector2(1, 1).normalized, // Top-Right (normalized to prevent you from dashing diagonally too fast)
        new Vector2(0, 1), // Up
        new Vector2(-1, 1).normalized, // Top-Left (normalized to prevent you from dashing diagonally too fast)
        new Vector2(-1, 0), // Left
        new Vector2(-1, -1).normalized, // Bottom-Left (normalized to prevent you from dashing diagonally too fast)
        new Vector2(0, -1), // Down
        new Vector2(1, -1).normalized, // Bottom-Right (normalized to prevent you from dashing diagonally too fast)
    };

    // Jump
    public float Gravity { get; private set; }
    public float InitialJumpVelocity { get; private set; }
    public float AdjustedJumpHeight { get; private set;}

    // Wall Jump
    public float WallJumpGravity { get; private set; }
    public float InitialWallJumpVelocity { get; private set; }
    public float AdjustedWallJumpHeight { get; private set;}

    private void OnValidate()
    {
        CalculateValues();
    }

    private void OnEnabled()
    {
        CalculateValues();
    }

    private void CalculateValues()
    {
        // Jump
        AdjustedJumpHeight = jumpHeight * jumpHeightCompensationFactor;
        Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(timeTillJumpApex, 2f);
        InitialJumpVelocity = Mathf.Abs(Gravity) * timeTillJumpApex;

        // Wall Jump
        AdjustedWallJumpHeight = wallJumpDirection.y * jumpHeightCompensationFactor;
        WallJumpGravity = -(2f * AdjustedWallJumpHeight) / Mathf.Pow(timeTillJumpApex, 2f);
        InitialWallJumpVelocity = Mathf.Abs(WallJumpGravity) * timeTillJumpApex;
    }
}
