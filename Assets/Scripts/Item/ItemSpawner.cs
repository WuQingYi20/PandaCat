using UnityEngine;
using System.Collections.Generic;

namespace BearCar.Item
{
    /// <summary>
    /// 道具生成器 - 定时在指定位置生成道具
    /// </summary>
    public class ItemSpawner : MonoBehaviour
    {
        [Header("=== 生成设置 ===")]
        [Tooltip("可生成的道具列表")]
        public ItemData[] possibleItems;

        [Tooltip("生成间隔（秒）")]
        public float spawnInterval = 10f;

        [Tooltip("最大同时存在数量")]
        public int maxItems = 3;

        [Tooltip("生成范围半径")]
        public float spawnRadius = 2f;

        [Header("=== 初始设置 ===")]
        [Tooltip("游戏开始时立即生成")]
        public bool spawnOnStart = true;

        [Tooltip("初始生成数量")]
        public int initialSpawnCount = 1;

        private float spawnTimer = 0f;
        private List<ItemPickup> spawnedItems = new List<ItemPickup>();

        private void Start()
        {
            // 尝试从 Resources 加载道具
            if (possibleItems == null || possibleItems.Length == 0)
            {
                possibleItems = Resources.LoadAll<ItemData>("Items");
                if (possibleItems.Length > 0)
                {
                    Debug.Log($"[ItemSpawner] 从 Resources/Items 加载了 {possibleItems.Length} 个道具");
                }
            }

            // 如果还是没有道具，创建运行时测试道具
            if (possibleItems == null || possibleItems.Length == 0)
            {
                CreateRuntimeItems();
            }

            if (spawnOnStart)
            {
                for (int i = 0; i < initialSpawnCount; i++)
                {
                    SpawnRandomItem();
                }
            }
        }

        private void CreateRuntimeItems()
        {
            Debug.Log("[ItemSpawner] 未找到道具配置，创建圣诞特别版道具 🎄");

            var items = new List<ItemData>();

            // 圣诞树饼干（绿色亲和）
            var treeCookie = ScriptableObject.CreateInstance<ItemData>();
            treeCookie.itemName = "圣诞树饼干";
            treeCookie.description = "绿色的饼干，红熊吃了更开心";
            treeCookie.itemType = ItemType.Food;
            treeCookie.colorAffinity = ColorAffinity.Green;
            treeCookie.shape = ItemShape.Triangle;
            treeCookie.itemColor = new Color(0.2f, 0.7f, 0.3f);
            treeCookie.baseEffect = 5f;
            treeCookie.affinityBonus = 5f;
            items.Add(treeCookie);

            // 圣诞老人饼干（红色亲和）
            var santaCookie = ScriptableObject.CreateInstance<ItemData>();
            santaCookie.itemName = "圣诞老人饼干";
            santaCookie.description = "红色的饼干，绿熊吃了更开心";
            santaCookie.itemType = ItemType.Food;
            santaCookie.colorAffinity = ColorAffinity.Red;
            santaCookie.shape = ItemShape.Heart;
            santaCookie.itemColor = new Color(0.9f, 0.2f, 0.2f);
            santaCookie.baseEffect = 5f;
            santaCookie.affinityBonus = 5f;
            items.Add(santaCookie);

            // 姜饼人（中性）
            var gingerbread = ScriptableObject.CreateInstance<ItemData>();
            gingerbread.itemName = "姜饼人";
            gingerbread.description = "公平的圣诞小点心";
            gingerbread.itemType = ItemType.Food;
            gingerbread.colorAffinity = ColorAffinity.None;
            gingerbread.shape = ItemShape.Circle;
            gingerbread.itemColor = new Color(0.8f, 0.5f, 0.2f);
            gingerbread.baseEffect = 3f;
            gingerbread.affinityBonus = 0f;
            items.Add(gingerbread);

            // 热可可（火箭推进）
            var hotCocoa = ScriptableObject.CreateInstance<ItemData>();
            hotCocoa.itemName = "热可可";
            hotCocoa.description = "暖暖的力量，冲鸭！";
            hotCocoa.itemType = ItemType.RocketBoost;
            hotCocoa.shape = ItemShape.Square;
            hotCocoa.itemColor = new Color(0.4f, 0.2f, 0.1f);
            hotCocoa.effectDuration = 3f;
            items.Add(hotCocoa);

            // 圣诞礼物盒（惊喜）
            var giftBox = ScriptableObject.CreateInstance<ItemData>();
            giftBox.itemName = "圣诞礼物";
            giftBox.description = "里面会是什么呢？";
            giftBox.itemType = ItemType.Special_GiftBox;
            giftBox.shape = ItemShape.Square;
            giftBox.itemColor = new Color(0.9f, 0.1f, 0.2f);
            giftBox.isBreakable = true;
            giftBox.rarity = ItemRarity.Rare;
            items.Add(giftBox);

            possibleItems = items.ToArray();
            Debug.Log($"[ItemSpawner] 创建了 {possibleItems.Length} 个圣诞道具 🎅");
        }

        private void Update()
        {
            // 清理已拾取的道具
            spawnedItems.RemoveAll(item => item == null);

            // 定时生成
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval && spawnedItems.Count < maxItems)
            {
                spawnTimer = 0f;
                SpawnRandomItem();
            }
        }

        private void SpawnRandomItem()
        {
            if (possibleItems == null || possibleItems.Length == 0)
            {
                Debug.LogWarning("[ItemSpawner] 没有可用的道具配置");
                return;
            }

            // 过滤掉 null 的道具
            var validItems = new List<ItemData>();
            foreach (var item in possibleItems)
            {
                if (item != null) validItems.Add(item);
            }

            if (validItems.Count == 0)
            {
                Debug.LogWarning("[ItemSpawner] 所有道具配置都是空的");
                return;
            }

            // 随机选择道具
            ItemData itemData = validItems[Random.Range(0, validItems.Count)];

            // 随机位置
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

            // 创建道具
            GameObject pickupObj = new GameObject($"Pickup_{itemData.itemName}");
            pickupObj.transform.position = spawnPos;

            // 先添加必要组件，避免 RequireComponent 问题
            var sr = pickupObj.AddComponent<SpriteRenderer>();
            var col = pickupObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            var pickup = pickupObj.AddComponent<ItemPickup>();
            if (pickup == null)
            {
                Debug.LogError("[ItemSpawner] 无法创建 ItemPickup 组件");
                Destroy(pickupObj);
                return;
            }

            pickup.itemData = itemData;
            pickup.respawnTime = 0f; // 不重生，由生成器控制

            // 设置视觉效果
            sr.sprite = CreateItemSprite(itemData.shape);
            sr.color = itemData.itemColor;
            sr.sortingOrder = 10;
            pickupObj.transform.localScale = Vector3.one * 0.8f;

            spawnedItems.Add(pickup);

            Debug.Log($"[ItemSpawner] 生成了 {itemData.itemName}");
        }

        /// <summary>
        /// 手动生成道具
        /// </summary>
        public void ForceSpawn()
        {
            SpawnRandomItem();
        }

        private Sprite CreateItemSprite(ItemShape shape)
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
                        case ItemShape.Triangle:
                            filled = dy > -radius * 0.5f && Mathf.Abs(dx) < (radius - dy) * 0.6f;
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
                        case ItemShape.Hexagon:
                            float hx = Mathf.Abs(dx);
                            float hy = Mathf.Abs(dy);
                            filled = hy < radius * 0.85f && hx < radius * 0.7f && (hx + hy * 0.5f) < radius * 0.85f;
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

        private void OnDrawGizmos()
        {
            // 绘制生成范围
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            // 绘制更详细的信息
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(transform.position, spawnRadius);

#if UNITY_EDITOR
            string info = $"生成器\n间隔: {spawnInterval}s\n最大: {maxItems}";
            UnityEditor.Handles.Label(transform.position + Vector3.up, info);
#endif
        }
    }
}
