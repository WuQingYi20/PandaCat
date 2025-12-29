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

            // 本地模式或服务器模式都可以处理
            if (!IsLocalOnlyMode && !IsServer) return;

            DetectSlope();
            ApplyForces();
            UpdateState();
        }


        private void SetupPushZone()
        {
            // 删除所有旧的 PushZone
            string[] oldNames = { "PushZone", "PushZoneLeft", "PushZoneRight" };
            foreach (var name in oldNames)
            {
                Transform old = transform.Find(name);
                if (old != null)
                {
                    Destroy(old.gameObject);
                }
            }

            // 获取Cart的碰撞体信息，以此为基准创建PushZone
            Vector2 cartSize = new Vector2(2f, 1f);  // 默认大小
            Vector2 cartOffset = Vector2.zero;

            var cartBox = GetComponent<BoxCollider2D>();
            if (cartBox != null)
            {
                cartSize = cartBox.size;
                cartOffset = cartBox.offset;
            }

            // 创建推车区域，比Cart稍大，方便抓取
            // 宽度增加2单位（左右各1），高度增加1.5单位
            GameObject pushZone = new GameObject("PushZone");
            pushZone.transform.SetParent(transform);
            pushZone.transform.localPosition = new Vector3(cartOffset.x, cartOffset.y, 0);

            // 设置Layer（如果PushZone层存在）
            int pushZoneLayer = LayerMask.NameToLayer("PushZone");
            if (pushZoneLayer >= 0)
            {
                pushZone.layer = pushZoneLayer;
            }

            var col = pushZone.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(cartSize.x + 2f, cartSize.y + 1.5f);

            pushZone.AddComponent<CartPushZone>();

            Debug.Log($"[Cart] PushZone 设置完成，Cart大小: {cartSize}, PushZone大小: {col.size}, 偏移: {cartOffset}");
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

            // 确保碰撞体匹配精灵大小
            EnsureColliderMatchesSprite(sr);

            Debug.Log($"[Cart] Visuals set up at position {transform.position}");
        }

        private void EnsureColliderMatchesSprite(SpriteRenderer sr)
        {
            // 获取或创建 BoxCollider2D
            var boxCol = GetComponent<BoxCollider2D>();
            if (boxCol == null)
            {
                boxCol = gameObject.AddComponent<BoxCollider2D>();
            }

            // 根据精灵大小设置碰撞体
            if (sr != null && sr.sprite != null)
            {
                // 精灵是 64x32 像素，32ppu，所以是 2x1 单位
                Vector2 spriteSize = sr.sprite.bounds.size;
                boxCol.size = spriteSize;
                boxCol.offset = Vector2.zero;

                Debug.Log($"[Cart] 碰撞体大小设置为: {spriteSize}");
            }
            else
            {
                // 默认大小
                boxCol.size = new Vector2(2f, 1f);
                boxCol.offset = Vector2.zero;
            }

            // 更新缓存的碰撞体引用
            cartCollider = boxCol;
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

        // 本地模式用的状态变量
        private int localActivePushers = 0;
        private CartState localState = CartState.Idle;

        private void UpdateState()
        {
            // 本地模式下跳过 NetworkVariable 操作
            if (IsLocalOnlyMode) return;

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

        /// <summary>
        /// 获取所有吸附在车上的本地熊
        /// </summary>
        public List<LocalBearController> GetAttachedLocalBears()
        {
            var result = new List<LocalBearController>();
            foreach (var bear in localPushSlots)
            {
                if (bear != null)
                {
                    result.Add(bear);
                }
            }
            return result;
        }

        /// <summary>
        /// 检查是否有熊吸附
        /// </summary>
        public bool HasAttachedBears()
        {
            foreach (var bear in localPushSlots)
            {
                if (bear != null) return true;
            }
            foreach (var bear in pushSlots)
            {
                if (bear != null) return true;
            }
            return false;
        }

        /// <summary>
        /// 传送车和所有吸附的熊（整体移动）
        /// </summary>
        public void TeleportWithAttachedBears(Vector3 targetPosition)
        {
            // 计算位移
            Vector3 offset = targetPosition - transform.position;

            // 传送车 - 同时设置transform和rigidbody位置确保同步
            TeleportObject(gameObject, targetPosition);

            // 移动所有吸附的本地熊
            foreach (var bear in localPushSlots)
            {
                if (bear != null)
                {
                    TeleportObject(bear.gameObject, bear.transform.position + offset);
                }
            }

            // 移动网络熊
            foreach (var bear in pushSlots)
            {
                if (bear != null)
                {
                    TeleportObject(bear.gameObject, bear.transform.position + offset);
                }
            }

            // 强制同步物理系统
            Physics2D.SyncTransforms();

            Debug.Log($"[Cart] 整体传送到 {targetPosition}，包含 {GetAttachedLocalBears().Count} 只吸附的熊");
        }

        /// <summary>
        /// 安全地传送一个对象（同步transform和rigidbody）
        /// </summary>
        private void TeleportObject(GameObject obj, Vector3 targetPos)
        {
            if (obj == null) return;

            // 设置transform位置
            obj.transform.position = targetPos;

            // 同步Rigidbody2D位置（关键！）
            var rb2d = obj.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                // 暂时禁用插值以避免视觉延迟
                var oldInterpolation = rb2d.interpolation;
                rb2d.interpolation = RigidbodyInterpolation2D.None;

                // 直接设置rigidbody位置
                rb2d.position = targetPos;

                // 重置速度（可选，根据需要）
                // rb2d.linearVelocity = Vector2.zero;

                // 恢复插值设置（延迟一帧）
                StartCoroutine(RestoreInterpolation(rb2d, oldInterpolation));
            }
        }

        private System.Collections.IEnumerator RestoreInterpolation(Rigidbody2D rb, RigidbodyInterpolation2D interpolation)
        {
            yield return new WaitForFixedUpdate();
            yield return null; // 等待一帧
            if (rb != null)
            {
                rb.interpolation = interpolation;
            }
        }
    }
}
