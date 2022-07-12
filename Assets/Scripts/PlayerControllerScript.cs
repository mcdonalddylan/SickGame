using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerControllerScript : MonoBehaviour
{
    private Vector3 FACING_LEFT = new Vector3(0, 180, 0);
    private Vector3 FACING_RIGHT = Vector3.zero;

    private float playerSpeed = 10.0f;
    private float smallJumpHeight = 0.3f;
    private float longJumpHeight = 0.8f;
    private float gravityValue = -12.5f;

    private CharacterController controller;
    private PlayerParticleScript particleScript;
    private float playerYVelocity;
    private float inAirTimer; // needed to determine when landing 
    private bool groundedPlayer;
    private bool smallJump;
    private bool triggeredSmallJump;
    private bool longJump;
    private float horizontalAxis;
    private float verticalAxis;

    // Player state variables
    public static int damage = 0;
    public bool faceRightState = true;
    public static bool hitLagState;
    public static bool hitStunState;
    public static bool walkState;
    public static bool runningState;
    public static bool sprintState;
    public static bool crouchState;
    public static bool jumpSquatState;
    public static bool jumpState;
    public static bool doubleJumpState;
    public static bool turningAroundState = false;
    public static bool attackState;

    private bool emitLandingParticles = false;
    private bool emitDashParticles = false;

    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        particleScript = gameObject.GetComponent<PlayerParticleScript>();
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
        Physics.SyncTransforms();  // needed to avoid a bug where the player doesn't spawn
    }

    void Update()
    {
        groundedPlayer = controller.isGrounded;
        if (!groundedPlayer)
        {
            inAirTimer += Time.deltaTime;
        }
        else if (groundedPlayer)
        {
            playerYVelocity = 0f;
            jumpState = false;
        }

        // Jump velocity change
        if (smallJump && groundedPlayer)
        {
            smallJump = false;
            playerYVelocity += Mathf.Sqrt(smallJumpHeight * -1.0f * gravityValue);
        }
        else if (longJump && groundedPlayer)
        {
            longJump = false;
            playerYVelocity += Mathf.Sqrt(longJumpHeight * -1.0f * gravityValue);
        }

        if (playerYVelocity <= 0 && verticalAxis <= -0.6f)
        {
            // Fastfalling: if holding down while falling, you fall faster
            playerYVelocity += (gravityValue - 80) * Time.deltaTime;
        }
        else
        {
            playerYVelocity += gravityValue * Time.deltaTime;
        }

        Vector3 move = new Vector3(horizontalAxis, playerYVelocity, 0);

        // Can move faster left and right while air borne
        if (jumpState)
        {
            move = new Vector3(horizontalAxis * 1.5f, playerYVelocity, 0);
        }

        // The one and only move function for the player
        controller.Move(move * Time.deltaTime * playerSpeed);

        // If player facing left...
        if (move.x != 0 && groundedPlayer && horizontalAxis < -0.2f && faceRightState)
        {
            StartCoroutine(TurningAnimation(FACING_LEFT, 0.05f));
            faceRightState = false;
        }
        // If player facing right...
        else if (move.x != 0 && groundedPlayer && horizontalAxis > 0.2f && !faceRightState)
        {
            StartCoroutine(TurningAnimation(FACING_RIGHT, 0.05f));
            faceRightState = true;
        }

        if(!emitDashParticles && groundedPlayer && Mathf.Abs(horizontalAxis) > 0.8f)
        {
            emitDashParticles = true;
            particleScript.EmitRunningAndJumpingParticles();
        }
        else if (Mathf.Abs(horizontalAxis) < 0.1f)
        {
            emitDashParticles = false;
        }

        //print("horizontal axis: " + horizontalAxis);

        if (!emitLandingParticles && groundedPlayer && inAirTimer > 0.01f)
        {
            particleScript.EmitLandingParticles();
            emitLandingParticles = true;
            inAirTimer = 0;
        }
    }

    //-------------
    // INPUT EVENTS
    //-------------

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && groundedPlayer)
        {
            Debug.LogWarning("PRESSED JUMP**");
            StartCoroutine(JumpSquatAnimation(0.06f));
        }
        if (context.canceled)
        {
            Debug.LogWarning("RELEASED JUMP** squating? " + jumpSquatState + " | grounded? " + groundedPlayer);
        }
        if (context.canceled && !jumpSquatState && groundedPlayer)
        {
            triggeredSmallJump = true;
        }
    }

    public void HorizontalAxis(InputAction.CallbackContext context)
    {
        horizontalAxis = context.ReadValue<float>();
    }

    public void VerticalAxis(InputAction.CallbackContext context)
    {
        verticalAxis = context.ReadValue<float>();
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

        triggeringFirstJump();
    }

    private void triggeringFirstJump()
    {
        print("+++SQUATTING DONE++ TRIGGED SMALL JUMP?: " + triggeredSmallJump);
        if (triggeredSmallJump)
        {
            longJump = false;
            jumpState = true;
            smallJump = true;
            emitLandingParticles = false;
            particleScript.EmitRunningAndJumpingParticles();
            inAirTimer = 0;
        }
        else
        {
            smallJump = false;
            jumpState = true;
            longJump = true;
            emitLandingParticles = false;
            particleScript.EmitRunningAndJumpingParticles();
            inAirTimer = 0;
        }
        triggeredSmallJump = false;
    }
}
