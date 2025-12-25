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

        [Header("Physics Settings")]
        [Tooltip("车的质量，应该远大于熊(2)，让身体碰撞难以推动")]
        [SerializeField] private float cartMass = 50f;

        [Tooltip("每只熊的推力。50kg车在15°坡: 重力分量≈127N，设130让1熊刚好维持")]
        [SerializeField] private float pushForcePerBear = 130f;

        [Tooltip("线性阻力，模拟滚动摩擦。值越高车越难被撞动")]
        [SerializeField] private float linearDrag = 10f;

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
        private HashSet<BearAI> registeredAIs = new HashSet<BearAI>();
        private float currentSlopeAngle = 0f;
        private Vector2 slopeNormal = Vector2.up;
        private Collider2D cartCollider;
        private List<Collider2D> bearColliders = new List<Collider2D>();

        // 推车位置管理（左右各2个位置，共4个）
        // 0,1 = 左侧（推车前进），2,3 = 右侧（推车后退）
        private BearController[] pushSlots = new BearController[4];

        // 本地玩家推车位置（本地多人模式）
        private LocalBearController[] localPushSlots = new LocalBearController[4];
        private HashSet<LocalBearController> registeredLocalBears = new HashSet<LocalBearController>();

        // 是否为纯本地模式（无网络）
        public bool IsLocalOnlyMode { get; set; } = false;

        public override void OnNetworkSpawn()
        {
            rb = GetComponent<Rigidbody2D>();
            cartCollider = GetComponent<Collider2D>();

            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }

            // 设置为 Dynamic - 真实物理驱动
            rb.bodyType = RigidbodyType2D.Dynamic;

            // 强制使用固定值（避免 Inspector 旧值问题）
            rb.mass = 500f;          // 车非常重，防止被熊撞动
            rb.linearDamping = 0.5f; // 低阻力，允许滑动
            rb.angularDamping = 5f;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 防止翻车

            Debug.Log($"[Cart] 物理设置: 质量={rb.mass}, 阻力={rb.linearDamping}");

            // 车的碰撞体保持普通碰撞（不是 Trigger）
            if (cartCollider != null)
            {
                cartCollider.isTrigger = false;

                // 添加物理材质
                // 30°坡需要 friction < tan(30°) ≈ 0.577 才能滑动
                PhysicsMaterial2D cartMaterial = new PhysicsMaterial2D("CartMaterial")
                {
                    friction = 0.25f,  // 允许在坡上滑动
                    bounciness = 0f    // 不反弹
                };
                cartCollider.sharedMaterial = cartMaterial;
            }

            // 确保车辆可见
            EnsureVisuals();

            // 设置推车区域
            SetupPushZone();
        }

        // ========== 碰撞处理：防止被熊撞动 ==========
        private Vector2 velocityBeforeCollision;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (rb == null) return;

            // 收集熊的碰撞体
            var bearCollider = collision.collider;
            bool isBear = collision.gameObject.GetComponent<BearController>() != null ||
                          collision.gameObject.GetComponent<BearAI>() != null;

            if (isBear && !bearColliders.Contains(bearCollider))
            {
                bearColliders.Add(bearCollider);
            }

            // 如果没人推车，且是被熊撞的，抵消碰撞冲量
            if (ActivePushers.Value == 0 && isBear)
            {
                rb.linearVelocity = velocityBeforeCollision;
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            // 移除离开的熊碰撞体
            var bearCollider = collision.collider;
            bearColliders.Remove(bearCollider);
        }

        private void SetBearCollisionIgnored(bool ignore)
        {
            if (cartCollider == null) return;

            // 使用吸附位置的熊的碰撞体，而不是依赖碰撞事件
            foreach (var bear in pushSlots)
            {
                if (bear != null)
                {
                    var bearCol = bear.GetComponent<Collider2D>();
                    if (bearCol != null)
                    {
                        Physics2D.IgnoreCollision(cartCollider, bearCol, ignore);
                    }
                }
            }

            // 也处理 bearColliders 列表（用于非吸附的熊）
            foreach (var bearCol in bearColliders)
            {
                if (bearCol != null)
                {
                    Physics2D.IgnoreCollision(cartCollider, bearCol, ignore);
                }
            }
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            // 保存碰撞前的速度
            velocityBeforeCollision = rb.linearVelocity;

            if (!IsServer) return;

            DetectSlope();
            ApplyForces();
            UpdateState();
        }


        private void SetupPushZone()
        {
            // 删除旧的 PushZone（如果存在）
            Transform oldPushZone = transform.Find("PushZone");
            if (oldPushZone != null)
            {
                Destroy(oldPushZone.gameObject);
                Debug.Log("[Cart] 删除旧的 PushZone");
            }

            // 创建左侧推车区域（车后方，推动前进）
            SetupSinglePushZone("PushZoneLeft", new Vector3(-1.5f, 0, 0));

            // 创建右侧推车区域（车前方，推动后退）
            SetupSinglePushZone("PushZoneRight", new Vector3(1.5f, 0, 0));

            Debug.Log("[Cart] PushZones setup complete (Left & Right)");
        }

        private void SetupSinglePushZone(string zoneName, Vector3 localPosition)
        {
            Transform pushZoneTransform = transform.Find(zoneName);
            GameObject pushZone;

            if (pushZoneTransform == null)
            {
                pushZone = new GameObject(zoneName);
                pushZone.transform.SetParent(transform);
                pushZone.transform.localPosition = localPosition;
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
            col.size = new Vector2(2f, 2f); // 足够大，容纳两只熊

            // 确保有 CartPushZone 脚本
            if (pushZone.GetComponent<CartPushZone>() == null)
            {
                pushZone.AddComponent<CartPushZone>();
            }
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
                slopeNormal = hit.normal;
            }
            else
            {
                // 没有检测到地面，使用配置的坡度或默认水平
                if (config != null && config.slopeAngle > 0)
                {
                    currentSlopeAngle = config.slopeAngle;
                    float angleRad = config.slopeAngle * Mathf.Deg2Rad;
                    slopeNormal = new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));
                }
                else
                {
                    currentSlopeAngle = 0f;
                    slopeNormal = Vector2.up;
                }
            }
        }

        private void ApplyForces()
        {
            // 本地模式或服务器模式都可以处理
            if (!IsLocalOnlyMode && !IsServer) return;

            // 计算总推力方向（每个熊的推力方向累加）
            float totalPushInput = 0f;
            int activePushers = 0;

            // 统计网络玩家
            foreach (var bear in pushSlots)
            {
                if (bear != null && bear.IsPushing.Value && bear.HasStamina)
                {
                    totalPushInput += bear.PushDirection;
                    activePushers++;
                }
            }

            // 统计本地玩家
            foreach (var localBear in localPushSlots)
            {
                if (localBear != null && localBear.IsPushing && localBear.HasStamina)
                {
                    totalPushInput += localBear.PushDirection;
                    activePushers++;
                }
            }

            // 统计 AI 熊（跟随玩家方向）
            foreach (var ai in registeredAIs)
            {
                if (ai != null && ai.IsPushingCart && Mathf.Abs(ai.PushDirection) > 0.1f)
                {
                    totalPushInput += ai.PushDirection;
                    activePushers++;
                }
            }

            // 更新推车人数（本地模式直接设置，网络模式用 NetworkVariable）
            if (IsLocalOnlyMode)
            {
                // 本地模式下无法使用 NetworkVariable，跳过
            }
            else
            {
                ActivePushers.Value = activePushers;
            }

            // 计算沿坡面的方向
            Vector2 slopeRight = new Vector2(slopeNormal.y, -slopeNormal.x);
            if (slopeRight.x < 0) slopeRight = -slopeRight;

            // 施加推力 - 使用 AddForce 对抗质量和阻力
            if (Mathf.Abs(totalPushInput) > 0.1f)
            {
                float forcePerBear = 3400f;  // 每只熊 3400N
                float totalForce = forcePerBear * totalPushInput;

                // 沿坡面方向施加力
                Vector2 pushForce = slopeRight * totalForce;
                rb.AddForce(pushForce, ForceMode2D.Force);
            }

            // 调试日志
            if (Time.frameCount % 30 == 0)
            {
                int attachedCount = 0;
                foreach (var bear in pushSlots) if (bear != null) attachedCount++;
                foreach (var localBear in localPushSlots) if (localBear != null) attachedCount++;
                Debug.Log($"[Cart] Attached: {attachedCount}, Pushers: {activePushers}, Input: {totalPushInput:F1}, Vel: {rb.linearVelocity:F2}");
            }
        }

        private void UpdateState()
        {
            float velocityX = rb.linearVelocity.x;
            bool onSlope = currentSlopeAngle > 5f;

            if (ActivePushers.Value == 0)
            {
                State.Value = (onSlope && velocityX < -0.1f) ? CartState.Sliding : CartState.Idle;
            }
            else if (velocityX > 0.5f)
            {
                State.Value = CartState.Advancing;
            }
            else if (velocityX < -0.1f)
            {
                State.Value = CartState.Sliding;
            }
            else
            {
                State.Value = CartState.Holding;
            }
        }

        public void RegisterBear(BearController bear)
        {
            if (!IsServer) return;

            registeredBears.Add(bear);
            bear.SetNearCart(true, this);
        }

        public void UnregisterBear(BearController bear)
        {
            if (!IsServer) return;

            registeredBears.Remove(bear);
            bear.SetNearCart(false, null);
        }

        public void RegisterAI(BearAI ai)
        {
            registeredAIs.Add(ai);
            Debug.Log($"[Cart] AI registered, total AIs: {registeredAIs.Count}");
        }

        public void UnregisterAI(BearAI ai)
        {
            registeredAIs.Remove(ai);
            Debug.Log($"[Cart] AI unregistered, total AIs: {registeredAIs.Count}");
        }

        // ========== 推车位置管理 ==========
        // 槽位布局: 0,1 = 左侧, 2,3 = 右侧

        /// <summary>
        /// 获取任意可用槽位
        /// </summary>
        public int GetAvailablePushSlot()
        {
            for (int i = 0; i < pushSlots.Length; i++)
            {
                if (pushSlots[i] == null)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 根据玩家位置获取最近侧的可用槽位
        /// </summary>
        public int GetAvailablePushSlotBySide(Vector3 playerPos)
        {
            bool isLeftSide = playerPos.x < transform.position.x;
            return isLeftSide ? GetAvailableLeftSlot() : GetAvailableRightSlot();
        }

        /// <summary>
        /// 获取左侧可用槽位 (0,1)
        /// </summary>
        public int GetAvailableLeftSlot()
        {
            if (pushSlots[0] == null) return 0;
            if (pushSlots[1] == null) return 1;
            return -1;
        }

        /// <summary>
        /// 获取右侧可用槽位 (2,3)
        /// </summary>
        public int GetAvailableRightSlot()
        {
            if (pushSlots[2] == null) return 2;
            if (pushSlots[3] == null) return 3;
            return -1;
        }

        /// <summary>
        /// 判断槽位是左侧还是右侧
        /// </summary>
        public static bool IsLeftSlot(int slotIndex) => slotIndex < 2;

        public void OccupyPushSlot(int slotIndex, BearController bear)
        {
            if (slotIndex >= 0 && slotIndex < pushSlots.Length)
            {
                pushSlots[slotIndex] = bear;

                // 吸附时忽略碰撞
                if (bear != null && cartCollider != null)
                {
                    var bearCol = bear.GetComponent<Collider2D>();
                    if (bearCol != null)
                    {
                        Physics2D.IgnoreCollision(cartCollider, bearCol, true);
                        Debug.Log($"[Cart] 忽略碰撞: Bear slot {slotIndex}");
                    }
                }
            }
        }

        public void ReleasePushSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < pushSlots.Length)
            {
                // 先恢复碰撞，再移除引用
                var bear = pushSlots[slotIndex];
                if (bear != null && cartCollider != null)
                {
                    var bearCol = bear.GetComponent<Collider2D>();
                    if (bearCol != null)
                    {
                        Physics2D.IgnoreCollision(cartCollider, bearCol, false);
                        Debug.Log($"[Cart] 恢复碰撞: Bear slot {slotIndex}");
                    }
                }
                pushSlots[slotIndex] = null;
            }
        }

        // ========== 本地玩家推车位置管理 ==========
        public void RegisterLocalBear(LocalBearController bear)
        {
            registeredLocalBears.Add(bear);
            bear.SetNearCart(true, this);
            Debug.Log($"[Cart] Local bear registered, total: {registeredLocalBears.Count}");
        }

        public void UnregisterLocalBear(LocalBearController bear)
        {
            registeredLocalBears.Remove(bear);
            bear.SetNearCart(false, null);
            Debug.Log($"[Cart] Local bear unregistered, total: {registeredLocalBears.Count}");
        }

        public int GetAvailableLocalPushSlot()
        {
            for (int i = 0; i < localPushSlots.Length; i++)
            {
                if (localPushSlots[i] == null)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 根据玩家位置获取本地玩家最近侧的可用槽位
        /// </summary>
        public int GetAvailableLocalPushSlotBySide(Vector3 playerPos)
        {
            bool isLeftSide = playerPos.x < transform.position.x;
            return isLeftSide ? GetAvailableLocalLeftSlot() : GetAvailableLocalRightSlot();
        }

        public int GetAvailableLocalLeftSlot()
        {
            if (localPushSlots[0] == null) return 0;
            if (localPushSlots[1] == null) return 1;
            return -1;
        }

        public int GetAvailableLocalRightSlot()
        {
            if (localPushSlots[2] == null) return 2;
            if (localPushSlots[3] == null) return 3;
            return -1;
        }

        public void OccupyLocalPushSlot(int slotIndex, LocalBearController bear)
        {
            if (slotIndex >= 0 && slotIndex < localPushSlots.Length)
            {
                localPushSlots[slotIndex] = bear;

                // 吸附时忽略碰撞
                if (bear != null && cartCollider != null)
                {
                    var bearCol = bear.GetComponent<Collider2D>();
                    if (bearCol != null)
                    {
                        Physics2D.IgnoreCollision(cartCollider, bearCol, true);
                        Debug.Log($"[Cart] 忽略本地熊碰撞: slot {slotIndex}");
                    }
                }
            }
        }

        public void ReleaseLocalPushSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < localPushSlots.Length)
            {
                var bear = localPushSlots[slotIndex];
                if (bear != null && cartCollider != null)
                {
                    var bearCol = bear.GetComponent<Collider2D>();
                    if (bearCol != null)
                    {
                        Physics2D.IgnoreCollision(cartCollider, bearCol, false);
                        Debug.Log($"[Cart] 恢复本地熊碰撞: slot {slotIndex}");
                    }
                }
                localPushSlots[slotIndex] = null;
            }
        }

        /// <summary>
        /// 初始化本地模式（无网络时调用）
        /// </summary>
        public void InitializeLocalMode()
        {
            IsLocalOnlyMode = true;

            rb = GetComponent<Rigidbody2D>();
            cartCollider = GetComponent<Collider2D>();

            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 500f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 5f;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (cartCollider != null)
            {
                cartCollider.isTrigger = false;
                PhysicsMaterial2D cartMaterial = new PhysicsMaterial2D("CartMaterial")
                {
                    friction = 0.25f,
                    bounciness = 0f
                };
                cartCollider.sharedMaterial = cartMaterial;
            }

            EnsureVisuals();
            SetupPushZone();

            Debug.Log("[Cart] 本地模式初始化完成");
        }
    }
}
