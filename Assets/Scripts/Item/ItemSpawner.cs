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
            Debug.Log("[ItemSpawner] 未找到道具配置，创建运行时测试道具");

            var items = new List<ItemData>();

            // 加速道具
            var speedBoost = ScriptableObject.CreateInstance<ItemData>();
            speedBoost.itemName = "加速药水";
            speedBoost.description = "全队加速";
            speedBoost.itemType = ItemType.SpeedBoost;
            speedBoost.shape = ItemShape.Diamond;
            speedBoost.itemColor = new Color(0.2f, 0.8f, 1f);
            speedBoost.effectValue = 1.5f;
            speedBoost.effectDuration = 5f;
            items.Add(speedBoost);

            // 体力恢复
            var stamina = ScriptableObject.CreateInstance<ItemData>();
            stamina.itemName = "体力药水";
            stamina.description = "恢复体力";
            stamina.itemType = ItemType.StaminaRecover;
            stamina.shape = ItemShape.Heart;
            stamina.itemColor = new Color(0.2f, 1f, 0.4f);
            stamina.effectValue = 3f;
            stamina.effectDuration = 0f;
            items.Add(stamina);

            // 磁铁
            var magnet = ScriptableObject.CreateInstance<ItemData>();
            magnet.itemName = "磁铁";
            magnet.description = "吸引道具";
            magnet.itemType = ItemType.Magnet;
            magnet.shape = ItemShape.Star;
            magnet.itemColor = new Color(1f, 0.3f, 0.3f);
            magnet.effectValue = 8f;
            magnet.effectDuration = 5f;
            items.Add(magnet);

            possibleItems = items.ToArray();
            Debug.Log($"[ItemSpawner] 创建了 {possibleItems.Length} 个运行时道具");
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
            if (possibleItems == null || possibleItems.Length == 0) return;

            // 随机选择道具
            ItemData itemData = possibleItems[Random.Range(0, possibleItems.Length)];

            // 随机位置
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

            // 创建道具
            GameObject pickupObj = new GameObject($"Pickup_{itemData.itemName}");
            pickupObj.transform.position = spawnPos;

            var pickup = pickupObj.AddComponent<ItemPickup>();
            pickup.itemData = itemData;
            pickup.respawnTime = 0f; // 不重生，由生成器控制

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
