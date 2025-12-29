using UnityEngine;
using BearCar.Item;

namespace BearCar.Player
{
    /// <summary>
    /// 本地玩家输入处理器
    /// Player 1: WASD + E + Space + Q/Tab
    /// Player 2: 方向键 + Enter + RShift + ,/.
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
        private static readonly KeyCode P1_ItemPrev = KeyCode.Q;        // 上一个道具
        private static readonly KeyCode P1_ItemNext = KeyCode.R;        // 下一个道具（改用R，避免和E冲突）
        private static readonly KeyCode P1_ItemUse = KeyCode.Tab;       // 使用道具

        // Player 2 按键
        private static readonly KeyCode P2_Up = KeyCode.UpArrow;
        private static readonly KeyCode P2_Down = KeyCode.DownArrow;
        private static readonly KeyCode P2_Left = KeyCode.LeftArrow;
        private static readonly KeyCode P2_Right = KeyCode.RightArrow;
        private static readonly KeyCode P2_Interact = KeyCode.Return;   // Enter
        private static readonly KeyCode P2_Jump = KeyCode.RightShift;
        private static readonly KeyCode P2_ItemPrev = KeyCode.Comma;    // , 上一个道具
        private static readonly KeyCode P2_ItemNext = KeyCode.Period;   // . 下一个道具
        private static readonly KeyCode P2_ItemUse = KeyCode.Slash;     // / 使用道具

        public void Initialize(int index)
        {
            playerIndex = index;
            controller = GetComponent<LocalBearController>();
            string bearName = playerIndex == 0 ? "绿熊" : "红熊";
            Debug.Log($"[InputHandler] Initialize: playerIndex={playerIndex} ({bearName}), 使用按键: {(playerIndex == 0 ? "Q/R/Tab" : ",/.//")}");
        }

        private void Update()
        {
            if (controller == null) return;

            Vector2 moveInput = GetMoveInput();
            bool interactPressed = GetInteractPressed();
            bool jumpPressed = GetJumpPressed();
            bool jumpReleased = GetJumpReleased();

            controller.SetInput(moveInput, interactPressed, jumpPressed, jumpReleased);

            // 道具操作（共享背包）
            HandleItemInput();
        }

        private void HandleItemInput()
        {
            var inventory = SharedInventory.Instance;
            if (inventory == null) return;

            if (playerIndex == 0)
            {
                // Player 1 (绿熊) 道具操作
                if (Input.GetKeyDown(P1_ItemPrev))
                {
                    Debug.Log($"[InputHandler] P1(绿熊) 按下 Q，playerIndex={playerIndex}");
                    inventory.RotatePrev(playerIndex);
                }
                if (Input.GetKeyDown(P1_ItemNext))
                {
                    Debug.Log($"[InputHandler] P1(绿熊) 按下 R，playerIndex={playerIndex}");
                    inventory.RotateNext(playerIndex);
                }
                if (Input.GetKeyDown(P1_ItemUse))
                {
                    Debug.Log($"[InputHandler] P1(绿熊) 按下 Tab，playerIndex={playerIndex}");
                    var item = inventory.UseCurrentItem(playerIndex);
                    if (item != null)
                    {
                        controller.OnItemUsed(item);
                    }
                }
            }
            else
            {
                // Player 2 (红熊) 道具操作
                if (Input.GetKeyDown(P2_ItemPrev))
                {
                    Debug.Log($"[InputHandler] P2(红熊) 按下 ,，playerIndex={playerIndex}");
                    inventory.RotatePrev(playerIndex);
                }
                if (Input.GetKeyDown(P2_ItemNext))
                {
                    Debug.Log($"[InputHandler] P2(红熊) 按下 .，playerIndex={playerIndex}");
                    inventory.RotateNext(playerIndex);
                }
                if (Input.GetKeyDown(P2_ItemUse))
                {
                    Debug.Log($"[InputHandler] P2(红熊) 按下 /，playerIndex={playerIndex}");
                    var item = inventory.UseCurrentItem(playerIndex);
                    if (item != null)
                    {
                        controller.OnItemUsed(item);
                    }
                }
            }
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

        private bool GetJumpReleased()
        {
            if (playerIndex == 0)
            {
                return Input.GetKeyUp(P1_Jump);
            }
            else
            {
                return Input.GetKeyUp(P2_Jump);
            }
        }
    }
}
