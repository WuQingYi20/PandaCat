using UnityEngine;

namespace BearCar.Item
{
    /// <summary>
    /// 道具数据 - 可在 Inspector 中配置
    /// 创建方法: 右键 -> Create -> BearCar -> Item Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "BearCar/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("=== 基础信息 ===")]
        public string itemName = "道具";
        [TextArea(2, 4)]
        public string description = "道具描述";
        public ItemRarity rarity = ItemRarity.Common;

        [Header("=== 视觉 ===")]
        [Tooltip("道具图标（用于 UI）")]
        public Sprite icon;

        [Tooltip("道具颜色（如果没有图标）")]
        public Color itemColor = Color.white;

        [Tooltip("道具形状")]
        public ItemShape shape = ItemShape.Circle;

        [Header("=== 类型与目标 ===")]
        [Tooltip("道具类型")]
        public ItemType itemType = ItemType.Food;

        [Tooltip("作用对象")]
        public TargetType targetType = TargetType.Bear;

        [Header("=== 颜色亲和系统 ===")]
        [Tooltip("颜色亲和 - 影响不同颜色熊的效果")]
        public ColorAffinity colorAffinity = ColorAffinity.None;

        [Tooltip("基础效果值")]
        public float baseEffect = 5f;

        [Tooltip("异色熊额外加成（同色熊获得基础值，异色熊获得基础值+加成）")]
        public float affinityBonus = 5f;

        [Header("=== 效果参数 ===")]
        [Tooltip("效果数值（通用）")]
        public float effectValue = 1f;

        [Tooltip("效果持续时间（0=瞬间）")]
        public float effectDuration = 0f;

        [Header("=== 特殊属性 ===")]
        [Tooltip("是否可放置在场景中")]
        public bool isPlaceable = false;

        [Tooltip("是否可被击碎（需要铲子）")]
        public bool isBreakable = false;

        [Tooltip("击碎后掉落的道具")]
        public ItemData[] breakRewards;

        [Tooltip("是否为组合道具触发器")]
        public bool isComboTrigger = false;

        [Tooltip("组合所需的另一个道具")]
        public ItemData comboPartner;

        [Tooltip("组合后产生的效果类型")]
        public ItemType comboResultType = ItemType.RocketBoost;

        [Header("=== 堆叠 ===")]
        [Tooltip("是否可堆叠")]
        public bool stackable = true;

        [Tooltip("最大堆叠数量")]
        public int maxStack = 5;

        [Header("=== 音效 ===")]
        public AudioClip pickupSound;
        public AudioClip useSound;

        /// <summary>
        /// 计算对特定玩家的实际效果值
        /// </summary>
        public float GetEffectForPlayer(int playerIndex)
        {
            // playerIndex 0 = 绿熊, 1 = 红熊
            if (colorAffinity == ColorAffinity.None)
            {
                return baseEffect;
            }

            bool isGreenBear = (playerIndex == 0);
            bool isGreenItem = (colorAffinity == ColorAffinity.Green);

            // 同色熊获得基础值，异色熊获得基础值+加成
            if (isGreenBear == isGreenItem)
            {
                return baseEffect; // 同色
            }
            else
            {
                return baseEffect + affinityBonus; // 异色，获得更多！
            }
        }

        /// <summary>
        /// 获取效果描述（用于日志）
        /// </summary>
        public string GetEffectDescription(int playerIndex)
        {
            float effect = GetEffectForPlayer(playerIndex);

            switch (itemType)
            {
                case ItemType.Food:
                    if (effect >= 0)
                        return $"体力 +{effect}";
                    else
                        return $"体力 {effect}";

                case ItemType.Poison:
                    return $"体力 {-Mathf.Abs(effectValue)}";

                case ItemType.RocketBoost:
                    return $"火箭推进 {effectDuration}秒";

                case ItemType.Trap_Nail:
                    return $"拦截车辆 {effectDuration}秒";

                case ItemType.Trap_PermanentNail:
                    return "永久拦截车辆";

                case ItemType.Trap_StickyPad:
                    return $"车辆减速 {effectValue * 100}%";

                case ItemType.Tool_Shovel:
                    return "可击碎物体";

                case ItemType.Tool_SoulNet:
                    return "复活灵魂状态的熊";

                case ItemType.Obstacle_Rock:
                    return "拦截车辆，可被击碎";

                case ItemType.Special_EasterEgg:
                    return "击碎后随机奖励！";

                case ItemType.Special_PuddingPad:
                    return "弹跳平台";

                default:
                    return description;
            }
        }
    }

    /// <summary>
    /// 道具类型
    /// </summary>
    public enum ItemType
    {
        [Header("=== 食物 ===")]
        [Tooltip("食物 - 恢复体力")]
        Food,

        [Tooltip("毒物 - 扣除体力")]
        Poison,

        [Header("=== 特殊效果 ===")]
        [Tooltip("火箭推进")]
        RocketBoost,

        [Tooltip("加速道具")]
        SpeedBoost,

        [Tooltip("体力恢复（旧版兼容）")]
        StaminaRecover,

        [Tooltip("护盾")]
        Shield,

        [Tooltip("磁铁")]
        Magnet,

        [Tooltip("时间减缓")]
        TimeSlowdown,

        [Header("=== 陷阱/障碍 ===")]
        [Tooltip("钉子 - 临时拦截")]
        Trap_Nail,

        [Tooltip("高级钉子 - 永久拦截")]
        Trap_PermanentNail,

        [Tooltip("粘鼠板 - 减速")]
        Trap_StickyPad,

        [Tooltip("石头障碍")]
        Obstacle_Rock,

        [Header("=== 工具 ===")]
        [Tooltip("铲子 - 击碎物体")]
        Tool_Shovel,

        [Tooltip("灵魂网 - 复活")]
        Tool_SoulNet,

        [Header("=== 特殊 ===")]
        [Tooltip("复活节彩蛋 - 抽奖")]
        Special_EasterEgg,

        [Tooltip("布丁垫子 - 弹跳")]
        Special_PuddingPad,

        [Tooltip("组合触发 - 曼妥思等")]
        ComboTrigger
    }

    /// <summary>
    /// 作用对象
    /// </summary>
    public enum TargetType
    {
        [Tooltip("只作用于熊")]
        Bear,

        [Tooltip("只作用于车")]
        Cart,

        [Tooltip("作用于所有对象")]
        All,

        [Tooltip("只作用于灵魂")]
        Soul
    }

    /// <summary>
    /// 颜色亲和
    /// </summary>
    public enum ColorAffinity
    {
        [Tooltip("无亲和 - 对所有熊效果相同")]
        None,

        [Tooltip("绿色亲和 - 绿熊吃效果较低，红熊吃效果更高")]
        Green,

        [Tooltip("红色亲和 - 红熊吃效果较低，绿熊吃效果更高")]
        Red
    }

    /// <summary>
    /// 道具形状（用于生成图标）
    /// </summary>
    public enum ItemShape
    {
        Circle,      // 圆形 - 通用
        Square,      // 方形 - 障碍物
        Diamond,     // 菱形 - 特殊道具
        Star,        // 星形 - 稀有道具
        Heart,       // 心形 - 治疗类
        Triangle,    // 三角 - 陷阱类
        Hexagon      // 六边形 - 工具类
    }

    /// <summary>
    /// 道具稀有度
    /// </summary>
    public enum ItemRarity
    {
        [Tooltip("普通 - 随处可见")]
        Common,

        [Tooltip("稀有 - 需要探索")]
        Rare,

        [Tooltip("史诗 - 特殊位置")]
        Epic,

        [Tooltip("传说 - 极其罕见")]
        Legendary
    }
}
