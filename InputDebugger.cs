using UnityEngine;
using UnityEngine.InputSystem;

public class InputDebugger : MonoBehaviour
{
    private PlayerInputActions inputActions;

    private void Start()
    {
        inputActions = new PlayerInputActions();
        inputActions.Enable();

        inputActions.Movement.Move.performed += ctx =>
        {
            var value = ctx.ReadValue<Vector2>();
            Debug.Log($"移动: {value}");
        };

        inputActions.Movement.Jump.performed += ctx =>
        {
            Debug.Log("跳跃按下");
        };
    }
}