using UnityEngine;
using UnityEngine.InputSystem;

namespace BearCar.Player
{
    [RequireComponent(typeof(BearController))]
    public class BearInputHandler : MonoBehaviour
    {
        private BearController controller;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction interactAction;
        private InputAction jumpAction;

        private bool lastInteractState = false;
        private bool lastJumpState = false;

        private void Awake()
        {
            controller = GetComponent<BearController>();
            playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                moveAction = playerInput.actions["Move"];
                interactAction = playerInput.actions["Interact"];
                jumpAction = playerInput.actions["Jump"];
            }

            enabled = false;
        }

        private void Update()
        {
            if (controller == null || !controller.IsOwner) return;

            Vector2 moveInput = Vector2.zero;
            bool interactPressed = false;
            bool jumpPressed = false;

            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }

            if (interactAction != null)
            {
                bool currentInteract = interactAction.IsPressed();
                // 检测按下瞬间（上升沿）
                interactPressed = currentInteract && !lastInteractState;
                lastInteractState = currentInteract;
            }

            if (jumpAction != null)
            {
                bool currentJump = jumpAction.IsPressed();
                jumpPressed = currentJump && !lastJumpState;
                lastJumpState = currentJump;
            }
            else
            {
                // 备用：直接检测空格键
                jumpPressed = Input.GetKeyDown(KeyCode.Space);
            }

            controller.SubmitInputServerRpc(moveInput, interactPressed, jumpPressed);
        }
    }
}
