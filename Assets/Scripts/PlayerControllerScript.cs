using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerControllerScript : MonoBehaviour
{
    private Vector3 FACING_LEFT = new Vector3(0, 180, 0);
    private Vector3 FACING_RIGHT = Vector3.zero;
    private float MAX_ACCELERATION = 2f;

    private float playerSpeed = 10.0f;
    private float smallJumpHeight = 0.3f;
    private float doubleJumpHeight = 0.7f;
    private float longJumpHeight = 0.8f;
    private float gravityValue = 12.5f;

    public int playerNumber = 1;
    public bool timeHalted = false;
    private CharacterController controller;
    private PlayerParticleScript particleScript;
    private TimeControlScript timeControlScript;
    public float playerYVelocity;
    public float playerXVelocity;
    private float decelerationDirection = -1;
    private float decelerationVelocity;
    private float inAirTimer; // needed to determine when landing 
    private bool groundedPlayer;
    private float groundedTimer; // needed to allow player to jump while going down ramp
    private bool smallJump;
    private bool airDash;
    private bool triggeredFirstJump;
    private bool triggeredSmallJump;
    private bool longJump;
    private bool doubleJump;
    private float horizontalAxis;
    private int horizontalDirection;
    private float verticalAxis;
    private float verticalDirection;

    // Player state variables
    public int damage = 0;
    public float currentTimeSlowValue = 1;
    public bool faceRightState = true;
    public bool turningAroundState;
    private bool airDashState;
    private bool hitLagState;
    private bool hitStunState;
    private bool walkState;
    private bool runningState;
    private bool sprintState;
    private bool crouchState;
    private bool jumpSquatState;
    private bool jumpState;
    private bool doubleJumpState;
    private bool fallState;
    public bool attackState;

    private bool emitLandingParticles = false;
    private bool emitDashParticles = false;

    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        particleScript = gameObject.GetComponent<PlayerParticleScript>();
        timeControlScript = gameObject.GetComponent<TimeControlScript>();
        FindCheckpointAndSpawn();
    }

    private void FindCheckpointAndSpawn()
    {
        GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
        GameObject checkpoint = new GameObject();
        for (int i = 0; i < checkpoints.Length; i++)
        {
            print(checkpoints[i].GetComponent<CheckpointScript>().checkpointNumber == GameManager.playerCheckpoint);
            if (checkpoints[i].GetComponent<CheckpointScript>().checkpointNumber == GameManager.playerCheckpoint)
            {
                checkpoint = checkpoints[i];
            }
        }
        gameObject.transform.localPosition = checkpoint.transform.position;
        Physics.SyncTransforms();  // needed to avoid a bug where the player doesn't spawn every time you load the level
    }

    void Update()
    {
        groundedPlayer = controller.isGrounded;
        if (!groundedPlayer)
        {
            inAirTimer += Time.deltaTime;
        }
        else
        {
            groundedTimer = 0.2f;
        }

        if (groundedTimer > 0)
        {
            groundedTimer -= Time.deltaTime;
        }

        if (groundedPlayer && playerYVelocity < 0)
        {
            playerYVelocity = 0f;
            jumpState = false;
            doubleJumpState = false;
            airDashState = false;
        }

        if (!jumpState && !doubleJumpState && !(groundedTimer > 0) && playerYVelocity < -0.01f)
        {
            fallState = true;
        }
        else
        {
            fallState = false;
        }

        triggeringFirstJump();

        // Jump velocity change
        if (smallJump && groundedTimer > 0)
        {
            groundedTimer = 0;
            smallJump = false;
            playerYVelocity += Mathf.Sqrt(smallJumpHeight * -1.0f * -gravityValue);
        }
        else if (longJump && groundedTimer > 0)
        {
            groundedTimer = 0;
            longJump = false;
            playerYVelocity += Mathf.Sqrt(longJumpHeight * -1.0f * -gravityValue);
        }

        // Double jump
        if ((jumpState || fallState) && doubleJump)
        {
            groundedTimer = 0;
            doubleJump = false;
            doubleJumpState = true;
            particleScript.EmitDoubleJumpParticles();
            playerYVelocity += Mathf.Sqrt(doubleJumpHeight * -1.0f * -gravityValue);
        }

        // Air dash velocity change
        if (airDash)
        {
            particleScript.EmitDashTrail();
            airDash = false;
            playerYVelocity += 3.5f * verticalDirection;
            playerXVelocity += 2.5f * horizontalDirection;
            airDashState = true;
        }

        // Gravity when either holding down or not
        if (playerYVelocity <= 0 && verticalAxis <= -0.6f)
        {
            // Fastfalling: if holding down while falling, you fall faster
            playerYVelocity -= (gravityValue + 30) * Time.deltaTime;
        }
        else
        {
            playerYVelocity -= gravityValue * Time.deltaTime;
        }

        // Restricts momentum so that you won't reverse directions upon slowing down
        if(jumpState && faceRightState && horizontalAxis <= 0)
        {
            playerXVelocity += (horizontalAxis * Time.deltaTime) * 4.5f;
        }
        else if (jumpState && !faceRightState && horizontalAxis >= 0)
        {
            playerXVelocity += (horizontalAxis * Time.deltaTime) * 4.5f;
        }
        else if (airDashState)
        {
            //Debug.LogError("SHOULD NOT BE DECLERATING");
        }
        else
        {
            playerXVelocity += (decelerationDirection * Time.deltaTime) * 6f;
            if (playerXVelocity < 0 && faceRightState)
            {
                playerXVelocity = 0;
            }
            else if (playerXVelocity > 0 && !faceRightState)
            {
                playerXVelocity = 0;
            }
            playerXVelocity += (horizontalAxis * Time.deltaTime) * 17f;
        }

        // Handling horizontal acceleration / momentum
        if(playerXVelocity > MAX_ACCELERATION && faceRightState)
        {
            playerXVelocity = MAX_ACCELERATION;
        }
        else if (playerXVelocity < -MAX_ACCELERATION && !faceRightState)
        {
            playerXVelocity = -MAX_ACCELERATION;
        }
        else if (airDashState)
        {
            //Debug.LogError("No Acceleration limit while air dashing");
        }

        Vector3 move = new Vector3(playerXVelocity, playerYVelocity, 0);

        // Can move faster left and right while air borne
        if (jumpState)
        {
            move = new Vector3(playerXVelocity * 0.9f, playerYVelocity, 0);
        }

        //----------------
        // MOVE FUNCTION |
        //----------------
        CollisionFlags collisionFlags = controller.Move(move * Time.deltaTime * playerSpeed);

        // If player facing left...
        if (groundedPlayer && !jumpSquatState && horizontalAxis < -0.2f && faceRightState)
        {
            StartCoroutine(TurningAnimation(FACING_LEFT, 0.05f));
            faceRightState = false;
            decelerationDirection = 1;
        }
        // If player facing right...
        else if (groundedPlayer && !jumpSquatState && horizontalAxis > 0.2f && !faceRightState)
        {
            StartCoroutine(TurningAnimation(FACING_RIGHT, 0.05f));
            faceRightState = true;
            decelerationDirection = -1;
        }

        if(!emitDashParticles && groundedPlayer && Mathf.Abs(horizontalAxis) > 0.9f)
        {
            emitDashParticles = true;
            particleScript.EmitRunningAndJumpingParticles();
        }
        else if (Mathf.Abs(horizontalAxis) < 0.1f)
        {
            emitDashParticles = false;
        }    

        if (!emitLandingParticles && groundedPlayer && inAirTimer > 0.01f)
        {
            if(Mathf.Abs(horizontalAxis) < 0.15f)
            {
                playerXVelocity = 0;
            }
            particleScript.EmitLandingParticles();
            emitLandingParticles = true;
            inAirTimer = 0;
        }

        // If Collides with wall -> reduce xVelocity to 0
        if((collisionFlags & (CollisionFlags.Sides)) != 0)
        {
            //Debug.LogError("HAS HIT WALL");
            playerXVelocity = 0;
        }

        // If Collides with ceiling -> reduce yVelocity to 0
        if ((collisionFlags & (CollisionFlags.Above)) != 0)
        {
            //Debug.LogError("HAS HIT CEILING");
            playerYVelocity -= gravityValue * Time.deltaTime;
        }

        print("fallState : " + fallState);
        //print(" --- end of Update() this frame ----");
    }

    //-------------
    // INPUT EVENTS
    //-------------

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && groundedTimer > 0 && !jumpState && !fallState && !doubleJump)
        {
            //Debug.LogWarning("PRESSED JUMP**");
            StartCoroutine(JumpSquatAnimation(0.07f));
        }
        else if (context.performed && !doubleJumpState)
        {
            //Debug.LogWarning("PRESSED JUMP**");
            playerYVelocity = 0;
            doubleJump = true;
        }
        if (context.canceled && jumpSquatState && !doubleJumpState && groundedTimer > 0)
        {
            triggeredSmallJump = true;
        }
    }

    public void HorizontalAxis(InputAction.CallbackContext context)
    {
        horizontalAxis = context.ReadValue<float>();
        if (horizontalAxis > 0)
        {
            horizontalDirection = 1;
        }
        else if (horizontalAxis < 0)
        {
            horizontalDirection = -1;
        }
        else
        {
            horizontalDirection = 0;
        }
    }

    public void VerticalAxis(InputAction.CallbackContext context)
    {
        verticalAxis = context.ReadValue<float>();
        if (verticalAxis >= 0.15f)
        {
            verticalDirection = 1;
        }
        else if (verticalAxis <= -0.25f)
        {
            verticalDirection = -0.3f;
        }
        else
        {
            verticalDirection = 0.35f;
        }
    }

    public void AirDash(InputAction.CallbackContext context)
    {
        if (context.performed && jumpState)
        {
            //Debug.LogWarning("PRESSED AIR DASH");
            playerYVelocity = 0;
            airDash = true;
        }
    }

    public void TimeControl(InputAction.CallbackContext context)
    {
        if (context.performed && !GameManager.isTimeSlow && !timeHalted && currentTimeSlowValue > 0.05f)
        {
            timeControlScript.StartTimeSlow(0.35f, playerNumber);
        }
        else if (context.performed && GameManager.isTimeSlow)
        {
            timeControlScript.ReturnNormalTime(0.35f, playerNumber);
        }
    }


    //-----------------
    // STATE COROUTINES
    //-----------------

    private IEnumerator TurningAnimation(Vector3 direction, float duration)
    {
        Vector3 originalRotation = gameObject.transform.rotation.eulerAngles;
        for(float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            turningAroundState = true;
            gameObject.transform.eulerAngles = Vector3.Lerp(originalRotation, direction, i);
            yield return null;
        }
        gameObject.transform.eulerAngles = direction;
        turningAroundState = false;
    }

    private IEnumerator JumpSquatAnimation(float duration)
    {
        for (float i = 0; i < 1; i += Time.deltaTime / duration)
        {
            jumpSquatState= true;
            yield return null;
        }
        jumpSquatState = false;
        triggeredFirstJump = true;
    }

    private void triggeringFirstJump()
    {
        if (triggeredSmallJump && triggeredFirstJump)
        {
            triggeredFirstJump = false;
            longJump = false;
            jumpState = true;
            smallJump = true;
            emitLandingParticles = false;
            particleScript.EmitRunningAndJumpingParticles();
            inAirTimer = 0;
            triggeredSmallJump = false;
        }
        else if (!triggeredSmallJump && triggeredFirstJump)
        {
            triggeredFirstJump = false;
            smallJump = false;
            jumpState = true;
            longJump = true;
            emitLandingParticles = false;
            particleScript.EmitRunningAndJumpingParticles();
            inAirTimer = 0;
            triggeredSmallJump = false;
        }
    }
}
