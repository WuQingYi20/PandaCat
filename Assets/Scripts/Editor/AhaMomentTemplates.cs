#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Item;
using BearCar.Level;

namespace BearCar.Editor
{
    /// <summary>
    /// Aha Moment 类型分类
    /// </summary>
    public enum AhaCategory
    {
        Discovery,      // 发现型 - 揭示隐藏机制
        Synchronization,// 同步型 - 需要精确配合
        Sacrifice,      // 牺牲型 - 一人让利
        Rescue,         // 救援型 - 互相帮助
        Optimization    // 优化型 - 发现更好策略
    }

    /// <summary>
    /// Aha Moment 模板定义
    /// </summary>
    public class AhaTemplate
    {
        public string name;
        public string emoji;
        public string description;
        public string ahaHint;
        public string difficulty; // 简单/中等/困难
        public AhaCategory category;
        public string designRationale; // 设计原理说明

        // 创建模板内容的委托
        public System.Func<Vector3, GameObject> CreateInScene;
    }

    /// <summary>
    /// 12个内置 Aha Moment 模板 - 覆盖所有合作设计类型
    /// 设计原则: 正向依存、不对称收益、空间协调、沟通触发、角色流动
    /// </summary>
    public static class AhaTemplates
    {
        // ========== 模板索引 ==========
        public static AhaTemplate[] AllTemplates => new AhaTemplate[]
        {
            // 发现型 (Discovery)
            ColorSwap,          // 颜色互换
            MomentumRush,       // 动量冲刺
            HiddenCombo,        // 隐藏组合

            // 同步型 (Synchronization)
            CoopPortal,         // 合作传送
            TimeRace,           // 时间竞速
            DualSwitch,         // 双开关门

            // 牺牲型 (Sacrifice)
            Teamwork,           // 分工合作
            StaminaSacrifice,   // 体力让渡

            // 救援型 (Rescue)
            GiftSurprise,       // 礼物惊喜
            RescueMission,      // 救援任务

            // 优化型 (Optimization)
            ColorOptimization,  // 颜色优化
            PathChoice          // 路径抉择
        };

        // ========== 发现型模板 (Discovery) ==========

        /// <summary>
        /// 模板1: 颜色互换 - 让玩家发现吃对方颜色的道具更好
        /// </summary>
        public static AhaTemplate ColorSwap = new AhaTemplate
        {
            name = "颜色互换",
            emoji = "🔄",
            category = AhaCategory.Discovery,
            description = "红绿道具相邻放置，中间有保底道具。玩家发现吃对方颜色收益更高。",
            ahaHint = "原来拿对方颜色的更好！",
            difficulty = "简单",
            designRationale = "利用颜色亲和系统，让玩家自然发现「利他=利己」的核心机制。香蕉作为保底确保没人空手。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_ColorSwap");
                parent.transform.position = basePos;

                // 加载道具
                var greenApple = LoadItem("GreenApple");
                var redApple = LoadItem("RedApple");
                var banana = LoadItem("Banana");

                // 放置道具: 绿-黄-红 垂直排列
                if (greenApple != null)
                    CreateItemPickup(greenApple, parent.transform, new Vector3(0, 1.5f, 0));
                if (banana != null)
                    CreateItemPickup(banana, parent.transform, new Vector3(0, 0, 0));
                if (redApple != null)
                    CreateItemPickup(redApple, parent.transform, new Vector3(0, -1.5f, 0));

                // 添加说明标签
                var label = new GameObject("_DesignNote");
                label.transform.parent = parent.transform;
                label.transform.localPosition = new Vector3(2, 0, 0);

                return parent;
            }
        };

        // ========== 同步型模板 (Synchronization) ==========

        /// <summary>
        /// 模板4: 合作传送 - 两人同时站上压力板才能激活传送门
        /// </summary>
        public static AhaTemplate CoopPortal = new AhaTemplate
        {
            name = "合作传送",
            emoji = "🚀",
            category = AhaCategory.Synchronization,
            description = "两个压力板控制一个传送门，需要两人同时站上才能激活。",
            ahaHint = "我们要一起行动！",
            difficulty = "中等",
            designRationale = "强制同步行为是最直接的合作训练。压力板间距设计要让玩家能互相看到对方，激活延迟给沟通时间。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_CoopPortal");
                parent.transform.position = basePos;

                // 创建两个压力板
                var plateA = CreatePressurePlate("PlateA", parent.transform, new Vector3(-2, -1, 0));
                var plateB = CreatePressurePlate("PlateB", parent.transform, new Vector3(2, -1, 0));

                // 创建合作传送门
                var portal = CreatePortalObject("CoopPortal", parent.transform, new Vector3(0, 1, 0));
                var portalComp = portal.GetComponent<Portal>();
                portalComp.activation = PortalActivation.CoopTrigger;
                portalComp.requiredPlayers = 2;
                portalComp.activationDelay = 0.5f;
                portalComp.portalColor = new Color(0.8f, 0.3f, 1f);
                portalComp.activeColor = new Color(0.3f, 1f, 0.5f);

                // 创建出口
                var exit = CreatePortalObject("CoopPortal_Exit", parent.transform, new Vector3(0, 6, 0));
                var exitComp = exit.GetComponent<Portal>();
                exitComp.direction = PortalDirection.OneWayOut;
                exitComp.portalColor = new Color(0.5f, 0.8f, 1f);

                portalComp.targetPoint = exit.transform;

                // 链接压力板
                plateA.GetComponent<PressurePlate>().linkedObjects = new GameObject[] { portal };
                plateB.GetComponent<PressurePlate>().linkedObjects = new GameObject[] { portal };

                // 出口处放奖励
                var reward = LoadItem("Cherry");
                if (reward != null)
                    CreateItemPickup(reward, parent.transform, new Vector3(0, 7, 0));

                return parent;
            }
        };

        // ========== 救援型模板 (Rescue) ==========

        /// <summary>
        /// 模板9: 礼物惊喜 - 用铲子开礼物获得惊喜
        /// </summary>
        public static AhaTemplate GiftSurprise = new AhaTemplate
        {
            name = "礼物惊喜",
            emoji = "🎁",
            category = AhaCategory.Rescue,
            description = "圣诞礼物需要铲子才能打开，铲子在另一条路上。",
            ahaHint = "你帮我拿铲子，我们一起开礼物！",
            difficulty = "中等",
            designRationale = "帮助队友解决问题的典型场景。礼物的随机奖励增加惊喜感，铲子持有者有权力感，被帮助者有感恩机会。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_GiftSurprise");
                parent.transform.position = basePos;

                // 主路线: 礼物盒 (需要铲子)
                var giftBox = LoadItem("GiftBox");
                if (giftBox == null) giftBox = LoadItem("EasterEgg"); // 兼容旧名称

                if (giftBox != null)
                    CreateItemPickup(giftBox, parent.transform, new Vector3(0, 0, 0));

                // 分支路线: 铲子
                var shovel = LoadItem("Shovel");
                if (shovel != null)
                    CreateItemPickup(shovel, parent.transform, new Vector3(-3, -2, 0));

                // 提示用的石头障碍
                var rock = LoadItem("Rock");
                if (rock != null)
                    CreateItemPickup(rock, parent.transform, new Vector3(-1.5f, -1, 0));

                // 视觉引导: 画一条虚线
                var guide = new GameObject("_PathGuide");
                guide.transform.parent = parent.transform;
                guide.transform.localPosition = Vector3.zero;

                return parent;
            }
        };

        /// <summary>
        /// 模板2: 动量冲刺 - 利用传送门加速冲上高处
        /// </summary>
        public static AhaTemplate MomentumRush = new AhaTemplate
        {
            name = "动量冲刺",
            emoji = "💨",
            category = AhaCategory.Discovery,
            description = "传送门提供2倍加速，让玩家冲上原本到不了的高处。",
            ahaHint = "传送门还能加速！",
            difficulty = "中等",
            designRationale = "隐藏的物理机制发现。玩家第一次尝试可能失败，成功后会有强烈的成就感。高处奖励作为正反馈。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_MomentumRush");
                parent.transform.position = basePos;

                // 加速传送门入口
                var portalIn = CreatePortalObject("SpeedPortal_In", parent.transform, new Vector3(-3, 0, 0));
                var compIn = portalIn.GetComponent<Portal>();
                compIn.preserveMomentum = true;
                compIn.momentumMultiplier = 2f;
                compIn.momentumDirection = MomentumDirection.TowardsTarget;
                compIn.portalColor = new Color(1f, 0.8f, 0.2f);

                // 加速传送门出口 (朝右)
                var portalOut = CreatePortalObject("SpeedPortal_Out", parent.transform, new Vector3(0, 0, 0));
                var compOut = portalOut.GetComponent<Portal>();
                compOut.direction = PortalDirection.OneWayOut;
                compOut.portalColor = new Color(1f, 0.5f, 0.1f);
                portalOut.transform.rotation = Quaternion.Euler(0, 0, 0); // 朝右

                compIn.targetPoint = portalOut.transform;

                // 坡道指示 (用一个倾斜的方块表示)
                var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = "Ramp";
                ramp.transform.parent = parent.transform;
                ramp.transform.localPosition = new Vector3(3, 1, 0);
                ramp.transform.localScale = new Vector3(4, 0.5f, 1);
                ramp.transform.rotation = Quaternion.Euler(0, 0, 20);

                var rampRenderer = ramp.GetComponent<MeshRenderer>();
                if (rampRenderer != null)
                    rampRenderer.enabled = false; // 隐藏3D渲染，只作为位置参考

                Object.DestroyImmediate(ramp.GetComponent<Collider>());

                // 高处奖励
                var cola = LoadItem("Cola");
                if (cola != null)
                    CreateItemPickup(cola, parent.transform, new Vector3(6, 3, 0));

                return parent;
            }
        };

        // ========== 牺牲型模板 (Sacrifice) ==========

        /// <summary>
        /// 模板7: 分工合作 - 一人清障，一人拾取
        /// </summary>
        public static AhaTemplate Teamwork = new AhaTemplate
        {
            name = "分工合作",
            emoji = "🤝",
            category = AhaCategory.Sacrifice,
            description = "一条路有铲子，另一条路被石头堵住有宝藏。需要配合。",
            ahaHint = "你开路，我拿宝！",
            difficulty = "简单",
            designRationale = "经典的分工模式。拿铲子的人牺牲了拾取宝藏的机会，但团队总收益更高。奖励要足够丰厚来激励这种牺牲。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_Teamwork");
                parent.transform.position = basePos;

                // 路线A: 铲子
                var shovel = LoadItem("Shovel");
                if (shovel != null)
                    CreateItemPickup(shovel, parent.transform, new Vector3(-3, 0, 0));

                // 路线B: 石头障碍 + 宝藏
                var rock = LoadItem("Rock");
                if (rock != null)
                    CreateItemPickup(rock, parent.transform, new Vector3(2, 0, 0));

                // 宝藏 (石头后面)
                var gummyFish = LoadItem("GummyFish");
                if (gummyFish != null)
                    CreateItemPickup(gummyFish, parent.transform, new Vector3(4, 0, 0));

                var cherry = LoadItem("Cherry");
                if (cherry != null)
                    CreateItemPickup(cherry, parent.transform, new Vector3(4, 1, 0));

                // 分叉点指示
                var fork = new GameObject("_ForkPoint");
                fork.transform.parent = parent.transform;
                fork.transform.localPosition = new Vector3(0, -2, 0);

                return parent;
            }
        };

        /// <summary>
        /// 模板5: 时间竞速 - 限时传送门，需要快速行动
        /// </summary>
        public static AhaTemplate TimeRace = new AhaTemplate
        {
            name = "时间竞速",
            emoji = "⏱️",
            category = AhaCategory.Synchronization,
            description = "压力板激活的传送门只开5秒，需要两人快速配合。",
            ahaHint = "快！趁传送门还开着！",
            difficulty = "困难",
            designRationale = "时间压力创造紧张感。一人必须留守压力板牺牲自己的机动性，考验信任和快速决策。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_TimeRace");
                parent.transform.position = basePos;

                // 远处的压力板
                var plate = CreatePressurePlate("TimerPlate", parent.transform, new Vector3(-5, 0, 0));
                var plateComp = plate.GetComponent<PressurePlate>();

                // 限时传送门
                var portal = CreatePortalObject("TimedPortal", parent.transform, new Vector3(3, 0, 0));
                var portalComp = portal.GetComponent<Portal>();
                portalComp.activation = PortalActivation.ExternalTrigger;
                portalComp.activeDuration = 5f; // 5秒后关闭
                portalComp.portalColor = new Color(1f, 0.3f, 0.3f);
                portalComp.activeColor = new Color(0.3f, 1f, 0.3f);

                // 出口
                var exit = CreatePortalObject("TimedPortal_Exit", parent.transform, new Vector3(3, 6, 0));
                exit.GetComponent<Portal>().direction = PortalDirection.OneWayOut;
                portalComp.targetPoint = exit.transform;

                // 链接压力板
                plateComp.linkedObjects = new GameObject[] { portal };

                // 丰厚奖励
                var kiwi = LoadItem("Kiwi");
                if (kiwi != null)
                    CreateItemPickup(kiwi, parent.transform, new Vector3(3, 7, 0));

                var strawberry = LoadItem("Strawberry");
                if (strawberry != null)
                    CreateItemPickup(strawberry, parent.transform, new Vector3(4, 7, 0));

                return parent;
            }
        };

        // ========== 新增模板 ==========

        /// <summary>
        /// 模板3: 隐藏组合 - 曼妥思+可乐的秘密
        /// </summary>
        public static AhaTemplate HiddenCombo = new AhaTemplate
        {
            name = "隐藏组合",
            emoji = "🧪",
            category = AhaCategory.Discovery,
            description = "曼妥思和可乐分开只是普通道具，同时拥有时按Q键触发超级火箭推进！",
            ahaHint = "曼妥思+可乐=超级火箭！按Q触发！",
            difficulty = "困难",
            designRationale = "隐藏的道具组合机制，鼓励玩家尝试和交流。当两个道具都在背包时，屏幕顶部会显示明显的组合提示。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_HiddenCombo");
                parent.transform.position = basePos;

                var mentos = LoadItem("Mentos");
                var cola = LoadItem("Cola");

                if (mentos != null)
                    CreateItemPickup(mentos, parent.transform, new Vector3(-1.5f, 0, 0));
                if (cola != null)
                    CreateItemPickup(cola, parent.transform, new Vector3(1.5f, 0, 0));

                // 提示符号
                var hint = new GameObject("_ComboHint");
                hint.transform.parent = parent.transform;
                hint.transform.localPosition = new Vector3(0, 1, 0);

                return parent;
            }
        };

        /// <summary>
        /// 模板6: 双开关门 - 两个开关控制同一道门
        /// </summary>
        public static AhaTemplate DualSwitch = new AhaTemplate
        {
            name = "双开关门",
            emoji = "🚪",
            category = AhaCategory.Synchronization,
            description = "两个压力板必须同时踩下才能开启通道，任一松开门就关闭。",
            ahaHint = "我们必须同时保持！",
            difficulty = "中等",
            designRationale = "持续性的同步要求。与合作传送门不同，这个需要持续配合而非瞬时触发，考验耐心和沟通。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_DualSwitch");
                parent.transform.position = basePos;

                // 两个压力板，间距较远
                var plateA = CreatePressurePlate("SwitchA", parent.transform, new Vector3(-4, 0, 0));
                var plateB = CreatePressurePlate("SwitchB", parent.transform, new Vector3(4, 0, 0));

                // 中间的传送门（需要两个都踩才开）
                var portal = CreatePortalObject("GatedPortal", parent.transform, new Vector3(0, 0, 0));
                var portalComp = portal.GetComponent<Portal>();
                portalComp.activation = PortalActivation.CoopTrigger;
                portalComp.requiredPlayers = 2;
                portalComp.portalColor = new Color(0.8f, 0.4f, 0.4f);

                var exit = CreatePortalObject("GatedPortal_Exit", parent.transform, new Vector3(0, 5, 0));
                exit.GetComponent<Portal>().direction = PortalDirection.OneWayOut;
                portalComp.targetPoint = exit.transform;

                // 链接两个压力板
                plateA.GetComponent<PressurePlate>().linkedObjects = new GameObject[] { portal };
                plateB.GetComponent<PressurePlate>().linkedObjects = new GameObject[] { portal };

                // 丰厚奖励
                var kiwi = LoadItem("Kiwi");
                var cherry = LoadItem("Cherry");
                if (kiwi != null)
                    CreateItemPickup(kiwi, parent.transform, new Vector3(-1, 6, 0));
                if (cherry != null)
                    CreateItemPickup(cherry, parent.transform, new Vector3(1, 6, 0));

                return parent;
            }
        };

        /// <summary>
        /// 模板8: 体力让渡 - 体力低的人应该优先吃东西
        /// </summary>
        public static AhaTemplate StaminaSacrifice = new AhaTemplate
        {
            name = "体力让渡",
            emoji = "💚",
            category = AhaCategory.Sacrifice,
            description = "只有一个食物，体力低的队友更需要它。谁来牺牲？",
            ahaHint = "你先吃，我还撑得住！",
            difficulty = "简单",
            designRationale = "资源稀缺时的利他决策。单个高价值食物强迫玩家讨论谁更需要，培养团队意识。颜色亲和增加了决策复杂度。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_StaminaSacrifice");
                parent.transform.position = basePos;

                // 单个高价值道具（红色，对绿熊更好）
                var strawberry = LoadItem("Strawberry");
                if (strawberry != null)
                    CreateItemPickup(strawberry, parent.transform, new Vector3(0, 0, 0));

                // 两侧放置陷阱增加消耗
                var nail1 = LoadItem("Nail");
                var nail2 = LoadItem("Nail");
                if (nail1 != null)
                    CreateItemPickup(nail1, parent.transform, new Vector3(-3, 0, 0));
                if (nail2 != null)
                    CreateItemPickup(nail2, parent.transform, new Vector3(3, 0, 0));

                return parent;
            }
        };

        /// <summary>
        /// 模板10: 救援任务 - 一人被困，另一人来救
        /// </summary>
        public static AhaTemplate RescueMission = new AhaTemplate
        {
            name = "救援任务",
            emoji = "🆘",
            category = AhaCategory.Rescue,
            description = "一条路通向宝藏但有陷阱，另一人需要用铲子清除陷阱来救援。",
            ahaHint = "我被困住了，快来救我！",
            difficulty = "中等",
            designRationale = "创造紧急情况和英雄时刻。被困者有紧迫感，救援者有使命感。成功救援强化信任。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_RescueMission");
                parent.transform.position = basePos;

                // 陷阱区域
                var nail1 = LoadItem("AdvancedNail");
                var nail2 = LoadItem("AdvancedNail");
                if (nail1 != null)
                    CreateItemPickup(nail1, parent.transform, new Vector3(2, 0, 0));
                if (nail2 != null)
                    CreateItemPickup(nail2, parent.transform, new Vector3(4, 0, 0));

                // 被困区域的宝藏
                var gummyFish = LoadItem("GummyFish");
                if (gummyFish != null)
                    CreateItemPickup(gummyFish, parent.transform, new Vector3(6, 0, 0));

                // 救援路线的铲子
                var shovel = LoadItem("Shovel");
                if (shovel != null)
                    CreateItemPickup(shovel, parent.transform, new Vector3(-3, -2, 0));

                // 起点提示
                var start = new GameObject("_RescueStart");
                start.transform.parent = parent.transform;
                start.transform.localPosition = new Vector3(-3, 0, 0);

                return parent;
            }
        };

        // ========== 优化型模板 (Optimization) ==========

        /// <summary>
        /// 模板11: 颜色优化 - 发现最优的道具分配策略
        /// </summary>
        public static AhaTemplate ColorOptimization = new AhaTemplate
        {
            name = "颜色优化",
            emoji = "🎯",
            category = AhaCategory.Optimization,
            description = "多个颜色道具散落，玩家需要发现最优的拾取分配方案。",
            ahaHint = "你拿绿的，我拿红的，然后交换！",
            difficulty = "中等",
            designRationale = "颜色亲和系统的深度应用。最优解需要沟通和规划，而非本能反应。优化成功的收益要明显高于随便拿。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_ColorOptimization");
                parent.transform.position = basePos;

                // 散落的颜色道具
                var greenApple = LoadItem("GreenApple");
                var kiwi = LoadItem("Kiwi");
                var redApple = LoadItem("RedApple");
                var cherry = LoadItem("Cherry");

                // 绿色道具在红熊容易到达的位置
                if (greenApple != null)
                    CreateItemPickup(greenApple, parent.transform, new Vector3(3, 1, 0));
                if (kiwi != null)
                    CreateItemPickup(kiwi, parent.transform, new Vector3(4, -1, 0));

                // 红色道具在绿熊容易到达的位置
                if (redApple != null)
                    CreateItemPickup(redApple, parent.transform, new Vector3(-3, 1, 0));
                if (cherry != null)
                    CreateItemPickup(cherry, parent.transform, new Vector3(-4, -1, 0));

                // 中性保底
                var banana = LoadItem("Banana");
                if (banana != null)
                    CreateItemPickup(banana, parent.transform, new Vector3(0, 0, 0));

                return parent;
            }
        };

        /// <summary>
        /// 模板12: 路径抉择 - 两条路，不同风险和回报
        /// </summary>
        public static AhaTemplate PathChoice = new AhaTemplate
        {
            name = "路径抉择",
            emoji = "🔀",
            category = AhaCategory.Optimization,
            description = "两条路线：安全路线有少量奖励，危险路线有大量奖励但需要配合。",
            ahaHint = "我们一起走危险路，收获更大！",
            difficulty = "困难",
            designRationale = "风险-回报权衡的经典设计。安全路是保底，危险路需要信任和配合。选择危险路并成功是高峰体验。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("AhaTemplate_PathChoice");
                parent.transform.position = basePos;

                // 安全路线（上方）：少量奖励
                var banana = LoadItem("Banana");
                if (banana != null)
                    CreateItemPickup(banana, parent.transform, new Vector3(3, 2, 0));

                // 危险路线（下方）：陷阱 + 大量奖励
                var nail = LoadItem("Nail");
                var stickyPad = LoadItem("StickyPad");
                if (nail != null)
                    CreateItemPickup(nail, parent.transform, new Vector3(1, -2, 0));
                if (stickyPad != null)
                    CreateItemPickup(stickyPad, parent.transform, new Vector3(3, -2, 0));

                // 危险路线的丰厚奖励
                var gummyFish = LoadItem("GummyFish");
                var cola = LoadItem("Cola");
                var cherry = LoadItem("Cherry");
                if (gummyFish != null)
                    CreateItemPickup(gummyFish, parent.transform, new Vector3(5, -2, 0));
                if (cola != null)
                    CreateItemPickup(cola, parent.transform, new Vector3(6, -2, 0));
                if (cherry != null)
                    CreateItemPickup(cherry, parent.transform, new Vector3(7, -2, 0));

                // 铲子在分叉点，用来清除陷阱
                var shovel = LoadItem("Shovel");
                if (shovel != null)
                    CreateItemPickup(shovel, parent.transform, new Vector3(-2, 0, 0));

                // 分叉点标记
                var fork = new GameObject("_ForkPoint");
                fork.transform.parent = parent.transform;
                fork.transform.localPosition = Vector3.zero;

                return parent;
            }
        };

        #region 辅助方法

        private static ItemData LoadItem(string itemName)
        {
            string path = $"Assets/Resources/Items/{itemName}.asset";
            return AssetDatabase.LoadAssetAtPath<ItemData>(path);
        }

        private static GameObject CreateItemPickup(ItemData item, Transform parent, Vector3 localPos)
        {
            var go = new GameObject($"Pickup_{item.itemName}");
            go.transform.parent = parent;
            go.transform.localPosition = localPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateItemSprite(item.shape);
            sr.color = item.itemColor;
            sr.sortingOrder = 10;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            var pickup = go.AddComponent<ItemPickup>();
            pickup.itemData = item;

            go.transform.localScale = Vector3.one * 0.8f;

            return go;
        }

        private static GameObject CreatePortalObject(string id, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(id);
            go.transform.parent = parent;
            go.transform.localPosition = localPos;

            var portal = go.AddComponent<Portal>();
            portal.portalId = id;

            return go;
        }

        private static GameObject CreatePressurePlate(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.parent = parent;
            go.transform.localPosition = localPos;

            var plate = go.AddComponent<PressurePlate>();

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlateSprite();
            sr.color = new Color(0.6f, 0.6f, 0.6f);
            sr.sortingOrder = 5;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 0.3f);
            col.isTrigger = true;

            return go;
        }

        private static Sprite CreateItemSprite(ItemShape shape)
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

                    bool filled = shape switch
                    {
                        ItemShape.Circle => dist < radius,
                        ItemShape.Square => Mathf.Abs(dx) < radius * 0.7f && Mathf.Abs(dy) < radius * 0.7f,
                        ItemShape.Diamond => Mathf.Abs(dx) + Mathf.Abs(dy) < radius,
                        ItemShape.Triangle => dy > -radius * 0.5f && Mathf.Abs(dx) < (radius - dy) * 0.6f,
                        ItemShape.Star => dist < radius * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(angle * 2.5f))),
                        ItemShape.Heart => Mathf.Pow((dx / radius) * (dx / radius) + (-dy / radius) * (-dy / radius) - 1, 3) - (dx / radius) * (dx / radius) * Mathf.Pow(-dy / radius, 3) < 0,
                        _ => dist < radius
                    };

                    pixels[y * size + x] = filled ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreatePlateSprite()
        {
            int width = 48, height = 12;
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        #endregion
    }

    /// <summary>
    /// 自定义模板资源 - 设计师可以保存自己的模板
    /// </summary>
    [CreateAssetMenu(fileName = "NewAhaTemplate", menuName = "BearCar/Aha Template")]
    public class AhaTemplateAsset : ScriptableObject
    {
        public string templateName = "自定义模板";
        public string description = "描述你的模板...";
        public string ahaHint = "Aha!";
        public string difficulty = "中等";
        public GameObject prefab;
    }

    /// <summary>
    /// 保存模板窗口
    /// </summary>
    public class SaveTemplateWindow : EditorWindow
    {
        private string templateName = "我的模板";
        private string description = "";
        private string ahaHint = "";
        private string difficulty = "中等";
        private GameObject[] selectedObjects;

        public static void ShowWindow(GameObject[] objects)
        {
            var window = GetWindow<SaveTemplateWindow>("保存模板");
            window.minSize = new Vector2(300, 250);
            window.selectedObjects = objects;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("💾 保存为 Aha 模板", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            templateName = EditorGUILayout.TextField("模板名称", templateName);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("描述");
            description = EditorGUILayout.TextArea(description, GUILayout.Height(50));

            EditorGUILayout.Space(5);
            ahaHint = EditorGUILayout.TextField("Aha 提示", ahaHint);

            difficulty = EditorGUILayout.TextField("难度", difficulty);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField($"将保存 {selectedObjects?.Length ?? 0} 个对象", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("取消"))
            {
                Close();
            }

            if (GUILayout.Button("保存"))
            {
                SaveTemplate();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SaveTemplate()
        {
            if (string.IsNullOrEmpty(templateName))
            {
                EditorUtility.DisplayDialog("错误", "请输入模板名称", "确定");
                return;
            }

            // 确保目录存在
            string folderPath = "Assets/Resources/AhaTemplates";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "AhaTemplates");
            }

            // 创建父对象
            var parent = new GameObject($"Template_{templateName}");
            foreach (var obj in selectedObjects)
            {
                var copy = Object.Instantiate(obj);
                copy.name = obj.name;
                copy.transform.parent = parent.transform;
                copy.transform.position = obj.transform.position - selectedObjects[0].transform.position;
            }

            // 保存为 Prefab
            string prefabPath = $"{folderPath}/{templateName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(parent, prefabPath);
            Object.DestroyImmediate(parent);

            // 创建模板资源
            var asset = ScriptableObject.CreateInstance<AhaTemplateAsset>();
            asset.templateName = templateName;
            asset.description = description;
            asset.ahaHint = ahaHint;
            asset.difficulty = difficulty;
            asset.prefab = prefab;

            string assetPath = $"{folderPath}/{templateName}_Template.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AhaTemplate] 模板已保存: {templateName}");
            EditorUtility.DisplayDialog("成功", $"模板 \"{templateName}\" 已保存!", "确定");

            Close();
        }
    }
}
#endif
