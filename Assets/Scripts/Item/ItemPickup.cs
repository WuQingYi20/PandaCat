using UnityEngine;
using BearCar.Player;

namespace BearCar.Item
{
    /// <summary>
    /// 可拾取的道具 - 放置在场景中
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [Header("=== 道具数据 ===")]
        [Tooltip("拖入 ItemData 配置")]
        public ItemData itemData;

        [Tooltip("数量")]
        public int count = 1;

        [Header("=== 拾取设置 ===")]
        [Tooltip("自动拾取（触碰即拾取）")]
        public bool autoPickup = true;

        [Tooltip("拾取后重生时间（0=不重生）")]
        public float respawnTime = 0f;

        [Header("=== 视觉效果 ===")]
        [Tooltip("悬浮动画")]
        public bool floatAnimation = true;

        [Tooltip("旋转动画")]
        public bool rotateAnimation = true;

        [Tooltip("发光效果")]
        public bool glowEffect = true;

        private SpriteRenderer spriteRenderer;
        private Vector3 startPosition;
        private float animTime = 0f;
        private bool isPickedUp = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            startPosition = transform.position;
            SetupCollider();
        }

        private void Start()
        {
            // 在 Start 中设置视觉，确保 itemData 已被设置
            SetupVisuals();
        }

        /// <summary>
        /// 手动初始化（用于运行时创建的道具）
        /// </summary>
        public void Initialize()
        {
            startPosition = transform.position;
            SetupVisuals();
        }

        private void SetupCollider()
        {
            var col = GetComponent<Collider2D>();
            if (col == null)
            {
                var circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = 0.5f;
                col = circle;
            }
            col.isTrigger = true;
        }

        private void SetupVisuals()
        {
            if (itemData == null) return;

            // 使用图标或生成形状
            if (itemData.icon != null)
            {
                spriteRenderer.sprite = itemData.icon;
            }
            else
            {
                spriteRenderer.sprite = CreateShapeSprite(itemData.shape);
            }

            spriteRenderer.color = itemData.itemColor;
            spriteRenderer.sortingOrder = 10;

            // 设置大小
            transform.localScale = Vector3.one * 0.8f;
        }

        private Sprite CreateShapeSprite(ItemShape shape)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            float center = size / 2f;
            float radius = size / 2f - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    bool filled = false;

                    switch (shape)
                    {
                        case ItemShape.Circle:
                            filled = dist < radius;
                            break;

                        case ItemShape.Square:
                            filled = Mathf.Abs(dx) < radius * 0.7f && Mathf.Abs(dy) < radius * 0.7f;
                            break;

                        case ItemShape.Diamond:
                            filled = Mathf.Abs(dx) + Mathf.Abs(dy) < radius;
                            break;

                        case ItemShape.Star:
                            float starRadius = radius * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(angle * 2.5f)));
                            filled = dist < starRadius;
                            break;

                        case ItemShape.Heart:
                            float nx = dx / radius;
                            float ny = -dy / radius;
                            filled = Mathf.Pow(nx * nx + ny * ny - 1, 3) - nx * nx * ny * ny * ny < 0;
                            break;
                    }

                    pixels[y * size + x] = filled ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private void Update()
        {
            if (isPickedUp) return;

            animTime += Time.deltaTime;

            // 悬浮动画
            if (floatAnimation)
            {
                float yOffset = Mathf.Sin(animTime * 2f) * 0.15f;
                transform.position = startPosition + Vector3.up * yOffset;
            }

            // 旋转动画
            if (rotateAnimation)
            {
                transform.Rotate(0, 0, Time.deltaTime * 60f);
            }

            // 发光效果（脉冲）
            if (glowEffect && spriteRenderer != null)
            {
                float glow = 0.8f + Mathf.Sin(animTime * 3f) * 0.2f;
                Color c = itemData != null ? itemData.itemColor : Color.white;
                spriteRenderer.color = new Color(c.r * glow, c.g * glow, c.b * glow, c.a);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isPickedUp) return;
            if (!autoPickup) return;

            // 检查是否是玩家
            bool isPlayer = other.GetComponent<LocalBearController>() != null ||
                           other.GetComponent<BearController>() != null;

            if (isPlayer)
            {
                TryPickup(other.gameObject);
            }
        }

        /// <summary>
        /// 尝试拾取道具
        /// </summary>
        public bool TryPickup(GameObject picker)
        {
            if (isPickedUp) return false;
            if (itemData == null) return false;

            // 尝试添加到共享背包
            var inventory = SharedInventory.Instance;
            if (inventory == null)
            {
                Debug.LogWarning("[ItemPickup] SharedInventory 不存在!");
                return false;
            }

            if (inventory.AddItem(itemData, count))
            {
                OnPickedUp(picker);
                return true;
            }

            Debug.Log("[ItemPickup] 背包已满，无法拾取");
            return false;
        }

        private void OnPickedUp(GameObject picker)
        {
            isPickedUp = true;

            // 获取拾取者的玩家索引
            int playerIndex = -1;
            var localBear = picker.GetComponent<LocalBearController>();
            if (localBear != null)
            {
                playerIndex = localBear.PlayerIndex;
            }

            // 显示拾取提示
            ShowPickupNotification(playerIndex);

            // 播放音效
            if (itemData.pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(itemData.pickupSound, transform.position);
            }

            // 拾取动画
            StartCoroutine(PickupAnimation());
        }

        private void ShowPickupNotification(int playerIndex)
        {
            // 显示到日志UI
            var log = FindFirstObjectByType<ItemEffectLog>();
            if (log != null)
            {
                log.AddPickupLog(playerIndex, itemData, count);
            }
            else
            {
                string playerName = playerIndex >= 0 ? $"玩家{playerIndex + 1}" : "玩家";
                Debug.Log($"[ItemPickup] {playerName} 拾取了 {itemData.itemName} x{count}");
            }
        }

        private System.Collections.IEnumerator PickupAnimation()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;
            Color originalColor = spriteRenderer.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 缩小并上升
                transform.localScale = originalScale * (1f - t);
                transform.position += Vector3.up * Time.deltaTime * 3f;

                // 淡出
                Color c = originalColor;
                c.a = 1f - t;
                spriteRenderer.color = c;

                yield return null;
            }

            // 重生或销毁
            if (respawnTime > 0f)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSeconds(respawnTime);
                Respawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Respawn()
        {
            isPickedUp = false;
            transform.position = startPosition;
            transform.localScale = Vector3.one * 0.8f;
            spriteRenderer.enabled = true;
            spriteRenderer.color = itemData.itemColor;
        }

        private void OnDrawGizmos()
        {
            if (itemData == null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
                return;
            }

            // 道具主体颜色
            Gizmos.color = itemData.itemColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // 颜色亲和指示器
            if (itemData.colorAffinity != ColorAffinity.None)
            {
                Color affinityColor = itemData.colorAffinity == ColorAffinity.Green
                    ? new Color(0.2f, 0.9f, 0.3f)
                    : new Color(0.9f, 0.2f, 0.2f);
                Gizmos.color = affinityColor;
                Gizmos.DrawSphere(transform.position + Vector3.up * 0.7f, 0.12f);
            }

            // 稀有度指示 (稀有以上显示星星)
            if (itemData.rarity >= ItemRarity.Rare)
            {
                Color rarityColor = GetRarityGizmoColor(itemData.rarity);
                Gizmos.color = rarityColor;
                float starY = itemData.colorAffinity != ColorAffinity.None ? 0.95f : 0.7f;
                Gizmos.DrawSphere(transform.position + Vector3.up * starY, 0.08f);
            }

#if UNITY_EDITOR
            // 绘制名称和效果
            string label = itemData.itemName;
            if (itemData.colorAffinity != ColorAffinity.None)
            {
                string emoji = itemData.colorAffinity == ColorAffinity.Green ? "🟢" : "🔴";
                label = $"{emoji} {label}";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.1f, label);
#endif
        }

        private Color GetRarityGizmoColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return new Color(0.3f, 0.6f, 1f);
                case ItemRarity.Epic: return new Color(0.7f, 0.3f, 0.9f);
                case ItemRarity.Legendary: return new Color(1f, 0.7f, 0.2f);
                default: return Color.gray;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (itemData == null) return;

            // 选中时显示详细信息
            Gizmos.color = new Color(itemData.itemColor.r, itemData.itemColor.g, itemData.itemColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, 0.6f);

#if UNITY_EDITOR
            // 显示效果信息
            string info = $"{itemData.itemName}\n";
            info += $"类型: {itemData.itemType}\n";

            if (itemData.colorAffinity != ColorAffinity.None)
            {
                info += $"绿熊: +{itemData.GetEffectForPlayer(0)}\n";
                info += $"红熊: +{itemData.GetEffectForPlayer(1)}";
            }
            else if (itemData.itemType == ItemType.Food)
            {
                info += $"效果: +{itemData.baseEffect}";
            }

            UnityEditor.Handles.Label(transform.position + Vector3.right * 0.8f, info);
#endif
        }
    }
}
