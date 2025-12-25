using UnityEngine;

namespace BearCar.Player
{
    /// <summary>
    /// 本地玩家输入处理器
    /// Player 1: WASD + E
    /// Player 2: 方向键 + Enter
    /// </summary>
    [RequireComponent(typeof(LocalBearController))]
    public class LocalBearInputHandler : MonoBehaviour
    {
        private LocalBearController controller;
        private int playerIndex;

        // Player 1 按键
        private static readonly KeyCode P1_Up = KeyCode.W;
        private static readonly KeyCode P1_Down = KeyCode.S;
        private static readonly KeyCode P1_Left = KeyCode.A;
        private static readonly KeyCode P1_Right = KeyCode.D;
        private static readonly KeyCode P1_Interact = KeyCode.E;
        private static readonly KeyCode P1_Jump = KeyCode.Space;

        // Player 2 按键
        private static readonly KeyCode P2_Up = KeyCode.UpArrow;
        private static readonly KeyCode P2_Down = KeyCode.DownArrow;
        private static readonly KeyCode P2_Left = KeyCode.LeftArrow;
        private static readonly KeyCode P2_Right = KeyCode.RightArrow;
        private static readonly KeyCode P2_Interact = KeyCode.Return; // Enter
        private static readonly KeyCode P2_Jump = KeyCode.RightShift;

        public void Initialize(int index)
        {
            playerIndex = index;
            controller = GetComponent<LocalBearController>();
        }

        private void Update()
        {
            if (controller == null) return;

            Vector2 moveInput = GetMoveInput();
            bool interactPressed = GetInteractPressed();
            bool jumpPressed = GetJumpPressed();

            controller.SetInput(moveInput, interactPressed, jumpPressed);
        }

        private Vector2 GetMoveInput()
        {
            Vector2 input = Vector2.zero;

            if (playerIndex == 0)
            {
                // Player 1: WASD
                if (Input.GetKey(P1_Up)) input.y += 1f;
                if (Input.GetKey(P1_Down)) input.y -= 1f;
                if (Input.GetKey(P1_Left)) input.x -= 1f;
                if (Input.GetKey(P1_Right)) input.x += 1f;
            }
            else
            {
                // Player 2: 方向键
                if (Input.GetKey(P2_Up)) input.y += 1f;
                if (Input.GetKey(P2_Down)) input.y -= 1f;
                if (Input.GetKey(P2_Left)) input.x -= 1f;
                if (Input.GetKey(P2_Right)) input.x += 1f;
            }

            // 归一化对角线移动
            if (input.magnitude > 1f)
            {
                input.Normalize();
            }

            return input;
        }

        private bool GetInteractPressed()
        {
            if (playerIndex == 0)
            {
                return Input.GetKeyDown(P1_Interact);
            }
            else
            {
                return Input.GetKeyDown(P2_Interact);
            }
        }

        private bool GetJumpPressed()
        {
            if (playerIndex == 0)
            {
                return Input.GetKeyDown(P1_Jump);
            }
            else
            {
                return Input.GetKeyDown(P2_Jump);
            }
        }
    }
}
