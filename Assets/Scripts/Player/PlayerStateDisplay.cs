using UnityEngine;
using TMPro;

public class PlayerStateDisplay : MonoBehaviour
{
    // Reference to the TextMeshPro component
    public TMP_Text stateText;

    // Example boolean states
    public bool isFacingRight;
    public bool isGrounded;
    public bool isTouchingWall;
    public bool bumpedHead;
    public bool isJumping;
    public bool isWallJumping;
    public float wallJumpPostBufferTimer;
    public bool isWallSlideFalling;
    public bool isFalling;
    public bool isFastFalling;
    public bool isForcedFastFalling;
    public bool isWallSliding;
    public bool isDashing;
    public float fallTimer;
    public float coyoteTimer;
    public float dropThroughHoldTimer;

    void Update()
    {
        // Update the displayed text every frame
        UpdateStateDisplay();
    }

    void UpdateStateDisplay()
    {
        // Build a string showing the current state of each boolean
        string displayText = "Player State:\n" +
                             $"Facing Right: {(isFacingRight ? "*TRUE*": "False")}\n" +
                             $"Grounded: {(isGrounded ? "*TRUE*": "False")}\n" +
                             $"Touching Wall: {(isTouchingWall ? "*TRUE*": "False")}\n" +
                             $"Bumped Head: {(bumpedHead ? "*TRUE*": "False")}\n" +
                             $"Jumping: {(isJumping ? "*TRUE*": "False")}\n" +
                             $"Wall Jumping: {(isWallJumping ? "*TRUE*": "False")}\n" +
                             $"Wall Jump Timer: {(wallJumpPostBufferTimer)}\n" +
                             $"Wall Sliding: {(isWallSliding ? "*TRUE*": "False")}\n" +
                             $"Wall Slide Falling: {(isWallSlideFalling ? "*TRUE*": "False")}\n" +
                             $"Falling: {(isFalling ? "*TRUE*": "False")}\n" +
                             $"Fast Falling: {(isFastFalling ? "*TRUE*": "False")}\n" +
                             $"Forced Fast Falling: {(isForcedFastFalling ? "*TRUE*": "False")}\n" +
                             $"Dashing: {(isDashing ? "*TRUE*": "False")}\n" +
                             $"Fall Timer: {fallTimer}\n" +
                             $"Coyote Timer: {coyoteTimer}\n" +
                             $"Drop Through Hold Timer: {dropThroughHoldTimer}\n";

        // Update the UI text
        if (stateText != null)
        {
            stateText.text = displayText;
        }
    }
}
