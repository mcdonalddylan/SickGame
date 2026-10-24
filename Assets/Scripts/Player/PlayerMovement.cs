using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const float MaxUpwardSpeed = 50f;
    private const float MaxDashFallSpeed = 50f;
    private const float PostJumpCoyoteTime = 0.1f;

    [Header("References")]
    public PlayerMovementStats playerMovementStats;

    private new Rigidbody rigidbody;
    private PlayerParticleScript particleScript;
    private TimeControlScript timeControlScript;
    public PlayerStateDisplay stateDisplay;
    public int playerNumber = 1;

    // Movement vars
    [HideInInspector] public Vector2 velocity;
    public bool IsFacingRight { get; set; }
    public MovementController Controller { get; private set; }
    private static readonly Vector3 FACING_LEFT = new Vector3(0, 180, 0);
    private static readonly Vector3 FACING_RIGHT = Vector3.zero;

    // Input vars
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool dashPressed;

    // Jump vars
    private bool isJumping;
    public bool isFastFalling;
    public bool isFalling = false;
    private float fastFallTime;
    private float fastFallReleaseSpeed;
    private int numberOfJumpsUsed;
    public float fallTimer;

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
    public bool isWallSliding;
    private bool isWallSlideFalling;

    // Wall jump vars
    private bool useWallJumpMoveStats;
    public bool isWallJumping;
    private float wallJumpTime;
    private bool isWallJumpFastFalling;
    private bool isWallJumpFalling;
    private float wallJumpFastFallTime;
    private float wallJumpFastFallReleaseSpeed;
    private float wallJumpPostBufferTimer;
    private bool isWallJumpPostBufferCounting;
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
    public bool isForcedFastFalling;

    // Drop through one-way platform vars
    private float dropThroughHoldTimer;

    private void Awake()
    {
        IsFacingRight = true;
        rigidbody = GetComponent<Rigidbody>();
        ConfigureRigidbody();

        Controller = GetComponent<MovementController>();
        particleScript = gameObject.GetComponent<PlayerParticleScript>();
        timeControlScript = gameObject.GetComponent<TimeControlScript>();
    }

    // Kinematic body moved manually through MovePosition: the physics engine applies no forces,
    // which prevents jitter from conflicting physics and manual movement.
    // Continuous detection avoids missed collisions at high speed, interpolation smooths frames,
    // and all rotations are frozen (the turn animation rotates the transform directly).
    private void ConfigureRigidbody()
    {
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        CountTimers(Time.deltaTime);
        LogDebugStats();
        moveInput = InputManager.movement;
        if (InputManager.jumpWasPressed) jumpPressed = true;
        if (InputManager.jumpWasReleased) jumpReleased = true;
        if (InputManager.dashWasPressed) dashPressed = true;
    }

    private void FixedUpdate()
    {
        JumpChecks();
        LandCheck();
        WallJumpCheck();
        WallSlideCheck();
        DashCheck();
        CheckTimeControl();
        ForcedFastFallCheck();

        float timeStep = Time.fixedDeltaTime;
        HandleHorizontalMovement(timeStep);
        Jump(timeStep);
        WallSlide(timeStep);
        WallJump(timeStep);
        Dash(timeStep);
        Fall(timeStep);
        ForcedFastFall(timeStep);

        ClampVelocity();

        if (InputManager.dropThroughHeld && Controller.IsStandingOnOneWay)
        {
            dropThroughHoldTimer += timeStep;
            if (dropThroughHoldTimer >= playerMovementStats.dropThroughHoldTime)
            {
                Controller.DropThroughOneWay();
                dropThroughHoldTimer = 0f;
            }
        }
        else
        {
            dropThroughHoldTimer = 0f;
        }

        Controller.Move(new Vector3(velocity.x, velocity.y, 0f) * timeStep);

        // Reset input bools
        jumpPressed = false;
        jumpReleased = false;
        dashPressed = false;
    }

    private void ClampVelocity()
    {
        // Dashes can exceed the normal fall speed cap
        float minVelocityY = isDashing ? -MaxDashFallSpeed : -playerMovementStats.maxFallSpeed;
        velocity.y = Mathf.Clamp(velocity.y, minVelocityY, MaxUpwardSpeed);
    }

    #region Movement

    private void HandleHorizontalMovement(float timeStep)
    {
        if (!isDashing)
        {
            TurnCheck(moveInput);
            float targetVelocityX = 0f;

            if (Mathf.Abs(moveInput.x) >= playerMovementStats.moveThreshold)
            {
                float moveDirection = Mathf.Sign(moveInput.x);
                targetVelocityX = playerMovementStats.maxRunSpeed * moveDirection;
            }

            float acceleration = Controller.IsGrounded() ? playerMovementStats.groundAcceleration : playerMovementStats.airAcceleration;
            float deceleration = Controller.IsGrounded() ? playerMovementStats.groundDeceleration : playerMovementStats.airDeceleration;

            if (useWallJumpMoveStats)
            {
                acceleration = playerMovementStats.wallJumpMoveAcceleration;
                deceleration = playerMovementStats.wallJumpMoveDeceleration;
            }

            if (Mathf.Abs(moveInput.x) >= playerMovementStats.moveThreshold)
            {
                velocity.x = Mathf.Lerp(velocity.x, targetVelocityX, acceleration * timeStep);
            }
            else
            {
                velocity.x = Mathf.Lerp(velocity.x, 0, deceleration * timeStep);
            }
        }
    }

    private void TurnCheck(Vector2 moveInput)
    {
        if (IsFacingRight && moveInput.x < 0)
        {
            Turn(false);
        }
        else if (!IsFacingRight && moveInput.x > 0)
        {
            Turn(true);
        }
    }

    private void Turn(bool turnRight)
    {
        IsFacingRight = turnRight;
        StartCoroutine(TurningAnimation(turnRight ? FACING_RIGHT : FACING_LEFT, playerMovementStats.turningSpeed));
    }

    #endregion

    #region Land/Fall

    private void LandCheck()
    {
        // Check if currently grounded
        if (Controller.IsGrounded() && velocity.y <= 0f)
        {
            // Only reset states if we were actually in a falling/jumping/wall state
            if (isJumping || isFalling || isWallJumpFalling || isWallJumping || isWallSlideFalling || isWallSliding || isDashFastFalling)
            {
                ResetJumpValues();
                StopWallSlide();
                ResetWallJumpValues();
                ResetDashes();
                isForcedFastFalling = false;

                numberOfJumpsUsed = 0;

                if (isDashFastFalling)
                {
                    ResetDashValues();
                    return;
                }

                ResetDashValues();
            }
            else
            {
                // Standing on ground but not resetting - just ensure isFalling is false
                isFalling = false;
            }
        }
        else if (!Controller.IsGrounded())
        {
            // In the air and not jumping/wall jumping/dashing - we're falling
            if (!isJumping && !isWallJumping && !isDashing && !isDashFastFalling && !isWallSliding)
            {
                isFalling = true;
            }
        }
    }

    private void Fall(float timeStep)
    {
        // Don't apply gravity if grounded (LandCheck handles resetting isFalling)
        if (Controller.IsGrounded())
        {
            return;
        }

        // Normal gravity while falling
        if (isFalling && !isJumping && !isWallJumping && !isDashing && !isDashFastFalling)
        {
            velocity.y += playerMovementStats.Gravity * Time.fixedDeltaTime;
        }
        else if (!isFalling && !isJumping && !isWallJumping && !isDashing && !isDashFastFalling)
        {
            isFalling = true;
            velocity.y += playerMovementStats.Gravity * timeStep;
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
        if (jumpPressed)
        {
            if (isWallSlideFalling && wallJumpPostBufferTimer >= 0f)
            {
                return;
            }
            else if (isWallSliding || (Controller.IsTouchingWall(IsFacingRight) && !Controller.IsGrounded()))
            {
                return;
            }

            jumpBufferTimer = playerMovementStats.jumpBufferTime;
            jumpReleasedDuringBuffer = false;
        }        

        // When we release the jump button
        if (jumpReleased && jumpBufferTimer > 0f)
        {
            jumpReleasedDuringBuffer = true;
        }
        else if (jumpReleased && isJumping && velocity.y > 0f && isPastApexThreshold)
        {
            isPastApexThreshold = false;
            isFastFalling = true;
            fastFallTime = playerMovementStats.timeForUpwardsCancel;
            velocity.y = 0f;
        }
        else if (jumpReleased && isJumping && velocity.y > 0f && !isPastApexThreshold)
        {
            isFastFalling = true;
            fastFallReleaseSpeed = velocity.y;
        }

        // Initate jump while considering jump buffer and coyote buffer
        if (jumpBufferTimer > 0f && !isJumping && !jumpReleasedDuringBuffer && (Controller.IsGrounded() || coyoteTimer > 0f))
        {
            InitiateJump(1);
        }
        else if (jumpBufferTimer > 0f && !isJumping && jumpReleasedDuringBuffer && (Controller.IsGrounded() || coyoteTimer > 0f))
        {
            isFastFalling = true;
            fastFallReleaseSpeed = velocity.y;
        }

        // Handle additional jumps
        else if (jumpBufferTimer > 0f && (isJumping || isWallJumping || isWallSlideFalling || isDashFastFalling || isAirDashing) && !Controller.IsTouchingWall(IsFacingRight) && numberOfJumpsUsed < playerMovementStats.numberOfJumpsAllowed)
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
        coyoteTimer = PostJumpCoyoteTime; // Small grace window if the player walks off a ledge after jumping
        numberOfJumpsUsed += numOfJumpsUsed;
        velocity.y = playerMovementStats.InitialJumpVelocity;
        particleScript.EmitDoubleJumpParticles();
    }

    private void Jump(float timeStep)
    {
        // Apply gravity while jumping
        if (isJumping)
        {
            // Head bump: drop the upward momentum and fall under gravity right away.
            // Velocity must go slightly negative: at exactly 0 the apex branch below would hold the player in place.
            if (Controller.BumpedHead() && velocity.y > 0f)
            {
                isFastFalling = true;
                isPastApexThreshold = false;
                fastFallTime = playerMovementStats.timeForUpwardsCancel;
                velocity.y = -0.01f;
            }

            // Gravity on ascending
            if (velocity.y >= 0f)
            {
                // Apex controls
                apexPoint = Mathf.InverseLerp(playerMovementStats.InitialJumpVelocity, 0f, velocity.y);

                if (apexPoint > playerMovementStats.apexThreshold)
                {
                    if (!isPastApexThreshold)
                    {
                        isPastApexThreshold = true;
                        timePastApexThreshold = 0f;
                    }

                    if (isPastApexThreshold)
                    {
                        timePastApexThreshold += timeStep;
                        if (timePastApexThreshold < playerMovementStats.apexHangTime)
                        {
                            velocity.y = 0f;
                        }
                        else
                        {
                            velocity.y = -0.01f;
                        }
                    }
                }

                // Gravity on ascending but not past apex threshold
                else if (!isFastFalling)
                {
                    velocity.y += playerMovementStats.Gravity * timeStep;
                    if (isPastApexThreshold)
                    {
                        isPastApexThreshold = false;
                    }
                }
            }

            // Gravity on descending
            else if (!isFastFalling)
            {
                velocity.y += playerMovementStats.Gravity * playerMovementStats.gravityOnReleaseMultiplier * timeStep;
            }
            else if (velocity.y > 0f && !isFalling)
            {
                isFalling = true;
            }
        }

        // Jump cut
        if (isFastFalling && fastFallTime >= playerMovementStats.timeForUpwardsCancel)
        {
            velocity.y += playerMovementStats.Gravity * playerMovementStats.gravityOnReleaseMultiplier * timeStep;
            fastFallTime += timeStep;
        }
        else if (isFastFalling && fastFallTime < playerMovementStats.timeForUpwardsCancel)
        {
            velocity.y = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / playerMovementStats.timeForUpwardsCancel));
            fastFallTime += timeStep;
        }
    }

    #endregion

    #region Wall Slide

    private void WallSlideCheck()
    {
        bool touchingWall = Controller.IsTouchingWall(IsFacingRight);
        bool grounded = Controller.IsGrounded();

        if (!isWallSliding)
        {
            // Start sliding when falling while pressed against a wall
            if (touchingWall && !grounded && !isDashing && velocity.y < 0f)
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
        }
        else if (grounded || isDashing)
        {
            StopWallSlide();
        }
        else if (!touchingWall)
        {
            // Slid off the wall (or turned away from it)
            isWallSlideFalling = true;
            StopWallSlide();
        }
        // Otherwise: still touching the wall in the air, so keep sliding
    }

    private void StopWallSlide()
    {
        if (isWallSliding)
        {
            numberOfJumpsUsed++;
            isWallSliding = false;
        }
    }

    private void WallSlide(float timeStep)
    {
        if (isWallSliding)
        {
            velocity.y = Mathf.Lerp(velocity.y, -playerMovementStats.wallSlideSpeed, playerMovementStats.wallSlideDecelerationSpeed * timeStep);
        }
    }

    #endregion

    #region Wall Jump

    private void WallJumpCheck()
    {
        if (Controller.IsGrounded() || isWallSliding || (Controller.IsTouchingWall(IsFacingRight) && !isWallJumping))
        {
            wallJumpPostBufferTimer = playerMovementStats.postWallJumpBufferTime;
            isWallJumpPostBufferCounting = false;
        }

        // Wall jump fast falling
        if (jumpReleased && isPastApexThreshold && !isWallSliding && !Controller.IsTouchingWall(IsFacingRight) && isWallJumping && velocity.y > 0f)
        {
            isPastWallJumpApexThreshold = false;
            isWallJumpFastFalling = true;
            wallJumpFastFallTime = playerMovementStats.timeForUpwardsCancel;
        }
        else if (jumpReleased && !isPastApexThreshold && !isWallSliding && !Controller.IsTouchingWall(IsFacingRight) && isWallJumping && velocity.y > 0f)
        {
            isWallJumpFastFalling = true;
            wallJumpFastFallReleaseSpeed = velocity.y;
        }

        // Actual jump with post wall jump buffer time
        if (jumpPressed && wallJumpPostBufferTimer > 0f && CanWallJump())
        {
            InitiateWallJump();
        }
    }

    // A wall jump needs wall contact in the air (touching, sliding, or having just slid off).
    // The buffer timer sits at its default on the ground, so it alone must never trigger a wall jump.
    private bool CanWallJump()
    {
        return !Controller.IsGrounded()
            && (isWallSliding || isWallSlideFalling || Controller.IsTouchingWall(IsFacingRight));
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
        wallJumpPostBufferTimer = playerMovementStats.postWallJumpBufferTime;
        isWallJumpPostBufferCounting = true;

        velocity.y = playerMovementStats.InitialWallJumpVelocity;
        velocity.x = Mathf.Abs(playerMovementStats.wallJumpDirection.x) * -Controller.GetWallDirection();
    }

    private void WallJump(float timeStep)
    {
        // Apply wall jump gravity
        if (isWallJumping)
        {
            // Time to take over movement controls while wall jumping
            wallJumpTime += timeStep;
            if (wallJumpTime >= playerMovementStats.timeTillJumpApex)
            {
                useWallJumpMoveStats = false;
            }

            // Hit head: stop rising and fall right away (slightly negative so the apex hang doesn't hold the player)
            if (Controller.BumpedHead() && velocity.y > 0f)
            {
                isWallJumpFastFalling = true;
                isPastWallJumpApexThreshold = false;
                wallJumpFastFallTime = playerMovementStats.timeForUpwardsCancel;
                useWallJumpMoveStats = false;
                velocity.y = -0.01f;
            }

            // Gravity in ascending
            if (velocity.y >= 0f)
            {
                // Apex controls
                wallJumpApexPoint = Mathf.InverseLerp(playerMovementStats.wallJumpDirection.y, 0f, velocity.y);

                if (wallJumpApexPoint > playerMovementStats.apexThreshold)
                {
                    if (!isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold = true;
                        timePastWallJumpApexThreshold = 0f;
                    }

                    if (isPastWallJumpApexThreshold)
                    {
                        timePastWallJumpApexThreshold += timeStep;
                        if (timePastWallJumpApexThreshold < playerMovementStats.apexHangTime)
                        {
                            velocity.y = 0f;
                        }
                        else
                        {
                            velocity.y = -0.01f;
                        }
                    }
                }

                // Gravity is ascending but not past apex threshold
                else if (!isWallJumpFastFalling)
                {
                    velocity.y += playerMovementStats.WallJumpGravity * timeStep;

                    if (isPastWallJumpApexThreshold)
                    {
                        isPastWallJumpApexThreshold = false;
                    }
                }
            }

            // Gravity descending
            else if (!isWallJumpFastFalling)
            {
                velocity.y += playerMovementStats.WallJumpGravity * timeStep;
            }
            else if (velocity.y < 0f && !isWallJumpFalling)
            {
                isWallJumpFalling = true;
            }
        }

        if (isWallJumpFastFalling)
        {
            if(wallJumpFastFallTime >= playerMovementStats.timeForUpwardsCancel)
            {
                velocity.y += playerMovementStats.WallJumpGravity * playerMovementStats.wallJumpGravityOnReleaseMultiplier * timeStep;
            }
            else if (wallJumpFastFallTime < playerMovementStats.timeForUpwardsCancel)
            {
                velocity.y = Mathf.Lerp(wallJumpFastFallReleaseSpeed, 0f, (wallJumpFastFallTime / playerMovementStats.timeForUpwardsCancel));
            }

            wallJumpFastFallTime += timeStep;
        }
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
        if (dashPressed)
        {
            // Ground dash
            if (Controller.IsGrounded() && dashOnGroundTimer < 0 && !isDashing)
            {
                particleScript.EmitDashTrail();
                InitateDash();
            }

            // Air dash
            else if (!Controller.IsGrounded() && !isDashing && numberOfDashesUsed < playerMovementStats.numberOfDashesAllowed)
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
        dashDirection = moveInput;

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
            if (IsFacingRight)
            {
                closestDirection = Vector2.right;
            }
            else
            {
                closestDirection = Vector2.left;
            }
        }

        dashDirection = closestDirection;
        numberOfDashesUsed++;
        isDashing = true;
        dashTimer = 0f;
        dashOnGroundTimer = playerMovementStats.timeBtwDashesOnGround;

        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSlide();
    }

    private void Dash(float timeStep)
    {
        if (isDashing)
        {
            // Stop the dash after the timer
            dashTimer += timeStep;
            if (dashTimer >= playerMovementStats.dashTime)
            {
                if (Controller.IsGrounded())
                {
                    ResetDashes();
                }

                isAirDashing = false;
                isDashing = false;

                if (!isJumping && !isWallJumping)
                {
                    dashFastFallTime = 0f;
                    dashFastFallReleaseSpeed = velocity.y;

                    if (!Controller.IsGrounded())
                    {
                        isDashFastFalling = true;
                    }
                }

                return;
            }

            velocity.x = playerMovementStats.dashSpeed * dashDirection.x;

            if (dashDirection.y != 0f || isAirDashing)
            {
                velocity.y = playerMovementStats.dashSpeed * dashDirection.y;
            }
        }

        // Handle dash cut time
        else if (isDashFastFalling)
        {
            if (velocity.y > 0f)
            {
                if (dashFastFallTime < playerMovementStats.dashTimeForUpwardsCancel)
                {
                    velocity.y = Mathf.Lerp(dashFastFallReleaseSpeed, 0f, (dashFastFallTime / playerMovementStats.dashTimeForUpwardsCancel));
                }
                else if (dashFastFallTime >= playerMovementStats.dashTimeForUpwardsCancel)
                {
                    velocity.y += playerMovementStats.Gravity * playerMovementStats.dashGravityOnReleaseMultiplier * timeStep;
                }

                dashFastFallTime += timeStep;
            }
            else
            {
                velocity.y += playerMovementStats.Gravity * playerMovementStats.dashGravityOnReleaseMultiplier * timeStep;
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
        // Reset on the ground, and when jumping/air dashing/wall sliding interrupts it so it can only be re-triggered once falling again
        if (Controller.IsGrounded() || isDashing || isWallSliding || velocity.y > 0f)
        {
            isForcedFastFalling = false;
            return;
        }

        // Only begin while in the air and actually falling
        if (!isForcedFastFalling && velocity.y < 0f && moveInput.y <= -playerMovementStats.forcedFastFallMovementTriggerRange)
        {
            isForcedFastFalling = true;
        }
    }

    private void ForcedFastFall(float timeStep)
    {
        if (isForcedFastFalling)
        {
            velocity.y += playerMovementStats.Gravity * playerMovementStats.gravityOnReleaseMultiplier * playerMovementStats.forcedFastFallSpeed * timeStep;
        }
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

    private void CountTimers(float timeStep)
    {
        // Jump buffer
        jumpBufferTimer -= timeStep;

        // Jump Coyote time
        // Refills while grounded, only counts down while falling/fast falling (not while wall sliding or rising),
        // and is held in every other airborne state so it can't expire before the player starts falling
        if (Controller.IsGrounded())
        {
            coyoteTimer = playerMovementStats.jumpCoyoteTime;
        }
        else if ((isFalling || isFastFalling) && !isWallSliding)
        {
            coyoteTimer -= timeStep;
        }

        // Wall jump buffer timer
        if (isWallJumpPostBufferCounting)
        {
            wallJumpPostBufferTimer -= timeStep;
        }

        // Dash timer
        if (Controller.IsGrounded())
        {
            dashOnGroundTimer -= timeStep;
        }

        // Falling timer
        if (isFalling || isFastFalling || isWallJumpFalling || isWallJumpFastFalling || isDashFastFalling || isForcedFastFalling)
        {
            fallTimer += timeStep;
        }
        else
        {
            fallTimer = 0;
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

    #region Debug Stats

    private void LogDebugStats()
    {
        stateDisplay.isFacingRight = IsFacingRight;
        stateDisplay.isGrounded = Controller.IsGrounded();
        stateDisplay.isTouchingWall = Controller.IsTouchingWall(IsFacingRight);
        stateDisplay.bumpedHead = Controller.BumpedHead();
        stateDisplay.isJumping = isJumping;
        stateDisplay.isWallJumping = isWallJumping;
        stateDisplay.wallJumpPostBufferTimer = wallJumpPostBufferTimer;
        stateDisplay.isWallSliding = isWallSliding;
        stateDisplay.isWallSlideFalling = isWallSlideFalling;
        stateDisplay.isFalling = isFalling;
        stateDisplay.isFastFalling = isFastFalling;
        stateDisplay.isForcedFastFalling = isForcedFastFalling;
        stateDisplay.isDashing = isDashing;
        stateDisplay.fallTimer = fallTimer;
        stateDisplay.coyoteTimer = coyoteTimer;
        stateDisplay.dropThroughHoldTimer = dropThroughHoldTimer;
    }

    #endregion
}
