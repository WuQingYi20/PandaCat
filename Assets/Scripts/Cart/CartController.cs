using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using BearCar.Core;
using BearCar.Player;

namespace BearCar.Cart
{
    public enum CartState
    {
        Idle,
        Sliding,
        Holding,
        Advancing
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class CartController : NetworkBehaviour
    {
        [SerializeField] private GameConfig config;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float slideSpeed = 2f;

        public NetworkVariable<int> ActivePushers = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<CartState> State = new(
            CartState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Rigidbody2D rb;
        private HashSet<BearController> registeredBears = new HashSet<BearController>();
        private float currentSlopeAngle = 0f;
        private Collider2D cartCollider;

        public override void OnNetworkSpawn()
        {
            rb = GetComponent<Rigidbody2D>();
            cartCollider = GetComponent<Collider2D>();

            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }

            // 设置为 Kinematic - 完全由代码控制移动
            rb.bodyType = RigidbodyType2D.Kinematic;

            // 车的主碰撞体设为 Trigger，避免与地面碰撞反弹
            if (cartCollider != null)
            {
                cartCollider.isTrigger = true;
            }

            // 确保车辆可见
            EnsureVisuals();

            // 设置推车区域
            SetupPushZone();
        }

        private void SetupCollisions()
        {
            // 让车的主碰撞体不与熊碰撞
            // 但推车区域（Trigger）仍然检测熊
            if (cartCollider != null)
            {
                var bears = FindObjectsByType<BearController>(FindObjectsSortMode.None);
                foreach (var bear in bears)
                {
                    var bearCollider = bear.GetComponent<Collider2D>();
                    if (bearCollider != null)
                    {
                        Physics2D.IgnoreCollision(cartCollider, bearCollider, true);
                    }
                }
            }
        }

        // 当新熊生成时也要忽略碰撞
        public void IgnoreCollisionWithBear(Collider2D bearCollider)
        {
            if (cartCollider != null && bearCollider != null)
            {
                Physics2D.IgnoreCollision(cartCollider, bearCollider, true);
            }
        }

        private void SetupPushZone()
        {
            // 查找或创建推车区域
            Transform pushZoneTransform = transform.Find("PushZone");
            GameObject pushZone;

            if (pushZoneTransform == null)
            {
                pushZone = new GameObject("PushZone");
                pushZone.transform.SetParent(transform);
                pushZone.transform.localPosition = new Vector3(-1.5f, 0, 0); // 车后方
            }
            else
            {
                pushZone = pushZoneTransform.gameObject;
            }

            // 确保有碰撞体
            var col = pushZone.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = pushZone.AddComponent<BoxCollider2D>();
            }
            col.isTrigger = true;
            col.size = new Vector2(3f, 2f); // 足够大，容纳两只熊

            // 确保有 CartPushZone 脚本
            if (pushZone.GetComponent<CartPushZone>() == null)
            {
                pushZone.AddComponent<CartPushZone>();
            }

            Debug.Log($"[Cart] PushZone setup complete, size: {col.size}");
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
                // 创建黄色方块（车辆）
                int w = 64, h = 32;
                Texture2D tex = new Texture2D(w, h);
                Color[] pixels = new Color[w * h];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            }

            sr.color = new Color(1f, 0.8f, 0.2f); // 黄色车
            sr.sortingOrder = 5;

            Debug.Log($"[Cart] Visuals set up at position {transform.position}");
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;

            DetectSlope();
            ApplyForces();
            UpdateState();
        }

        private void DetectSlope()
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                2f,
                LayerMask.GetMask("Ground")
            );

            if (hit.collider != null)
            {
                currentSlopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            }
            else if (config != null)
            {
                currentSlopeAngle = config.slopeAngle;
            }
        }

        private void ApplyForces()
        {
            if (!IsServer) return;
            if (config == null) return;

            // 统计正在推车的熊
            int pushers = 0;
            foreach (var bear in registeredBears)
            {
                if (bear != null && bear.IsPushing.Value && bear.HasStamina)
                {
                    pushers++;
                }
            }
            ActivePushers.Value = pushers;

            // 检测是否在坡道上
            bool onSlope = currentSlopeAngle > 5f;

            // 计算移动
            float movement = 0f;

            if (pushers == 0)
            {
                // 无人推
                if (onSlope)
                {
                    // 坡道上下滑
                    movement = -slideSpeed * Time.fixedDeltaTime;
                }
                // 平地上静止（Kinematic 不会被撞动）
            }
            else if (pushers == 1)
            {
                if (onSlope)
                {
                    // 坡道上：1人只能维持，不动
                    movement = 0f;
                }
                else
                {
                    // 平地上：1人可以推动
                    movement = moveSpeed * Time.fixedDeltaTime;
                }
            }
            else
            {
                // 2人推：可以推动（坡道和平地都可以）
                movement = moveSpeed * Time.fixedDeltaTime;
            }

            // 应用移动（Kinematic 使用 MovePosition）
            if (Mathf.Abs(movement) > 0.0001f)
            {
                Vector3 newPos = transform.position + new Vector3(movement, 0, 0);
                rb.MovePosition(newPos);
            }

            // 调试日志（降低频率）
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[Cart] Pushers: {pushers}, OnSlope: {onSlope}, Movement: {movement:F3}");
            }
        }

        private void UpdateState()
        {
            State.Value = ActivePushers.Value switch
            {
                0 when currentSlopeAngle > 1f => CartState.Sliding,
                0 => CartState.Idle,
                1 => CartState.Holding,
                _ => CartState.Advancing
            };
        }

        public void RegisterBear(BearController bear)
        {
            if (!IsServer) return;

            registeredBears.Add(bear);
            bear.SetNearCart(true);
        }

        public void UnregisterBear(BearController bear)
        {
            if (!IsServer) return;

            registeredBears.Remove(bear);
            bear.SetNearCart(false);
        }
    }
}
