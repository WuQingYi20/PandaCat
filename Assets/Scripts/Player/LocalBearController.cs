using UnityEngine;
using BearCar.Core;
using BearCar.Cart;

namespace BearCar.Player
{
    /// <summary>
    /// 本地玩家熊控制器 - 不依赖网络
    /// 用于本地多人模式
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class LocalBearController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        public int PlayerIndex { get; private set; }
        public bool IsPushing { get; private set; }
        public bool IsAttached { get; private set; }
        public float PushDirection { get; private set; }

        private LocalStaminaSystem staminaSystem;
        private JumpSystem jumpSystem;

        public LocalStaminaSystem Stamina => staminaSystem;
        public JumpSystem Jump => jumpSystem;
        public bool HasStamina => staminaSystem != null ? staminaSystem.HasStamina : true;

        private Rigidbody2D rb;
        private bool isNearCart = false;
        private CartController attachedCart = null;
        private int pushSlotIndex = -1;

        // 输入状态
        private Vector2 moveInput;
        private bool interactPressed;

        public void Initialize(int playerIndex)
        {
            PlayerIndex = playerIndex;
            rb = GetComponent<Rigidbody2D>();

            // 添加体力系统
            staminaSystem = gameObject.AddComponent<LocalStaminaSystem>();

            // 添加跳跃系统
            jumpSystem = gameObject.AddComponent<JumpSystem>();

            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }

            // 物理设置
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 2f;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // 碰撞体
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
            }
            col.size = new Vector2(1f, 1f);

            // 物理材质
            PhysicsMaterial2D bearMaterial = new PhysicsMaterial2D("LocalBearMaterial")
            {
                friction = 0.5f,
                bounciness = 0f
            };
            col.sharedMaterial = bearMaterial;

            // 视觉
            SetupVisuals();

            Debug.Log($"[LocalBear] Player {playerIndex} initialized");
        }

        private void SetupVisuals()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }

            if (sr.sprite == null)
            {
                int size = 32;
                Texture2D tex = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            }

            // 不同玩家不同颜色
            sr.color = PlayerIndex == 0
                ? new Color(0.3f, 0.5f, 1f)    // Player 1: 蓝色
                : new Color(1f, 0.5f, 0.3f);   // Player 2: 橙色
            sr.sortingOrder = 10;
        }

        public void SetInput(Vector2 move, bool interact, bool jump = false)
        {
            moveInput = move;
            interactPressed = interact;

            // 跳跃（未吸附时）
            if (jump && !IsAttached && jumpSystem != null)
            {
                jumpSystem.TryJump();
            }
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            if (IsAttached && attachedCart != null)
            {
                // 吸附模式：跟随车移动
                Vector3 pushPos = GetPushPosition();
                transform.position = pushPos;
                rb.linearVelocity = Vector2.zero;

                // A/D 或 方向键 控制推力方向
                PushDirection = moveInput.x;
                IsPushing = Mathf.Abs(moveInput.x) > 0.1f && HasStamina;
            }
            else
            {
                // 自由移动模式
                float moveSpeed = config != null ? config.playerMoveSpeed : 5f;
                Vector2 movement = moveInput * moveSpeed;
                rb.linearVelocity = new Vector2(movement.x, rb.linearVelocity.y);

                PushDirection = 0f;
                IsPushing = false;
            }
        }

        private void Update()
        {
            // 处理交互键吸附/脱离
            if (interactPressed)
            {
                interactPressed = false;

                if (IsAttached)
                {
                    DetachFromCart();
                }
                else if (isNearCart && attachedCart != null)
                {
                    AttachToCart();
                }
            }
        }

        private void AttachToCart()
        {
            if (attachedCart == null) return;

            // 获取可用的推车位置（本地玩家用 LocalPushSlot）
            pushSlotIndex = attachedCart.GetAvailableLocalPushSlot();
            if (pushSlotIndex < 0)
            {
                Debug.Log("[LocalBear] 没有可用的推车位置");
                return;
            }

            attachedCart.OccupyLocalPushSlot(pushSlotIndex, this);
            IsAttached = true;
            rb.bodyType = RigidbodyType2D.Kinematic;

            Debug.Log($"[LocalBear] Player {PlayerIndex} 吸附到推车位置 {pushSlotIndex}");
        }

        private void DetachFromCart()
        {
            if (attachedCart != null && pushSlotIndex >= 0)
            {
                attachedCart.ReleaseLocalPushSlot(pushSlotIndex);
            }

            IsAttached = false;
            IsPushing = false;
            pushSlotIndex = -1;
            rb.bodyType = RigidbodyType2D.Dynamic;

            Debug.Log($"[LocalBear] Player {PlayerIndex} 脱离推车");
        }

        private Vector3 GetPushPosition()
        {
            if (attachedCart == null) return transform.position;

            // 推车位置：车后方，两个位置上下分布
            Vector3 cartPos = attachedCart.transform.position;
            float offsetX = -1.5f;
            float offsetY = (pushSlotIndex == 0) ? 0.3f : -0.3f;

            return cartPos + new Vector3(offsetX, offsetY, 0);
        }

        public void SetNearCart(bool value, CartController cart = null)
        {
            isNearCart = value;
            if (value && cart != null)
            {
                attachedCart = cart;
            }
            else if (!value)
            {
                if (IsAttached)
                {
                    DetachFromCart();
                }
                attachedCart = null;
            }
        }

        private void OnDestroy()
        {
            if (IsAttached && attachedCart != null)
            {
                DetachFromCart();
            }
        }
    }
}
