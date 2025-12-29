using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BearCar.Player;
using BearCar.Cart;
using BearCar.Core;

namespace BearCar.Level
{
    /// <summary>
    /// 传送门 - 关卡设计师友好的传送系统
    ///
    /// Aha 机制:
    /// - 动量保持: 传送后保持速度，可用于加速冲刺
    /// - 合作触发: 需要多个玩家站在压力板上
    /// - 单向传送: 只能从一边进入
    /// - 选择性传送: 只传送玩家或只传送车
    /// - 限时传送: 只在特定时间窗口开启
    /// </summary>
    public class Portal : MonoBehaviour
    {
        [Header("=== 基础设置 ===")]
        [Tooltip("传送目标点")]
        public Transform targetPoint;

        [Tooltip("传送门ID，用于配对")]
        public string portalId = "Portal_A";

        [Header("=== 传送类型 ===")]
        [Tooltip("双向传送 / 单向传送")]
        public PortalDirection direction = PortalDirection.Bidirectional;

        [Tooltip("传送什么对象")]
        public PortalTarget targetFilter = PortalTarget.All;

        [Header("=== Aha 机制 ===")]
        [Tooltip("保持传送前的速度（可用于加速技巧）")]
        public bool preserveMomentum = true;

        [Tooltip("速度倍数（1=保持原速，2=加倍）")]
        [Range(0.5f, 3f)]
        public float momentumMultiplier = 1f;

        [Tooltip("传送后速度方向")]
        public MomentumDirection momentumDirection = MomentumDirection.Preserve;

        [Header("=== 触发条件 ===")]
        [Tooltip("触发方式")]
        public PortalActivation activation = PortalActivation.AlwaysActive;

        [Tooltip("需要的玩家数量（合作触发时）")]
        [Range(1, 4)]
        public int requiredPlayers = 1;

        [Tooltip("需要站在压力板上的秒数")]
        public float activationDelay = 0f;

        [Tooltip("激活后持续时间（0=永久）")]
        public float activeDuration = 0f;

        [Header("=== 冷却设置 ===")]
        [Tooltip("传送冷却时间")]
        public float cooldownTime = 0.1f;

        [Header("=== 渐变效果 ===")]
        [Tooltip("启用传送渐变效果")]
        public bool useFadeEffect = true;

        [Tooltip("消失时间")]
        [Range(0.1f, 1f)]
        public float fadeOutDuration = 0.3f;

        [Tooltip("出现时间")]
        [Range(0.1f, 1f)]
        public float fadeInDuration = 0.3f;

        [Tooltip("传送时的缩放效果")]
        public bool useScaleEffect = true;

        [Tooltip("传送时的旋转效果")]
        public bool useRotateEffect = true;

        [Header("=== 视觉设置 ===")]
        [Tooltip("传送门颜色")]
        public Color portalColor = new Color(0.5f, 0.8f, 1f, 0.8f);

        [Tooltip("激活时的颜色")]
        public Color activeColor = new Color(0.3f, 1f, 0.5f, 0.9f);

        [Tooltip("未激活时的颜色")]
        public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("=== 音效 (可选) ===")]
        public AudioClip teleportSound;
        public AudioClip activateSound;

        // 运行时状态
        private bool isActive = true;
        private float cooldownTimer = 0f;
        private float activationTimer = 0f;
        private float activeTimer = 0f;
        private int currentPlayersInZone = 0;
        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;

        // 正在传送中的对象（防止重复传送）
        private HashSet<GameObject> teleportingObjects = new HashSet<GameObject>();

        // 存储对象的原始缩放（用于确保恢复）
        private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();

        // 正在进行的传送协程
        private Dictionary<GameObject, Coroutine> activeTeleportCoroutines = new Dictionary<GameObject, Coroutine>();

        // 事件
        public System.Action<GameObject> OnTeleport;
        public System.Action OnActivate;
        public System.Action OnDeactivate;

        public bool IsActive => isActive && cooldownTimer <= 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null && (teleportSound != null || activateSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            SetupCollider();
            SetupVisuals();
        }

        private void Start()
        {
            // 根据激活方式初始化状态
            isActive = activation == PortalActivation.AlwaysActive;
            UpdateVisuals();
        }

        private void Update()
        {
            // 更新冷却
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            // 更新激活计时
            if (activation == PortalActivation.CoopTrigger && currentPlayersInZone >= requiredPlayers)
            {
                activationTimer += Time.deltaTime;
                if (activationTimer >= activationDelay && !isActive)
                {
                    Activate();
                }
            }
            else
            {
                activationTimer = 0f;
            }

            // 更新激活持续时间
            if (isActive && activeDuration > 0f)
            {
                activeTimer += Time.deltaTime;
                if (activeTimer >= activeDuration)
                {
                    Deactivate();
                }
            }

            UpdateVisuals();
        }

        private void SetupCollider()
        {
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(2f, 2f);
                box.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }
        }

        private void SetupVisuals()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (spriteRenderer.sprite == null)
            {
                // 创建默认的传送门外观
                int size = 64;
                Texture2D tex = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - size / 2f;
                        float dy = y - size / 2f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float ring = Mathf.Abs(dist - size * 0.35f) < size * 0.1f ? 1f : 0f;
                        float center = dist < size * 0.25f ? 0.5f : 0f;
                        pixels[y * size + x] = new Color(1, 1, 1, ring + center);
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply();
                spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            }

            spriteRenderer.sortingOrder = 5;
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer == null) return;

            Color targetColor;
            if (!isActive)
            {
                targetColor = inactiveColor;
            }
            else if (cooldownTimer > 0f)
            {
                targetColor = Color.Lerp(portalColor, inactiveColor, cooldownTimer / cooldownTime);
            }
            else
            {
                targetColor = activation == PortalActivation.CoopTrigger && currentPlayersInZone >= requiredPlayers
                    ? activeColor
                    : portalColor;
            }

            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 5f);

            // 旋转动画
            if (isActive)
            {
                transform.Rotate(0, 0, Time.deltaTime * 30f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 统计玩家数量
            if (other.GetComponent<LocalBearController>() != null || other.GetComponent<BearController>() != null)
            {
                currentPlayersInZone++;
            }

            // 尝试传送
            TryTeleport(other.gameObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<LocalBearController>() != null || other.GetComponent<BearController>() != null)
            {
                currentPlayersInZone = Mathf.Max(0, currentPlayersInZone - 1);
            }
        }

        private void TryTeleport(GameObject obj)
        {
            if (!IsActive) return;
            if (targetPoint == null) return;

            // 检查是否正在传送中
            if (teleportingObjects.Contains(obj)) return;

            // 检查传送方向
            if (direction == PortalDirection.OneWayIn)
            {
                // 单向入口，不能从这里传送出去
                return;
            }

            // 检查目标过滤（使用GetComponentInParent以支持子物体触发）
            var localBear = obj.GetComponent<LocalBearController>();
            var netBear = obj.GetComponent<BearController>();
            // Cart可能是通过子物体(如PushZone)触发的，所以用GetComponentInParent
            var cart = obj.GetComponentInParent<CartController>();
            bool isPlayer = localBear != null || netBear != null;
            bool isCart = cart != null;
            bool isAI = obj.GetComponent<BearAI>() != null;

            // 如果是Cart的子物体触发的，使用Cart作为传送目标
            if (isCart && obj != cart.gameObject)
            {
                // 检查Cart本身是否已经在传送中
                if (teleportingObjects.Contains(cart.gameObject)) return;
                obj = cart.gameObject;
            }

            // 如果是吸附状态的熊，改为传送车（整体传送）
            if (localBear != null && localBear.IsAttached)
            {
                // 找到吸附的车，改为传送车
                var attachedCart = FindAttachedCart(localBear);
                if (attachedCart != null)
                {
                    obj = attachedCart.gameObject;
                    cart = attachedCart;
                    isCart = true;
                    isPlayer = false;
                }
            }

            switch (targetFilter)
            {
                case PortalTarget.PlayersOnly:
                    if (!isPlayer) return;
                    break;
                case PortalTarget.CartOnly:
                    if (!isCart) return;
                    break;
                case PortalTarget.PlayersAndAI:
                    if (!isPlayer && !isAI) return;
                    break;
                case PortalTarget.CartAndPlayers:
                    if (!isCart && !isPlayer) return;
                    break;
            }

            // 执行传送（车带熊整体传送，不使用渐变效果以避免复杂性）
            if (isCart && cart != null && cart.HasAttachedBears())
            {
                TeleportCartWithBears(cart);
            }
            else if (useFadeEffect)
            {
                StartCoroutine(TeleportWithFade(obj));
            }
            else
            {
                TeleportImmediate(obj);
            }
        }

        /// <summary>
        /// 找到熊吸附的车
        /// </summary>
        private CartController FindAttachedCart(LocalBearController bear)
        {
            var carts = FindObjectsByType<CartController>(FindObjectsSortMode.None);
            foreach (var cart in carts)
            {
                var attachedBears = cart.GetAttachedLocalBears();
                if (attachedBears.Contains(bear))
                {
                    return cart;
                }
            }
            return null;
        }

        /// <summary>
        /// 传送车和所有吸附的熊
        /// </summary>
        private void TeleportCartWithBears(CartController cart)
        {
            // 标记车和所有熊为传送中
            teleportingObjects.Add(cart.gameObject);
            var attachedBears = cart.GetAttachedLocalBears();
            foreach (var bear in attachedBears)
            {
                teleportingObjects.Add(bear.gameObject);
            }

            // 也标记Cart的所有子物体（如PushZone），防止它们触发出口Portal
            foreach (Transform child in cart.transform)
            {
                teleportingObjects.Add(child.gameObject);
            }

            // 通知目标传送门
            var targetPortal = targetPoint.GetComponent<Portal>();
            if (targetPortal != null)
            {
                targetPortal.teleportingObjects.Add(cart.gameObject);
                foreach (var bear in attachedBears)
                {
                    targetPortal.teleportingObjects.Add(bear.gameObject);
                }
                foreach (Transform child in cart.transform)
                {
                    targetPortal.teleportingObjects.Add(child.gameObject);
                }
            }

            // 保存车的速度
            Vector2 velocity = Vector2.zero;
            Rigidbody2D rb = cart.GetComponent<Rigidbody2D>();
            if (rb != null && preserveMomentum)
            {
                velocity = rb.linearVelocity * momentumMultiplier;
            }

            // 整体传送
            cart.TeleportWithAttachedBears(targetPoint.position);

            // 应用速度
            if (rb != null)
            {
                ApplyMomentum(rb, velocity);
            }

            // 双向冷却
            cooldownTimer = cooldownTime;
            if (targetPortal != null)
            {
                targetPortal.cooldownTimer = targetPortal.cooldownTime;
            }

            // 音效
            PlayTeleportSound();

            // 事件
            OnTeleport?.Invoke(cart.gameObject);

            // 延迟清理传送标记
            StartCoroutine(CleanupCartTeleport(cart, attachedBears, targetPortal));

            Debug.Log($"[Portal] 车和 {attachedBears.Count} 只熊整体传送到 {targetPoint.name}");
        }

        private System.Collections.IEnumerator CleanupCartTeleport(CartController cart, List<LocalBearController> bears, Portal targetPortal)
        {
            yield return new WaitForSeconds(0.2f);

            if (cart != null)
            {
                teleportingObjects.Remove(cart.gameObject);

                // 清理子物体
                foreach (Transform child in cart.transform)
                {
                    teleportingObjects.Remove(child.gameObject);
                }
            }

            foreach (var bear in bears)
            {
                if (bear != null)
                {
                    teleportingObjects.Remove(bear.gameObject);
                }
            }

            if (targetPortal != null)
            {
                if (cart != null)
                {
                    targetPortal.teleportingObjects.Remove(cart.gameObject);
                    foreach (Transform child in cart.transform)
                    {
                        targetPortal.teleportingObjects.Remove(child.gameObject);
                    }
                }

                foreach (var bear in bears)
                {
                    if (bear != null)
                    {
                        targetPortal.teleportingObjects.Remove(bear.gameObject);
                    }
                }
            }
        }

        private void TeleportImmediate(GameObject obj)
        {
            Vector2 velocity = Vector2.zero;
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();

            // 保存速度
            if (rb != null && preserveMomentum)
            {
                velocity = rb.linearVelocity * momentumMultiplier;
            }

            // 安全传送（同步transform和rigidbody）
            SafeTeleport(obj, targetPoint.position);

            // 应用速度
            ApplyMomentum(rb, velocity);

            // 通知传送完成
            NotifyTeleportComplete(obj);

            // 双向冷却 - 入口和出口都进入冷却
            cooldownTimer = cooldownTime;
            var targetPortal = targetPoint.GetComponent<Portal>();
            if (targetPortal != null)
            {
                targetPortal.cooldownTimer = targetPortal.cooldownTime;
            }

            // 音效
            PlayTeleportSound();

            // 事件
            OnTeleport?.Invoke(obj);

            Debug.Log($"[Portal] {obj.name} 传送到 {targetPoint.name}");
        }

        /// <summary>
        /// 安全传送 - 同步transform和rigidbody位置，避免collider脱离
        /// </summary>
        private void SafeTeleport(GameObject obj, Vector3 targetPos)
        {
            if (obj == null) return;

            obj.transform.position = targetPos;

            var rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 暂时禁用插值
                var oldInterpolation = rb.interpolation;
                rb.interpolation = RigidbodyInterpolation2D.None;

                // 同步rigidbody位置
                rb.position = targetPos;

                // 延迟恢复插值
                StartCoroutine(RestoreRbInterpolation(rb, oldInterpolation));
            }

            // 强制同步物理
            Physics2D.SyncTransforms();
        }

        private System.Collections.IEnumerator RestoreRbInterpolation(Rigidbody2D rb, RigidbodyInterpolation2D interpolation)
        {
            yield return new WaitForFixedUpdate();
            if (rb != null)
            {
                rb.interpolation = interpolation;
            }
        }

        private IEnumerator TeleportWithFade(GameObject obj)
        {
            // 检查对象是否已经在传送中
            if (activeTeleportCoroutines.ContainsKey(obj))
            {
                Debug.LogWarning($"[Portal] {obj.name} 已经在传送中，跳过");
                yield break;
            }

            // 标记为正在传送
            teleportingObjects.Add(obj);

            // 通知目标传送门也标记此对象
            var targetPortal = targetPoint.GetComponent<Portal>();
            if (targetPortal != null)
            {
                targetPortal.teleportingObjects.Add(obj);
            }

            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            Vector2 velocity = Vector2.zero;

            // 保存速度
            if (rb != null && preserveMomentum)
            {
                velocity = rb.linearVelocity * momentumMultiplier;
            }

            // 获取所有 SpriteRenderer
            var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            var originalRendererColors = new Color[renderers.Length];

            // 获取并存储原始缩放（如果还没有存储）
            // 这确保即使对象已经被缩放过，我们也能恢复到正确的大小
            Vector3 trueOriginalScale;
            if (originalScales.ContainsKey(obj))
            {
                trueOriginalScale = originalScales[obj];
            }
            else
            {
                trueOriginalScale = obj.transform.localScale;
                originalScales[obj] = trueOriginalScale;
            }

            Quaternion originalRotation = obj.transform.rotation;

            for (int i = 0; i < renderers.Length; i++)
            {
                originalRendererColors[i] = renderers[i].color;
            }

            // 暂停物理
            bool wasKinematic = false;
            float originalGravityScale = 1f;
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                originalGravityScale = rb.gravityScale;
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            // === 消失动画 ===
            float elapsed = 0f;
            bool interrupted = false;
            while (elapsed < fadeOutDuration && !interrupted)
            {
                if (obj == null)
                {
                    interrupted = true;
                    break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 2f); // EaseOut

                // 透明度渐变
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        Color c = originalRendererColors[i];
                        c.a = Mathf.Lerp(originalRendererColors[i].a, 0f, easeT);
                        renderers[i].color = c;
                    }
                }

                // 缩放效果
                if (useScaleEffect)
                {
                    float scale = Mathf.Lerp(1f, 0.3f, easeT);
                    obj.transform.localScale = trueOriginalScale * scale;
                }

                // 旋转效果
                if (useRotateEffect)
                {
                    float angle = Mathf.Lerp(0f, 360f, easeT);
                    obj.transform.rotation = originalRotation * Quaternion.Euler(0, 0, angle);
                }

                yield return null;
            }

            if (interrupted || obj == null)
            {
                CleanupTeleport(obj, targetPortal);
                yield break;
            }

            // === 传送 ===
            // 同步设置transform和rigidbody位置
            obj.transform.position = targetPoint.position;
            obj.transform.rotation = originalRotation;
            if (rb != null)
            {
                rb.position = targetPoint.position;
            }
            Physics2D.SyncTransforms();

            // 音效
            PlayTeleportSound();

            // === 出现动画 ===
            elapsed = 0f;
            while (elapsed < fadeInDuration && !interrupted)
            {
                if (obj == null)
                {
                    interrupted = true;
                    break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // EaseOut cubic

                // 透明度渐变
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        Color c = originalRendererColors[i];
                        c.a = Mathf.Lerp(0f, originalRendererColors[i].a, easeT);
                        renderers[i].color = c;
                    }
                }

                // 缩放效果
                if (useScaleEffect)
                {
                    float scale = Mathf.Lerp(0.3f, 1f, easeT);
                    obj.transform.localScale = trueOriginalScale * scale;
                }

                // 旋转效果（反向）
                if (useRotateEffect)
                {
                    float angle = Mathf.Lerp(-180f, 0f, easeT);
                    obj.transform.rotation = originalRotation * Quaternion.Euler(0, 0, angle);
                }

                yield return null;
            }

            if (interrupted || obj == null)
            {
                CleanupTeleport(obj, targetPortal);
                yield break;
            }

            // === 恢复原始状态 ===
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = originalRendererColors[i];
                }
            }
            obj.transform.localScale = trueOriginalScale;
            obj.transform.rotation = originalRotation;

            // 恢复物理
            if (rb != null)
            {
                rb.isKinematic = wasKinematic;
                rb.gravityScale = originalGravityScale;
                ApplyMomentum(rb, velocity);
            }

            // 通知熊的跳跃系统重置地面检测
            NotifyTeleportComplete(obj);

            // 双向冷却 - 入口和出口都进入冷却
            cooldownTimer = cooldownTime;
            if (targetPortal != null)
            {
                targetPortal.cooldownTimer = targetPortal.cooldownTime;
            }

            // 事件
            OnTeleport?.Invoke(obj);

            // 延迟移除传送标记（防止立即再次触发）
            yield return new WaitForSeconds(0.15f);
            CleanupTeleport(obj, targetPortal);

            Debug.Log($"[Portal] {obj.name} 传送到 {targetPoint.name} (带渐变)");
        }

        /// <summary>
        /// 清理传送状态
        /// </summary>
        private void CleanupTeleport(GameObject obj, Portal targetPortal)
        {
            if (obj != null)
            {
                // 先恢复缩放再移除记录
                if (originalScales.TryGetValue(obj, out Vector3 savedScale))
                {
                    obj.transform.localScale = savedScale;
                    originalScales.Remove(obj);
                }

                teleportingObjects.Remove(obj);
                activeTeleportCoroutines.Remove(obj);
            }

            if (targetPortal != null)
            {
                if (obj != null && targetPortal.originalScales.TryGetValue(obj, out Vector3 targetSavedScale))
                {
                    obj.transform.localScale = targetSavedScale;
                    targetPortal.originalScales.Remove(obj);
                }
                targetPortal.teleportingObjects.Remove(obj);
            }
        }

        /// <summary>
        /// 当Portal被禁用时，恢复所有正在传送对象的状态
        /// </summary>
        private void OnDisable()
        {
            // 恢复所有对象的原始缩放
            foreach (var kvp in originalScales)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.localScale = kvp.Value;
                }
            }
            originalScales.Clear();
            teleportingObjects.Clear();
            activeTeleportCoroutines.Clear();
        }

        /// <summary>
        /// 通知对象传送完成，重置相关系统
        /// </summary>
        private void NotifyTeleportComplete(GameObject obj)
        {
            // 通知跳跃系统
            var jumpSystem = obj.GetComponent<JumpSystem>();
            if (jumpSystem != null)
            {
                jumpSystem.OnTeleported();
            }

            // 通知熊控制器
            var bearController = obj.GetComponent<LocalBearController>();
            if (bearController != null)
            {
                bearController.OnTeleported();
            }
        }

        private void ApplyMomentum(Rigidbody2D rb, Vector2 velocity)
        {
            if (rb == null || !preserveMomentum) return;

            switch (momentumDirection)
            {
                case MomentumDirection.Preserve:
                    rb.linearVelocity = velocity;
                    break;
                case MomentumDirection.Reverse:
                    rb.linearVelocity = -velocity;
                    break;
                case MomentumDirection.TowardsTarget:
                    rb.linearVelocity = targetPoint.right * velocity.magnitude;
                    break;
                case MomentumDirection.Zero:
                    rb.linearVelocity = Vector2.zero;
                    break;
            }
        }

        private void PlayTeleportSound()
        {
            if (audioSource != null && teleportSound != null)
            {
                audioSource.PlayOneShot(teleportSound);
            }
        }

        public void Activate()
        {
            if (isActive) return;

            isActive = true;
            activeTimer = 0f;

            if (audioSource != null && activateSound != null)
            {
                audioSource.PlayOneShot(activateSound);
            }

            OnActivate?.Invoke();
            Debug.Log($"[Portal] {portalId} 已激活");
        }

        public void Deactivate()
        {
            if (!isActive) return;

            isActive = false;

            OnDeactivate?.Invoke();
            Debug.Log($"[Portal] {portalId} 已关闭");
        }

        /// <summary>
        /// 外部触发激活（用于机关联动）
        /// </summary>
        public void ExternalActivate(float duration = 0f)
        {
            activeDuration = duration;
            Activate();
        }

        private void OnDrawGizmos()
        {
            // 绘制传送门
            Gizmos.color = isActive ? portalColor : inactiveColor;
            Gizmos.DrawWireSphere(transform.position, 1f);

            // 绘制到目标的连线
            if (targetPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, targetPoint.position);
                Gizmos.DrawWireSphere(targetPoint.position, 0.3f);

                // 绘制箭头
                Vector3 dir = (targetPoint.position - transform.position).normalized;
                Vector3 mid = (transform.position + targetPoint.position) / 2f;
                Gizmos.DrawLine(mid, mid + Quaternion.Euler(0, 0, 30) * -dir * 0.5f);
                Gizmos.DrawLine(mid, mid + Quaternion.Euler(0, 0, -30) * -dir * 0.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 显示触发范围
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
            }
        }
    }

    public enum PortalDirection
    {
        [Tooltip("双向传送")]
        Bidirectional,
        [Tooltip("只能进入此传送门")]
        OneWayIn,
        [Tooltip("只能从此传送门出来")]
        OneWayOut
    }

    public enum PortalTarget
    {
        [Tooltip("传送所有对象")]
        All,
        [Tooltip("只传送玩家")]
        PlayersOnly,
        [Tooltip("只传送车")]
        CartOnly,
        [Tooltip("传送玩家和AI")]
        PlayersAndAI,
        [Tooltip("传送车和玩家")]
        CartAndPlayers
    }

    public enum PortalActivation
    {
        [Tooltip("始终激活")]
        AlwaysActive,
        [Tooltip("需要多人合作触发")]
        CoopTrigger,
        [Tooltip("需要外部触发（机关/脚本）")]
        ExternalTrigger,
        [Tooltip("定时开关")]
        Timed
    }

    public enum MomentumDirection
    {
        [Tooltip("保持原方向")]
        Preserve,
        [Tooltip("反转方向")]
        Reverse,
        [Tooltip("朝向目标点方向")]
        TowardsTarget,
        [Tooltip("速度归零")]
        Zero
    }
}
