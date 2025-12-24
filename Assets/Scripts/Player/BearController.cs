using Unity.Netcode;
using UnityEngine;
using BearCar.Core;

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

        public NetworkVariable<Vector2> MoveInput = new(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Rigidbody2D rb;
        private StaminaSystem stamina;
        private bool isNearCart = false;
        private bool pushButtonHeld = false;

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

            // 设置生成位置（根据玩家ID）
            if (IsServer)
            {
                SetSpawnPosition();
            }

            // 确保熊可见
            EnsureVisuals();
        }

        private void SetSpawnPosition()
        {
            // 熊1 在 -6, 熊2 在 -7（车在 -4）
            float xPos = -6f - (OwnerClientId * 1.5f);
            transform.position = new Vector3(xPos, 0f, 0f);
            Debug.Log($"[Bear] Player {OwnerClientId} spawned at {transform.position}");

            // 通知车忽略与此熊的碰撞
            NotifyCartsOfNewBear();
        }

        private void NotifyCartsOfNewBear()
        {
            var myCollider = GetComponent<Collider2D>();
            if (myCollider == null) return;

            var carts = FindObjectsByType<Cart.CartController>(FindObjectsSortMode.None);
            foreach (var cart in carts)
            {
                cart.IgnoreCollisionWithBear(myCollider);
            }
        }

        private void EnsureVisuals()
        {
            // 检查是否有 SpriteRenderer
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }

            // 如果没有 Sprite，创建一个纯色方块
            if (sr.sprite == null)
            {
                sr.sprite = CreateColoredSprite();
            }

            // 根据玩家 ID 设置不同颜色
            sr.color = OwnerClientId == 0 ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.5f, 0.3f);
            sr.sortingOrder = 10;

            Debug.Log($"[Bear] Player {OwnerClientId} visuals set up, color: {sr.color}");
        }

        private Sprite CreateColoredSprite()
        {
            // 创建一个 32x32 的白色纹理
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;

            float moveSpeed = config != null ? config.playerMoveSpeed : 5f;
            Vector2 movement = MoveInput.Value * moveSpeed;
            rb.linearVelocity = new Vector2(movement.x, rb.linearVelocity.y);

            UpdatePushState();
        }

        private void UpdatePushState()
        {
            bool shouldBePushing = isNearCart &&
                                   pushButtonHeld &&
                                   HasStamina;

            if (IsPushing.Value != shouldBePushing)
            {
                IsPushing.Value = shouldBePushing;
            }
        }

        public void SetNearCart(bool value)
        {
            isNearCart = value;
            if (!value && IsServer)
            {
                IsPushing.Value = false;
            }
        }

        [ServerRpc]
        public void SubmitInputServerRpc(Vector2 moveInput, bool isPushHeld)
        {
            MoveInput.Value = moveInput;
            pushButtonHeld = isPushHeld;
        }
    }
}
