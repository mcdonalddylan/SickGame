using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerControllerScript : MonoBehaviour
{
    private float playerSpeed = 10.0f;
    private float smallJumpHeight = 0.65f;
    private float longJumpHeight = 1.1f;
    private float gravityValue = -65f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private bool smallJump;
    private bool longJump;
    private float horizontalAxis;
    private float verticalAxis;

    // Player state variables
    public static int damage = 0;
    public static bool faceRightState = false;
    public static bool hitLagState;
    public static bool hitStunState;
    public static bool walkState;
    public static bool runningState;
    public static bool sprintState;
    public static bool crouchState;
    public static bool jumpSquatState;
    public static bool jumpState;
    public static bool doubleJumpState;
    public static bool attackState;

    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
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
        print("go transaform: " + gameObject.transform.position + " | checkpoint pos: " + checkpoint.transform.position);
        gameObject.transform.position = checkpoint.transform.position;
        print("go transform after: " + gameObject.transform.position);
    }

    void Update()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
            jumpState = false;
        }

        Vector3 move = new Vector3(horizontalAxis, 0, 0);
        // Can move faster left and right while air borne
        if (jumpState)
        {
            move = new Vector3(horizontalAxis * 1.5f, 0, 0);
        }
        controller.Move(move * Time.deltaTime * playerSpeed);

        // Forces the player to face left or right
        //if (move != Vector3.zero && !jumpSquatState && jumpState)
        //{
        //    gameObject.transform.eulerAngles = move;
        //}

        // Changes the height position of the player
        if (smallJump && groundedPlayer)
        {
            smallJump = false;
            playerVelocity.y += Mathf.Sqrt(smallJumpHeight * -3.0f * gravityValue);
        }

        if (playerVelocity.y <= 0 && verticalAxis <= -0.6f)
        {
            // Fastfalling: if holding down while falling, you fall faster
            playerVelocity.y += (gravityValue - 80) * Time.deltaTime;
        }
        else
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
        }
        controller.Move(playerVelocity * Time.deltaTime);
    }

    //-------------
    // INPUT EVENTS
    //-------------

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpState = true;
            smallJump = true;
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
}
