#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Item;

namespace BearCar.Editor
{
    public class ItemSetupEditor : EditorWindow
    {
        [MenuItem("BearCar/道具系统/创建道具管理器")]
        public static void CreateItemManager()
        {
            if (FindFirstObjectByType<SharedInventory>() != null)
            {
                Debug.Log("SharedInventory 已存在");
                Selection.activeGameObject = FindFirstObjectByType<SharedInventory>().gameObject;
                return;
            }

            GameObject manager = new GameObject("ItemManager");
            manager.AddComponent<SharedInventory>();
            manager.AddComponent<InventoryUI>();
            manager.AddComponent<ItemEffectHandler>();
            manager.AddComponent<ItemEffectLog>();

            Undo.RegisterCreatedObjectUndo(manager, "Create Item Manager");
            Selection.activeGameObject = manager;

            Debug.Log("道具管理器已创建!");
        }

        [MenuItem("BearCar/道具系统/创建所有预设道具")]
        public static void CreateAllPresetItems()
        {
            string folderPath = "Assets/Resources/Items";
            EnsureFolderExists(folderPath);

            // ========== 绿色系水果 ==========
            CreateFoodItem(folderPath, "Kiwi", "猕猴桃",
                "绿色的猕猴桃，红熊吃了更有效！",
                ColorAffinity.Green, new Color(0.6f, 0.8f, 0.2f), ItemShape.Circle,
                5f, 5f, ItemRarity.Common);

            CreateFoodItem(folderPath, "GreenMelon", "绿色蜜瓜",
                "清甜的蜜瓜，红熊的最爱",
                ColorAffinity.Green, new Color(0.7f, 0.9f, 0.5f), ItemShape.Circle,
                5f, 5f, ItemRarity.Common);

            CreateFoodItem(folderPath, "GreenApple", "青苹果",
                "酸甜的青苹果",
                ColorAffinity.Green, new Color(0.5f, 0.9f, 0.3f), ItemShape.Circle,
                5f, 5f, ItemRarity.Common);

            // ========== 红色系水果 ==========
            CreateFoodItem(folderPath, "Cherry", "樱桃",
                "鲜红的樱桃，绿熊吃了更有效！",
                ColorAffinity.Red, new Color(0.9f, 0.2f, 0.2f), ItemShape.Heart,
                5f, 5f, ItemRarity.Common);

            CreateFoodItem(folderPath, "RedApple", "红苹果",
                "香甜的红苹果",
                ColorAffinity.Red, new Color(0.9f, 0.3f, 0.3f), ItemShape.Circle,
                5f, 5f, ItemRarity.Common);

            CreateFoodItem(folderPath, "Strawberry", "草莓",
                "可爱的草莓",
                ColorAffinity.Red, new Color(1f, 0.4f, 0.4f), ItemShape.Heart,
                5f, 5f, ItemRarity.Common);

            // ========== 中性食物 ==========
            CreateFoodItem(folderPath, "Banana", "香蕉",
                "黄澄澄的香蕉，人人都爱",
                ColorAffinity.None, new Color(1f, 0.9f, 0.3f), ItemShape.Diamond,
                2f, 0f, ItemRarity.Common);

            CreateFoodItem(folderPath, "Mentos", "曼妥思",
                "薄荷糖，和可乐一起会有奇效！",
                ColorAffinity.None, Color.white, ItemShape.Circle,
                1f, 0f, ItemRarity.Common);

            CreateFoodItem(folderPath, "GummyFish", "软糖鱼",
                "Q弹的软糖鱼",
                ColorAffinity.None, new Color(1f, 0.6f, 0.8f), ItemShape.Diamond,
                3f, 0f, ItemRarity.Rare);

            // ========== 毒物 ==========
            CreatePoisonItem(folderPath, "PoisonApple", "毒苹果",
                "看起来很像红苹果...小心！",
                new Color(0.5f, 0.1f, 0.3f), 5f);

            // ========== 特殊道具 ==========
            CreateSpecialItem(folderPath, "Cola", "罐装可乐",
                "喝下去会像火箭一样冲刺！",
                ItemType.RocketBoost, new Color(0.6f, 0.2f, 0.2f), ItemShape.Square,
                1f, 3f, ItemRarity.Rare);

            // ========== 陷阱道具 ==========
            CreateTrapItem(folderPath, "Nail", "钉子",
                "放置后可以临时拦截车辆",
                ItemType.Trap_Nail, new Color(0.5f, 0.5f, 0.5f), ItemShape.Triangle,
                1f, 3f, ItemRarity.Common, false);

            CreateTrapItem(folderPath, "AdvancedNail", "高级钉子",
                "永久拦截车辆，需要铲子才能清除",
                ItemType.Trap_PermanentNail, new Color(0.3f, 0.3f, 0.3f), ItemShape.Triangle,
                1f, 0f, ItemRarity.Rare, true);

            CreateTrapItem(folderPath, "StickyPad", "粘鼠板",
                "让车辆减速",
                ItemType.Trap_StickyPad, new Color(0.8f, 0.7f, 0.5f), ItemShape.Square,
                0.5f, 5f, ItemRarity.Common, false);

            // ========== 障碍物 ==========
            CreateObstacleItem(folderPath, "Rock", "石头",
                "坚硬的石头，可以用铲子击碎",
                new Color(0.6f, 0.6f, 0.6f));

            // ========== 工具 ==========
            CreateToolItem(folderPath, "Shovel", "铲子",
                "可以击碎石头和彩蛋",
                ItemType.Tool_Shovel, new Color(0.7f, 0.5f, 0.3f), ItemShape.Hexagon,
                ItemRarity.Rare);

            CreateToolItem(folderPath, "SoulNet", "灵魂网",
                "可以将灵魂状态的熊复活",
                ItemType.Tool_SoulNet, new Color(0.8f, 0.6f, 1f), ItemShape.Star,
                ItemRarity.Epic);

            // ========== 特殊机关 ==========
            CreateSpecialItem(folderPath, "EasterEgg", "复活节彩蛋",
                "击碎后会获得随机奖励！",
                ItemType.Special_EasterEgg, new Color(1f, 0.8f, 0.9f), ItemShape.Diamond,
                1f, 0f, ItemRarity.Epic);

            CreateSpecialItem(folderPath, "PuddingPad", "布丁垫子",
                "弹跳平台，不可移动",
                ItemType.Special_PuddingPad, new Color(1f, 0.9f, 0.6f), ItemShape.Square,
                1f, 0f, ItemRarity.Rare);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"所有预设道具已创建在 {folderPath}");
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Items");
            }
        }

        private static void CreateFoodItem(string folder, string fileName, string itemName, string desc,
            ColorAffinity affinity, Color color, ItemShape shape, float baseEffect, float affinityBonus, ItemRarity rarity)
        {
            string path = $"{folder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null)
            {
                Debug.Log($"{fileName} 已存在，跳过");
                return;
            }

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = itemName;
            item.description = desc;
            item.itemType = ItemType.Food;
            item.targetType = TargetType.Bear;
            item.colorAffinity = affinity;
            item.baseEffect = baseEffect;
            item.affinityBonus = affinityBonus;
            item.itemColor = color;
            item.shape = shape;
            item.rarity = rarity;
            item.stackable = true;
            item.maxStack = 5;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"创建食物: {itemName}");
        }

        private static void CreatePoisonItem(string folder, string fileName, string itemName, string desc,
            Color color, float damage)
        {
            string path = $"{folder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return;

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = itemName;
            item.description = desc;
            item.itemType = ItemType.Poison;
            item.targetType = TargetType.Bear;
            item.colorAffinity = ColorAffinity.None;
            item.baseEffect = -damage;
            item.effectValue = damage;
            item.itemColor = color;
            item.shape = ItemShape.Circle;
            item.rarity = ItemRarity.Rare;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"创建毒物: {itemName}");
        }

        private static void CreateTrapItem(string folder, string fileName, string itemName, string desc,
            ItemType type, Color color, ItemShape shape, float effectValue, float duration, ItemRarity rarity, bool breakable)
        {
            string path = $"{folder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return;

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = itemName;
            item.description = desc;
            item.itemType = type;
            item.targetType = TargetType.Cart;
            item.effectValue = effectValue;
            item.effectDuration = duration;
            item.itemColor = color;
            item.shape = shape;
            item.rarity = rarity;
            item.isPlaceable = true;
            item.isBreakable = breakable;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"创建陷阱: {itemName}");
        }

        private static void CreateObstacleItem(string folder, string fileName, string itemName, string desc, Color color)
        {
            string path = $"{folder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return;

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = itemName;
            item.description = desc;
            item.itemType = ItemType.Obstacle_Rock;
            item.targetType = TargetType.Cart;
            item.itemColor = color;
            item.shape = ItemShape.Square;
            item.rarity = ItemRarity.Common;
            item.isPlaceable = true;
            item.isBreakable = true;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"创建障碍物: {itemName}");
        }

        private static void CreateToolItem(string folder, string fileName, string itemName, string desc,
            ItemType type, Color color, ItemShape shape, ItemRarity rarity)
        {
            string path = $"{folder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return;

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = itemName;
            item.description = desc;
            item.itemType = type;
            item.targetType = TargetType.All;
            item.itemColor = color;
            item.shape = shape;
            item.rarity = rarity;
            item.stackable = false;
            item.maxStack = 1;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"创建工具: {itemName}");
        }

        private static void CreateSpecialItem(string folder, string fileName, string itemName, string desc,
            ItemType type, Color color, ItemShape shape, float effectValue, float duration, ItemRarity rarity)
        {
            string path = $"{folder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return;

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = itemName;
            item.description = desc;
            item.itemType = type;
            item.targetType = TargetType.Bear;
            item.effectValue = effectValue;
            item.effectDuration = duration;
            item.itemColor = color;
            item.shape = shape;
            item.rarity = rarity;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"创建特殊道具: {itemName}");
        }

        [MenuItem("BearCar/道具系统/创建道具生成点")]
        public static void CreateItemSpawner()
        {
            GameObject spawner = new GameObject("ItemSpawner");
            spawner.AddComponent<ItemSpawner>();

            Undo.RegisterCreatedObjectUndo(spawner, "Create Item Spawner");
            Selection.activeGameObject = spawner;

            Debug.Log("道具生成点已创建!");
        }

        [MenuItem("BearCar/道具系统/在场景中放置道具")]
        public static void PlaceItemInScene()
        {
            GameObject pickup = new GameObject("ItemPickup");
            var pickupComp = pickup.AddComponent<ItemPickup>();

            var banana = Resources.Load<ItemData>("Items/Banana");
            if (banana != null)
            {
                pickupComp.itemData = banana;
            }

            if (Selection.activeGameObject != null)
            {
                pickup.transform.position = Selection.activeGameObject.transform.position + Vector3.up * 2;
            }
            else
            {
                pickup.transform.position = Vector3.zero;
            }

            Undo.RegisterCreatedObjectUndo(pickup, "Create Item Pickup");
            Selection.activeGameObject = pickup;

            Debug.Log("道具拾取物已创建!");
        }

        [MenuItem("BearCar/道具系统/打开设计文档")]
        public static void OpenDesignDocument()
        {
            string docPath = "Assets/Documentation/ItemDesignDocument.md";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(docPath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogWarning($"找不到设计文档: {docPath}");
            }
        }
    }
}
#endif
