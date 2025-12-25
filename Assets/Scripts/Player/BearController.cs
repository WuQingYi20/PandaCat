using Unity.Netcode;
using UnityEngine;
using BearCar.Core;
using BearCar.Cart;

namespace BearCar.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BearController : NetworkBehaviour
    {
        [SerializeField] private GameConfig config;

        public NetworkVariable<bool> IsPushing = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> IsAttached = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Rigidbody2D rb;
        private StaminaSystem stamina;
        private bool isNearCart = false;
        private CartController attachedCart = null;
        private int pushSlotIndex = -1;  // 推车位置索引 (0 或 1)

        // 输入状态
        private Vector2 moveInput;
        private bool interactPressed;  // E 键按下（单次触发）

        // 推力方向（吸附后由 A/D 控制）
        public float PushDirection { get; private set; } = 0f;  // -1 = 左, 0 = 不推, 1 = 右

        public bool HasStamina => stamina != null && stamina.HasStamina;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stamina = GetComponent<StaminaSystem>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                var inputHandler = GetComponent<BearInputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.enabled = true;
                }
            }

            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }

            if (IsServer)
            {
                SetSpawnPosition();
            }

            EnsureVisuals();
        }

        private void SetSpawnPosition()
        {
            float xPos = -6f - (OwnerClientId * 1.5f);
            transform.position = new Vector3(xPos, 0f, 0f);
            Debug.Log($"[Bear] Player {OwnerClientId} spawned at {transform.position}");

            rb.mass = 2f;

            var bearCollider = GetComponent<Collider2D>();
            if (bearCollider != null)
            {
                PhysicsMaterial2D bearMaterial = new PhysicsMaterial2D("BearMaterial")
                {
                    friction = 0.5f,
                    bounciness = 0f
                };
                bearCollider.sharedMaterial = bearMaterial;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;

            if (IsAttached.Value && attachedCart != null)
            {
                // 吸附模式：跟随车移动
                Vector3 pushPos = GetPushPosition();
                transform.position = pushPos;
                rb.linearVelocity = Vector2.zero;

                // A/D 控制推力方向
                PushDirection = moveInput.x;  // -1 到 1
                IsPushing.Value = Mathf.Abs(moveInput.x) > 0.1f && HasStamina;

                if (Time.frameCount % 30 == 0)
                {
                    Debug.Log($"[Bear] 吸附中: moveInput={moveInput}, PushDir={PushDirection:F2}, IsPushing={IsPushing.Value}");
                }
            }
            else
            {
                // 自由移动模式
                float moveSpeed = config != null ? config.playerMoveSpeed : 5f;
                Vector2 movement = moveInput * moveSpeed;
                rb.linearVelocity = new Vector2(movement.x, rb.linearVelocity.y);

                PushDirection = 0f;
                IsPushing.Value = false;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            // 处理 E 键吸附/脱离
            if (interactPressed)
            {
                interactPressed = false;  // 消耗按键

                if (IsAttached.Value)
                {
                    // 脱离推车
                    DetachFromCart();
                }
                else if (isNearCart && attachedCart != null)
                {
                    // 吸附到推车
                    AttachToCart();
                }
            }
        }

        private void AttachToCart()
        {
            if (attachedCart == null) return;

            // 获取可用的推车位置
            pushSlotIndex = attachedCart.GetAvailablePushSlot();
            if (pushSlotIndex < 0)
            {
                Debug.Log("[Bear] 没有可用的推车位置");
                return;
            }

            attachedCart.OccupyPushSlot(pushSlotIndex, this);
            IsAttached.Value = true;
            rb.bodyType = RigidbodyType2D.Kinematic;

            Debug.Log($"[Bear] 吸附到推车位置 {pushSlotIndex}");
        }

        private void DetachFromCart()
        {
            if (attachedCart != null && pushSlotIndex >= 0)
            {
                attachedCart.ReleasePushSlot(pushSlotIndex);
            }

            IsAttached.Value = false;
            IsPushing.Value = false;
            pushSlotIndex = -1;
            rb.bodyType = RigidbodyType2D.Dynamic;

            Debug.Log("[Bear] 脱离推车");
        }

        private Vector3 GetPushPosition()
        {
            if (attachedCart == null) return transform.position;

            // 推车位置：车后方，两个位置左右分布
            Vector3 cartPos = attachedCart.transform.position;
            float offsetX = -1.5f;  // 车后方
            float offsetY = (pushSlotIndex == 0) ? 0.3f : -0.3f;  // 上下分布

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
                if (IsAttached.Value)
                {
                    DetachFromCart();
                }
                attachedCart = null;
            }
        }

        [ServerRpc]
        public void SubmitInputServerRpc(Vector2 move, bool interact)
        {
            moveInput = move;
            interactPressed = interact;
        }

        private void EnsureVisuals()
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

            sr.color = OwnerClientId == 0 ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.5f, 0.3f);
            sr.sortingOrder = 10;
        }
    }
}
