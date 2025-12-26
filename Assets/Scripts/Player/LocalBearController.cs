using UnityEngine;
using BearCar.Core;
using BearCar.Cart;
using BearCar.Item;

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

        // 供动画系统读取
        public Vector2 MoveInput => moveInput;

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
            sr.sortingOrder = 10;

            // 添加动画控制器
            var animator = GetComponent<BearAnimator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<BearAnimator>();
            }
            animator.SetPlayerIndex(PlayerIndex);

            // 如果没有动画帧，使用备用纯色方块
            if (sr.sprite == null)
            {
                CreateFallbackSprite(sr);
            }
        }

        private void CreateFallbackSprite(SpriteRenderer sr)
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

            // 备用颜色：绿熊 (P1) 和 红熊 (P2)
            sr.color = PlayerIndex == 0
                ? new Color(0.3f, 0.8f, 0.4f)  // 绿熊
                : new Color(0.9f, 0.3f, 0.3f); // 红熊
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

            // 根据玩家位置获取对应侧的可用槽位
            pushSlotIndex = attachedCart.GetAvailableLocalPushSlotBySide(transform.position);
            if (pushSlotIndex < 0)
            {
                Debug.Log("[LocalBear] 没有可用的推车位置");
                return;
            }

            attachedCart.OccupyLocalPushSlot(pushSlotIndex, this);
            IsAttached = true;
            rb.bodyType = RigidbodyType2D.Kinematic;

            string side = CartController.IsLeftSlot(pushSlotIndex) ? "左侧" : "右侧";
            Debug.Log($"[LocalBear] Player {PlayerIndex} 吸附到{side}推车位置 {pushSlotIndex}");
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

            Vector3 cartPos = attachedCart.transform.position;

            // 槽位布局: 0,1 = 左侧（车后方），2,3 = 右侧（车前方）
            bool isLeftSide = CartController.IsLeftSlot(pushSlotIndex);
            float offsetX = isLeftSide ? -1.5f : 1.5f;

            // 上下分布: 槽位 0,2 在上，槽位 1,3 在下
            int verticalIndex = pushSlotIndex % 2;
            float offsetY = (verticalIndex == 0) ? 0.3f : -0.3f;

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

        /// <summary>
        /// 当玩家使用道具时调用
        /// </summary>
        public void OnItemUsed(ItemData item)
        {
            if (item == null) return;

            // 记录到日志系统
            var log = FindFirstObjectByType<ItemEffectLog>();
            if (log != null)
            {
                log.AddLog(PlayerIndex, item);
            }

            // 计算颜色亲和效果
            float effectValue = item.GetEffectForPlayer(PlayerIndex);

            // 根据道具类型应用效果
            switch (item.itemType)
            {
                case ItemType.Food:
                case ItemType.StaminaRecover:
                    if (staminaSystem != null)
                    {
                        staminaSystem.Recover(effectValue);
                    }
                    break;

                case ItemType.Poison:
                    if (staminaSystem != null)
                    {
                        staminaSystem.Recover(-Mathf.Abs(item.effectValue)); // 负值扣血
                    }
                    break;

                case ItemType.SpeedBoost:
                    StartCoroutine(SpeedBoostEffect(item.effectValue, item.effectDuration));
                    break;

                case ItemType.RocketBoost:
                    StartCoroutine(RocketBoostEffect(item.effectDuration));
                    break;

                // 其他效果由 ItemEffectHandler 全局处理
            }

            Debug.Log($"[LocalBear] P{PlayerIndex} 使用了 {item.itemName}，效果值: {effectValue}");
        }

        private System.Collections.IEnumerator RocketBoostEffect(float duration)
        {
            // 火箭推进效果
            float originalGravity = rb.gravityScale;
            rb.gravityScale = 0;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 向前冲刺
                rb.linearVelocity = new Vector2(10f, rb.linearVelocity.y);
                elapsed += Time.deltaTime;
                yield return null;
            }

            rb.gravityScale = originalGravity;
        }

        private System.Collections.IEnumerator SpeedBoostEffect(float multiplier, float duration)
        {
            // 简单的速度加成效果
            float originalSpeed = config != null ? config.playerMoveSpeed : 5f;
            // 这里可以添加视觉效果

            yield return new WaitForSeconds(duration);
            // 效果结束
        }
    }
}
