using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementStats playerMovementStats;
    [SerializeField] private Collider2D bodyColl;
    [SerializeField] private Collider2D feetColl;

    private Rigidbody2D rigidbody;
    private PlayerParticleScript particleScript;
    private TimeControlScript timeControlScript;
    public int playerNumber = 1;

    // Movement vars
    public float HorizontalVelocity { get; private set; }
    public bool isFacingRight;
    private Vector3 FACING_LEFT = new Vector3(0, 180, 0);
    private Vector3 FACING_RIGHT = Vector3.zero;

    // Collision check vars
    private RaycastHit2D groundHit;
    private RaycastHit2D headHit;
    private RaycastHit2D wallHit;
    private RaycastHit2D lastWallHit;
    private bool isGrounded;
    private bool bumpedHead;
    private bool isTouchingWall;

    // Jump vars
    public float VerticalVelocity { get; private set; }
    private bool isJumping;
    private bool isFastFalling;
    private bool isFalling = true;
    private float fastFallTime;
    private float fastFallReleaseSpeed;
    private int numberOfJumpsUsed;

    // Apex vars
    private float apexPoint;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;

    // Jump buffer vars
    private float jumpBufferTimer;
    private bool jumpReleasedDuringBuffer;

    // Coyote time vars
    private float coyoteTimer;

    // Wall slide vars
    private bool isWallSliding;
    private bool isWallSlideFalling;

    // Wall jump vars
    private bool useWallJumpMoveStats;
    private bool isWallJumping;
    private float wallJumpTime;
    private bool isWallJumpFastFalling;
    private bool isWallJumpFalling;
    private float wallJumpFastFallTime;
    private float wallJumpFastFallReleaseSpeed;
    private float wallJumpPostBufferTimer;
    private float wallJumpApexPoint;
    private float timePastWallJumpApexThreshold;
    private bool isPastWallJumpApexThreshold;

    // Dash vars
    private bool isDashing;
    private bool isAirDashing;
    private float dashTimer;
    private float dashOnGroundTimer;
    private int numberOfDashesUsed;
    private Vector2 dashDirection;
    private bool isDashFastFalling;
    private float dashFastFallTime;
    private float dashFastFallReleaseSpeed;

    // Time control vars
    public float currentTimeSlowValue = 1f;
    public bool isTimeHalted = false;

    // Forced fast fall vars
    private bool isForcedFastFalling;

    private void Awake()
    {
        isFacingRight = true;
        rigidbody = GetComponent<Rigidbody2D>();
        particleScript = gameObject.GetComponent<PlayerParticleScript>();
        timeControlScript = gameObject.GetComponent<TimeControlScript>();
    }

    private void Update()
    {
        CountTimers();
        JumpChecks();
        LandCheck();
        WallJumpCheck();
        
        WallSlideCheck();
        DashCheck();
        CheckTimeControl();
        ForcedFastFallCheck();
    }

    private void FixedUpdate()
    {
        CollisionChecks();
        Jump();
        Fall();
        WallSlide();
        WallJump();
        Dash();
        ForcedFastFall();

        if (isGrounded)
        {
            Move(playerMovementStats.groundAcceleration, playerMovementStats.groundDeceleration, InputManager.movement);
        }
        else
        {
            // Wall jumping
            if (useWallJumpMoveStats)
            {
                Move(playerMovementStats.wallJumpMoveAcceleration, playerMovementStats.wallJumpMoveDeceleration, InputManager.movement);
            }
            // Airbone
            else
            {
                Move(playerMovementStats.airAcceleration, playerMovementStats.airDeceleration, InputManager.movement);
            }
        }

        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        // Clamp fall speed
        if (!isDashing)
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -playerMovementStats.maxFallSpeed, 50f);   
        }
        else if (isDashing || isForcedFastFalling)
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -50f, 50f);
        }

        rigidbody.linearVelocity = new Vector2(HorizontalVelocity, VerticalVelocity);
    }

    #region Movement

    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (!isDashing)
        {
            if (Mathf.Abs(moveInput.x) >= playerMovementStats.moveThreshold)
            {
                TurnCheck(moveInput);

                float targetVelocity = 0f;
                targetVelocity = moveInput.x * playerMovementStats.maxRunSpeed;

                HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            }
            else if (Mathf.Abs(moveInput.x) < playerMovementStats.moveThreshold)
            {
                HorizontalVelocity = Mathf.Lerp(HorizontalVelocity, 0f, deceleration * Time.fixedDeltaTime);
            }
        }
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (isFacingRight && moveInput.x < 0)
        {
            Turn(false);
        }
        else if (!isFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        if (turnRight)
        {
            isFacingRight = true;
            StartCoroutine(TurningAnimation(FACING_RIGHT, playerMovementStats.turningSpeed));
        }
        else
        {
            isFacingRight = false;
            StartCoroutine(TurningAnimation(FACING_LEFT, playerMovementStats.turningSpeed));
        }
    }

    #endregion

    #region Land/Fall

    private void LandCheck()
    {
        // Landed
        if ((isJumping || isFalling || isWallJumpFalling || isWallJumping || isWallSlideFalling || isWallSliding || isDashFastFalling) && isGrounded && VerticalVelocity <= 0f)
        {
            ResetJumpValues();
            StopWallSlide();
            ResetWallJumpValues();
            ResetDashes();
            isForcedFastFalling = false;

            numberOfJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;

            if (isDashFastFalling && isGrounded)
            {
                ResetDashValues();
                return;
            }

            ResetDashValues();
        }
    }

    private void Fall()
    {
        // Normal gravity while falling
        if (isFalling && !isGrounded && !isJumping && !isWallJumping && !isDashing && !isDashFastFalling)
        {
            VerticalVelocity += playerMovementStats.Gravity * Time.fixedDeltaTime;
        }
        else if (!isFalling && !isGrounded && !isJumping && !isWallJumping && !isDashing && !isDashFastFalling)
        {
            isFalling = true;
            VerticalVelocity += playerMovementStats.Gravity * Time.fixedDeltaTime;
        }
    }

    #endregion

    #region Jump

    private void ResetJumpValues()
    {
        isJumping = false;
        isFalling = false;
        isFastFalling = false;
        fastFallTime = 0f;
        isPastApexThreshold = false;
    }

    private void JumpChecks()
    {
        // When we press the jump button
        if (InputManager.jumpWasPressed)
        {
            if (isWallSlideFalling && wallJumpPostBufferTimer >= 0f)
            {
                return;
            }
            else if (isWallSliding || (isTouchingWall && !isGrounded))
            {
                return;
            }

            jumpBufferTimer = playerMovementStats.jumpBufferTime;
            jumpReleasedDuringBuffer = false;
        }        

        // When we release the jump button
        if (InputManager.jumpWasReleased && jumpBufferTimer > 0f)
        {
            jumpReleasedDuringBuffer = true;
        }
        else if (InputManager.jumpWasReleased && isJumping && VerticalVelocity > 0f && isPastApexThreshold)
        {
            isPastApexThreshold = false;
            isFastFalling = true;
            fastFallTime = playerMovementStats.timeForUpwardsCancel;
            VerticalVelocity = 0f;
        }
        else if (InputManager.jumpWasReleased && isJumping && VerticalVelocity > 0f && !isPastApexThreshold)
        {
            isFastFalling = true;
            fastFallReleaseSpeed = VerticalVelocity;
        }

        // Initate jump while considering jump buffer and coyote buffer
        if (jumpBufferTimer > 0f && !isJumping && !jumpReleasedDuringBuffer && (isGrounded || coyoteTimer > 0f))
        {
            InitiateJump(1);
        }
        else if (jumpBufferTimer > 0f && !isJumping && jumpReleasedDuringBuffer && (isGrounded || coyoteTimer > 0f))
        {
            isFastFalling = true;
            fastFallReleaseSpeed = VerticalVelocity;
        }

        // Handle additional jumps
        else if (jumpBufferTimer > 0f && (isJumping || isWallJumping || isWallSlideFalling || isDashFastFalling || isAirDashing) && !isTouchingWall && numberOfJumpsUsed < playerMovementStats.numberOfJumpsAllowed)
        {
            isFastFalling = false;
            InitiateJump(1);

            if (isDashFastFalling)
            {
                isDashFastFalling = false;
            }
        }
        
        // Air jump after coyote time lapsed
        else if (jumpBufferTimer > 0f && isFalling && !isWallSlideFalling && numberOfJumpsUsed < playerMovementStats.numberOfJumpsAllowed - 1)
        {
            InitiateJump(2); // We initiate 2 jumps to make sure the player only gets one air jump in the air after falling off a ledge and missing the coyote time window
            isFastFalling = false;
        }
    }

    private void InitiateJump(int numOfJumpsUsed)
    {
        if (!isJumping)
        {
            isJumping = true;
        }

        ResetWallJumpValues();

        jumpBufferTimer = 0f;
        numberOfJumpsUsed += numOfJumpsUsed;
        VerticalVelocity = playerMovementStats.InitialJumpVelocity;
        particleScript.EmitDoubleJumpParticles();
    }

    private void Jump()
    {
        // Apply gravity while jumping
        if (isJumping)
        {
            // Check for head bump
            if (bumpedHead)
            {
                isFastFalling = true;
            }

            // Gravity on ascending
            if (VerticalVelocity >= 0f)
            {
                // Apex controls
                apexPoint = Mathf.InverseLerp(playerMovementStats.InitialJumpVelocity, 0f, VerticalVelocity);

                if (apexPoint > playerMovementStats.apexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePastApexThreshold = 0f;
                    }

                    if (isPastApexThreshold)
                    {
                        timePastApexThreshold += Time.fixedDeltaTime;
                        if (timePastApexThreshold < playerMovementStats.apexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }
                    }
                }

                // Gravity on ascending but not past apex threshold
                else if (!isFastFalling)
                {
                    VerticalVelocity += playerMovementStats.Gravity * Time.fixedDeltaTime;
                    if (isPastApexThreshold)
                    {
                        isPastApexThreshold = false;
                    }
                }
            }

            // Gravity on descending
            else if (!isFastFalling)
            {
                VerticalVelocity += playerMovementStats.Gravity * playerMovementStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (VerticalVelocity > 0f && !isFalling)
            {
                isFalling = true;
            }
        }

        // Jump cut
        if (isFastFalling && fastFallTime >= playerMovementStats.timeForUpwardsCancel)
        {
            VerticalVelocity += playerMovementStats.Gravity * playerMovementStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
            fastFallTime += Time.fixedDeltaTime;
        }
        else if (isFastFalling && fastFallTime < playerMovementStats.timeForUpwardsCancel)
        {
            VerticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / playerMovementStats.timeForUpwardsCancel));
            fastFallTime += Time.fixedDeltaTime;
        }
    }

    #endregion

    #region Wall Slide

    private void WallSlideCheck()
    {
        if (isTouchingWall && !isGrounded && !isDashing && VerticalVelocity < 0f && !isWallSliding)
        {
            ResetJumpValues();
            ResetWallJumpValues();
            ResetDashValues();

            isWallSlideFalling = false;
            isWallSliding = true;

            if (playerMovementStats.resetDashOnWallSlide)
            {
                ResetDashes();
            }

            if (playerMovementStats.resetJumpsOnWallSlide)
            {
                numberOfJumpsUsed = 0;
            }
        }
        else if (isWallSliding && !isTouchingWall && !isGrounded && !isWallSlideFalling)
        {
            isWallSlideFalling = true;
            StopWallSlide();
        }
        else
        {
            StopWallSlide();
        }
    }

    private void StopWallSlide()
    {
        if (isWallSliding)
        {
            numberOfJumpsUsed++;
            isWallSliding = false;
        }
    }

    private void WallSlide()
    {
        if (isWallSliding)
        {
            print("**wall sliding speed active!");
            VerticalVelocity = Mathf.Lerp(VerticalVelocity, -playerMovementStats.wallSlideSpeed, playerMovementStats.wallSlideDecelerationSpeed * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region Wall Jump

    private void WallJumpCheck()
    {
        if (ShouldApplyPostWallJumpBuffer())
        {
            wallJumpPostBufferTimer = playerMovementStats.postWallJumpBufferTime;
        }

        // Wall jump fast falling
        if (InputManager.jumpWasReleased && isPastApexThreshold && !isWallSliding && !isTouchingWall && isWallJumping && VerticalVelocity > 0f)
        {
            isPastWallJumpApexThreshold = false;
            isWallJumpFastFalling = true;
            wallJumpFastFallTime = playerMovementStats.timeForUpwardsCancel;
        }
        else if (InputManager.jumpWasReleased && !isPastApexThreshold && !isWallSliding && !isTouchingWall && isWallJumping && VerticalVelocity > 0f)
        {
            isWallJumpFastFalling = true;
            wallJumpFastFallReleaseSpeed = VerticalVelocity;
        }

        // Actual jump with post wall jump buffer time
        if (InputManager.jumpWasPressed && wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();
        }
    }

    private void InitiateWallJump()
    {
        if (!isWallJumping)
        {
            isWallJumping = true;
            useWallJumpMoveStats = true;
        }

        StopWallSlide();
        ResetJumpValues();
        wallJumpTime = 0f;

        VerticalVelocity = playerMovementStats.InitialWallJumpVelocity;

        // Calculating horizontal velocity
        int dirMultiplier = 0;
        Vector2 hitPoint = lastWallHit.collider.ClosestPoint(bodyColl.bounds.center);

        // If the wall was behind you -> send you backwards
        if (hitPoint.x > transform.position.x)
        {
            dirMultiplier = -1;
        }
        // If the wall was in front of you -> send you away from it the other way
        else
        {
            dirMultiplier = 1;
        }

        HorizontalVelocity = Mathf.Abs(playerMovementStats.wallJumpDirection.x) * dirMultiplier;
    }

    private void WallJump()
    {
        // Apply wall jump gravity
        if (isWallJumping)
        {
            // Time to take over movement controls while wall jumping
            wallJumpTime += Time.fixedDeltaTime;
            if (wallJumpTime >= playerMovementStats.timeTillJumpApex)
            {
                useWallJumpMoveStats = false;
            }

            // Hit head
            if (bumpedHead)
            {
                isWallJumpFastFalling = true;
                useWallJumpMoveStats = false;
            }

            // Gravity in ascending
            if (VerticalVelocity >= 0f)
            {
                // Apex controls
                wallJumpApexPoint = Mathf.InverseLerp(playerMovementStats.wallJumpDirection.y, 0f, VerticalVelocity);

                if (wallJumpApexPoint > playerMovementStats.apexThreshold)
                {
                    if (!isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold = true;
                        timePastWallJumpApexThreshold = 0f;
                    }

                    if (isPastWallJumpApexThreshold)
                    {
                        timePastWallJumpApexThreshold += Time.fixedDeltaTime;
                        if (timePastWallJumpApexThreshold < playerMovementStats.apexHangTime)
                        {
                            VerticalVelocity = 0f;
                        }
                        else
                        {
                            VerticalVelocity = -0.01f;
                        }
                    }
                }

                // Gravity is ascending but not past apex threshold
                else if (!isWallJumpFastFalling)
                {
                    VerticalVelocity += playerMovementStats.WallJumpGravity * Time.fixedDeltaTime;

                    if (isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold = false;
                    }
                }
            }

            // Gravity descending
            else if (!isWallJumpFastFalling)
            {
                VerticalVelocity += playerMovementStats.WallJumpGravity * Time.fixedDeltaTime;
            }
            else if (VerticalVelocity < 0f && !isWallJumpFalling)
            {
                isWallJumpFalling = true;
            }
        }

        if (isWallJumpFastFalling)
        {
            if(wallJumpFastFallTime >= playerMovementStats.timeForUpwardsCancel)
            {
                VerticalVelocity += playerMovementStats.WallJumpGravity * playerMovementStats.wallJumpGravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (wallJumpFastFallTime < playerMovementStats.timeForUpwardsCancel)
            {
                VerticalVelocity = Mathf.Lerp(wallJumpFastFallReleaseSpeed, 0f, (wallJumpFastFallTime / playerMovementStats.timeForUpwardsCancel));
            }

            wallJumpFastFallTime += Time.fixedDeltaTime;
        }
    }

    private bool ShouldApplyPostWallJumpBuffer()
    {
        if (!isGrounded && (isTouchingWall || isWallSliding))
        {
            return true;
        }
        return false;
    }

    private void ResetWallJumpValues()
    {
        isWallSlideFalling = false;
        useWallJumpMoveStats = false;
        isWallJumping = false;
        isWallJumpFastFalling = false;
        isWallJumpFalling = false;
        isPastWallJumpApexThreshold = false;

        wallJumpFastFallTime = 0f;
        wallJumpTime = 0f;
    }

    #endregion

    #region Dash

    private void DashCheck()
    {
        if (InputManager.dashWasPressed)
        {
            // Ground dash
            if (isGrounded && dashOnGroundTimer < 0 && !isDashing)
            {
                particleScript.EmitDashTrail();
                InitateDash();
            }

            // Air dash
            else if (!isGrounded && !isDashing && numberOfDashesUsed < playerMovementStats.numberOfDashesAllowed)
            {
                isAirDashing = true;
                particleScript.EmitDashTrail();
                InitateDash();

                // You left a wallslide but dashed within the wall jump post buffer timer
                if (wallJumpPostBufferTimer > 0f)
                {
                    numberOfJumpsUsed--;
                    if (numberOfJumpsUsed < 0)
                    {
                        numberOfJumpsUsed = 0;
                    }
                }
            }
        }
    }

    private void InitateDash()
    {
        dashDirection = InputManager.movement;

        Vector2 closestDirection = Vector2.zero;
        float minDistance = Vector2.Distance(dashDirection, playerMovementStats.DashDirections[0]);

        for (int i = 0; i < playerMovementStats.DashDirections.Length; i++)
        {
            // If your controller movement was dead on, then use that direction
            if (dashDirection == playerMovementStats.DashDirections[i])
            {
                closestDirection = dashDirection;
                break;
            }

            float distance = Vector2.Distance(dashDirection, playerMovementStats.DashDirections[i]);

            // Check if this is a diagonal direction and apply bias if so (feels more precise with bias added)
            bool isDiagonal = (Mathf.Abs(playerMovementStats.DashDirections[i].x)  == 1 && Mathf.Abs(playerMovementStats.DashDirections[i].y) == 1);
            if (isDiagonal)
            {
                distance = playerMovementStats.dashDiagonallyBias;
            }
            else if (distance < minDistance)
            {
                minDistance = distance;
                closestDirection = playerMovementStats.DashDirections[i];
            }
        }

        // Handle direction with no input
        if (closestDirection == Vector2.zero)
        {
            if (isFacingRight)
            {
                closestDirection = Vector2.right;
            }
            else
            {
                closestDirection = Vector2.left;
            }
        }

        dashDirection = closestDirection;
        print("dashDirection: " + dashDirection);
        numberOfDashesUsed++;
        isDashing = true;
        dashTimer = 0f;
        dashOnGroundTimer = playerMovementStats.timeBtwDashesOnGround;

        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSlide();
    }

    private void Dash()
    {
        if (isDashing)
        {
            // Stop the dash after the timer
            dashTimer += Time.fixedDeltaTime;
            if (dashTimer >= playerMovementStats.dashTime)
            {
                if (isGrounded)
                {
                    ResetDashes();
                }

                isAirDashing = false;
                isDashing = false;

                if (!isJumping && !isWallJumping)
                {
                    dashFastFallTime = 0f;
                    dashFastFallReleaseSpeed = VerticalVelocity;

                    if (!isGrounded)
                    {
                        isDashFastFalling = true;
                    }
                }

                return;
            }

            HorizontalVelocity = playerMovementStats.dashSpeed * dashDirection.x;

            if (dashDirection.y != 0f || isAirDashing)
            {
                VerticalVelocity = playerMovementStats.dashSpeed * dashDirection.y;
            }
        }

        // Handle dash cut time
        else if (isDashFastFalling)
        {
            if (VerticalVelocity > 0f)
            {
                if (dashFastFallTime < playerMovementStats.dashTimeForUpwardsCancel)
                {
                    VerticalVelocity = Mathf.Lerp(dashFastFallReleaseSpeed, 0f, (dashFastFallTime / playerMovementStats.dashTimeForUpwardsCancel));
                }
                else if (dashFastFallTime >= playerMovementStats.dashTimeForUpwardsCancel)
                {
                    VerticalVelocity += playerMovementStats.Gravity * playerMovementStats.dashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }

                dashFastFallTime += Time.fixedDeltaTime;
            }
            else
            {
                VerticalVelocity += playerMovementStats.Gravity * playerMovementStats.dashGravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
        }
    }

    private void ResetDashValues()
    {
        isDashFastFalling = false;
        dashOnGroundTimer = -0.01f;
    }

    private void ResetDashes()
    {
        numberOfDashesUsed = 0;
    }

    #endregion

    #region Forced Fast Fall

    private void ForcedFastFallCheck()
    {
        print("InputManager.movement.y: " + InputManager.movement.y + " | -playerMovementStats.forcedFastFallMovementTriggerRange: " + -playerMovementStats.forcedFastFallMovementTriggerRange);
        if (!isGrounded && !isWallSliding && !isDashing && InputManager.movement.y <= -playerMovementStats.forcedFastFallMovementTriggerRange)
        {
            isForcedFastFalling = true;
        }
    }

    private void ForcedFastFall()
    {
        if (isForcedFastFalling)
        {
            VerticalVelocity += playerMovementStats.Gravity * playerMovementStats.gravityOnReleaseMultiplier * playerMovementStats.forcedFastFallSpeed * Time.fixedDeltaTime;
        }
    }

    #endregion

    #region Collision Checks

    private void CheckIfGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(feetColl.bounds.center.x, feetColl.bounds.min.y);
        Vector2 boxCastSize = new Vector2(feetColl.bounds.size.x, playerMovementStats.groundDetectionRayLength);

        groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, playerMovementStats.groundDetectionRayLength, playerMovementStats.groundLayer);
        if (groundHit.collider != null)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
        
        #region Debug isGrounded Visualization

        if (playerMovementStats.debugShowIsGroundedBox)
        {
            Color rayColor;
            if (isGrounded)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * playerMovementStats.groundDetectionRayLength, rayColor);
        }

        #endregion
    }

    private void CheckIfBumpedHead()
    {
        Vector2 boxCastOrigin = new Vector2(feetColl.bounds.center.x, bodyColl.bounds.max.y);
        Vector2 boxCastSize = new Vector2(feetColl.bounds.size.x * playerMovementStats.headWidth, playerMovementStats.headDetectionRayLength);

        headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, playerMovementStats.headDetectionRayLength, playerMovementStats.groundLayer);
        if (headHit.collider != null)
        {
            bumpedHead = true;
        }
        else
        {
            bumpedHead = false;
        }
        
        #region Debug bumpedHead Visualization

        if (playerMovementStats.debugShowHeadBumpBox)
        {
            float headWidth = playerMovementStats.headWidth;
            Color rayColor;
            if (bumpedHead)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y), Vector2.up * playerMovementStats.headDetectionRayLength, rayColor);
        }

        #endregion
    }

    private void IsTouchingWall()
    {
        float originEndPoint = 0f;
        if (isFacingRight)
        {
            originEndPoint = bodyColl.bounds.max.x;
        }
        else
        {
            originEndPoint = bodyColl.bounds.min.x;
        }

        float adjustedHeight = bodyColl.bounds.size.y * playerMovementStats.wallDetectionRayHeightMultiplier;

        Vector2 boxCastOrigin = new Vector2(originEndPoint, bodyColl.bounds.center.y);
        Vector2 boxCastSize = new Vector2(playerMovementStats.wallDetectionRayLength, adjustedHeight);

        wallHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, transform.right, playerMovementStats.wallDetectionRayLength, playerMovementStats.groundLayer);
        if (wallHit.collider != null)
        {
            lastWallHit = wallHit;
            isTouchingWall = true;
        }
        else
        {
            isTouchingWall = false;
        }

        #region Debug Visualization

        if (playerMovementStats.debugShowWallClingBox)
        {
            Color rayColor;
            if (isTouchingWall)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Vector2 boxBottomLeft = new Vector2(boxCastOrigin.x - boxCastSize.x /2, boxCastOrigin.y - boxCastSize.y /2);
            Vector2 boxBottomRight = new Vector2(boxCastOrigin.x + boxCastSize.x /2, boxCastOrigin.y - boxCastSize.y /2);
            Vector2 boxTopLeft = new Vector2(boxCastOrigin.x - boxCastSize.x /2, boxCastOrigin.y + boxCastSize.y /2);
            Vector2 boxTopRight = new Vector2(boxCastOrigin.x + boxCastSize.x /2, boxCastOrigin.y + boxCastSize.y /2);

            Debug.DrawLine(boxBottomLeft, boxBottomRight, rayColor);
            Debug.DrawLine(boxBottomRight, boxTopRight, rayColor);
            Debug.DrawLine(boxTopRight, boxTopLeft, rayColor);
            Debug.DrawLine(boxTopLeft, boxBottomLeft, rayColor);
        }

        #endregion
    }

    private void CollisionChecks()
    {
        CheckIfGrounded();
        CheckIfBumpedHead();
        IsTouchingWall();
    }


    #endregion

    #region Time Control

    private void CheckTimeControl()
    {
        if (InputManager.timeControlWasPressed && !GameManager.isTimeSlow && !isTimeHalted && currentTimeSlowValue > 0.05f)
        {
            timeControlScript.StartTimeSlow(0.35f, playerNumber);
        }
        else if (InputManager.timeControlWasPressed && GameManager.isTimeSlow)
        {
            timeControlScript.ReturnNormalTime(0.35f, playerNumber);
        }

        timeControlScript.UpdateTimeControlUI();
    }

    #endregion

    #region Timers

    private void CountTimers()
    {
        // Jump buffer
        jumpBufferTimer -= Time.deltaTime;

        // Jump Coyote time
        if (isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }
        else
        {
            coyoteTimer = playerMovementStats.jumpCoyoteTime;
        }

        // Wall jump buffer timer
        if (!ShouldApplyPostWallJumpBuffer())
        {
            wallJumpPostBufferTimer -= Time.deltaTime;
        }

        // Dash timer
        if (isGrounded)
        {
            dashOnGroundTimer -= Time.deltaTime;
        }
    }

    #endregion

    #region State Coroutines

    private IEnumerator TurningAnimation(Vector3 direction, float duration)
    {
        Vector3 originalRotation = gameObject.transform.rotation.eulerAngles;
        for(float i = 0; i < 1; i += Time.fixedDeltaTime / duration)
        {
            gameObject.transform.eulerAngles = Vector3.Lerp(originalRotation, direction, i);
            yield return null;
        }
        gameObject.transform.eulerAngles = direction;
    }

    #endregion
}
