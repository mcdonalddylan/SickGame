using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static PlayerInput playerInput;

    public static Vector2 movement;
    public static bool jumpWasPressed;
    public static bool jumpIsHeld;
    public static bool jumpWasReleased;
    public static bool pauseWasPressed;
    public static bool specialOrCancelWasPressed;
    public static bool timeControlWasPressed;
    public static bool dashWasPressed;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction pauseAction;
    private InputAction specialOrCancelAction;
    private InputAction timeControlAction;
    private InputAction dashAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        pauseAction = playerInput.actions["Pause"];
        specialOrCancelAction = playerInput.actions["SpecialAttack"];
        timeControlAction = playerInput.actions["TimeControl"];
        dashAction = playerInput.actions["Dash"];
    }

    private void Update()
    {
        movement = moveAction.ReadValue<Vector2>();
    
        jumpWasPressed = jumpAction.WasPressedThisFrame();
        jumpIsHeld = jumpAction.IsPressed();
        jumpWasReleased = jumpAction.WasReleasedThisFrame();

        pauseWasPressed = pauseAction.WasPressedThisFrame();

        specialOrCancelWasPressed = specialOrCancelAction.WasPressedThisFrame();

        timeControlWasPressed = timeControlAction.WasPressedThisFrame();
    
        dashWasPressed = dashAction.WasPressedThisFrame();
    }
}
