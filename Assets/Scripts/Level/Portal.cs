using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BearCar.Player;
using BearCar.Cart;

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
        public float cooldownTime = 0.5f;

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

            // 检查目标过滤
            bool isPlayer = obj.GetComponent<LocalBearController>() != null || obj.GetComponent<BearController>() != null;
            bool isCart = obj.GetComponent<CartController>() != null;
            bool isAI = obj.GetComponent<BearAI>() != null;

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

            // 执行传送
            if (useFadeEffect)
            {
                StartCoroutine(TeleportWithFade(obj));
            }
            else
            {
                TeleportImmediate(obj);
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

            // 传送位置
            obj.transform.position = targetPoint.position;

            // 应用速度
            ApplyMomentum(rb, velocity);

            // 冷却
            cooldownTimer = cooldownTime;

            // 音效
            PlayTeleportSound();

            // 事件
            OnTeleport?.Invoke(obj);

            Debug.Log($"[Portal] {obj.name} 传送到 {targetPoint.name}");
        }

        private IEnumerator TeleportWithFade(GameObject obj)
        {
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
            var originalColors = new Color[renderers.Length];
            var originalScales = new Vector3[renderers.Length];
            Vector3 originalScale = obj.transform.localScale;
            Quaternion originalRotation = obj.transform.rotation;

            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
                originalScales[i] = renderers[i].transform.localScale;
            }

            // 暂停物理
            bool wasKinematic = false;
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            // === 消失动画 ===
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 2f); // EaseOut

                // 透明度渐变
                for (int i = 0; i < renderers.Length; i++)
                {
                    Color c = originalColors[i];
                    c.a = Mathf.Lerp(originalColors[i].a, 0f, easeT);
                    renderers[i].color = c;
                }

                // 缩放效果
                if (useScaleEffect)
                {
                    float scale = Mathf.Lerp(1f, 0.3f, easeT);
                    obj.transform.localScale = originalScale * scale;
                }

                // 旋转效果
                if (useRotateEffect)
                {
                    float angle = Mathf.Lerp(0f, 360f, easeT);
                    obj.transform.rotation = originalRotation * Quaternion.Euler(0, 0, angle);
                }

                yield return null;
            }

            // === 传送 ===
            obj.transform.position = targetPoint.position;
            obj.transform.rotation = originalRotation;

            // 音效
            PlayTeleportSound();

            // === 出现动画 ===
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // EaseOut cubic

                // 透明度渐变
                for (int i = 0; i < renderers.Length; i++)
                {
                    Color c = originalColors[i];
                    c.a = Mathf.Lerp(0f, originalColors[i].a, easeT);
                    renderers[i].color = c;
                }

                // 缩放效果
                if (useScaleEffect)
                {
                    float scale = Mathf.Lerp(0.3f, 1f, easeT);
                    obj.transform.localScale = originalScale * scale;
                }

                // 旋转效果（反向）
                if (useRotateEffect)
                {
                    float angle = Mathf.Lerp(-180f, 0f, easeT);
                    obj.transform.rotation = originalRotation * Quaternion.Euler(0, 0, angle);
                }

                yield return null;
            }

            // 恢复原始状态
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].color = originalColors[i];
            }
            obj.transform.localScale = originalScale;
            obj.transform.rotation = originalRotation;

            // 恢复物理
            if (rb != null)
            {
                rb.isKinematic = wasKinematic;
                ApplyMomentum(rb, velocity);
            }

            // 冷却
            cooldownTimer = cooldownTime;

            // 事件
            OnTeleport?.Invoke(obj);

            // 延迟移除传送标记（防止立即再次触发）
            yield return new WaitForSeconds(0.1f);
            teleportingObjects.Remove(obj);
            if (targetPortal != null)
            {
                targetPortal.teleportingObjects.Remove(obj);
            }

            Debug.Log($"[Portal] {obj.name} 传送到 {targetPoint.name} (带渐变)");
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
