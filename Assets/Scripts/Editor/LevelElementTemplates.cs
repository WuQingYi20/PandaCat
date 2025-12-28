#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Item;
using BearCar.Level;

namespace BearCar.Editor
{
    /// <summary>
    /// 关卡元素分类
    /// </summary>
    public enum LevelElementCategory
    {
        Platform,       // 平台类
        Terrain,        // 地形类
        Mechanism,      // 机关类
        Portal,         // 传送门类
        CoopPuzzle,     // 双人谜题
        ClassicDesign   // 经典设计
    }

    /// <summary>
    /// 关卡元素模板
    /// </summary>
    public class LevelElementTemplate
    {
        public string name;
        public string emoji;
        public string description;
        public LevelElementCategory category;
        public string coopTip;          // 双人合作提示
        public string designNotes;      // 设计师备注
        public System.Func<Vector3, GameObject> CreateInScene;
    }

    /// <summary>
    /// 扩展关卡元素库 - 包含平台、斜坡、双人机制等
    /// </summary>
    public static class LevelElementTemplates
    {
        // ==================== 平台类 (Platform) ====================

        /// <summary>
        /// 移动平台 - 水平往返
        /// </summary>
        public static LevelElementTemplate MovingPlatformH = new LevelElementTemplate
        {
            name = "水平移动平台",
            emoji = "➡️",
            category = LevelElementCategory.Platform,
            description = "水平往返的移动平台，可承载玩家和车辆",
            coopTip = "两人可以同时站上，但要注意平台边缘",
            designNotes = "移动距离和速度影响难度。配合深坑使用效果更佳。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("MovingPlatform_H");
                parent.transform.position = basePos;
                CreateMovingPlatform("Platform", parent.transform,
                    new Vector3(-3, 0, 0), new Vector3(3, 0, 0), 2f);
                return parent;
            }
        };

        /// <summary>
        /// 移动平台 - 垂直往返
        /// </summary>
        public static LevelElementTemplate MovingPlatformV = new LevelElementTemplate
        {
            name = "垂直移动平台",
            emoji = "⬆️",
            category = LevelElementCategory.Platform,
            description = "垂直往返的移动平台，适合连接高低平台",
            coopTip = "一人先上去探路，确认安全后再让队友跟上",
            designNotes = "垂直移动更有紧张感。可以设计成电梯形式。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("MovingPlatform_V");
                parent.transform.position = basePos;
                CreateMovingPlatform("Platform", parent.transform,
                    new Vector3(0, 0, 0), new Vector3(0, 5, 0), 3f);
                return parent;
            }
        };

        /// <summary>
        /// 消失平台 - 踩上后消失
        /// </summary>
        public static LevelElementTemplate DisappearingPlatform = new LevelElementTemplate
        {
            name = "消失平台",
            emoji = "💨",
            category = LevelElementCategory.Platform,
            description = "踩上后1秒开始消失，3秒后完全消失并重生",
            coopTip = "第一人踩过后会消失，第二人需要等待重生或找其他路",
            designNotes = "消失延迟是核心参数。串联多个消失平台创造紧张感。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("DisappearingPlatforms");
                parent.transform.position = basePos;

                // 创建一排消失平台
                for (int i = 0; i < 4; i++)
                {
                    CreateDisappearingPlatformObject($"FadePlatform_{i}", parent.transform,
                        new Vector3(i * 2, 0, 0), 1f, 3f, 5f);
                }

                return parent;
            }
        };

        /// <summary>
        /// 单向平台 - 只能从下方穿过
        /// </summary>
        public static LevelElementTemplate OneWayPlatform = new LevelElementTemplate
        {
            name = "单向平台",
            emoji = "⬆️",
            category = LevelElementCategory.Platform,
            description = "可以从下方跳上去，但无法从上方穿过",
            coopTip = "适合分层设计，上层玩家可以掉下来帮助下层队友",
            designNotes = "经典平台跳跃元素。用于创建垂直探索空间。",
            CreateInScene = (basePos) =>
            {
                var platform = CreateOneWayPlatformObject("OneWayPlatform", basePos, 4f);
                return platform;
            }
        };

        /// <summary>
        /// 弹跳平台 - 跳上去会被弹起
        /// </summary>
        public static LevelElementTemplate BouncePlatform = new LevelElementTemplate
        {
            name = "弹跳平台",
            emoji = "🦘",
            category = LevelElementCategory.Platform,
            description = "踩上去会被弹起，弹跳高度固定",
            coopTip = "可以用来到达高处，两人可以同时弹跳",
            designNotes = "弹跳高度要精确计算，确保能到达目标平台。",
            CreateInScene = (basePos) =>
            {
                var platform = CreateBouncePlatformObject("BouncePlatform", basePos, 8f);
                return platform;
            }
        };

        // ==================== 地形类 (Terrain) ====================

        /// <summary>
        /// 上坡 - 减速但不消耗额外体力
        /// </summary>
        public static LevelElementTemplate SlopeUp = new LevelElementTemplate
        {
            name = "上坡",
            emoji = "📈",
            category = LevelElementCategory.Terrain,
            description = "30度上坡，推车时会减速",
            coopTip = "两人一起推车上坡效率更高",
            designNotes = "坡度影响速度衰减。可以在坡顶放置奖励。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("SlopeUp");
                parent.transform.position = basePos;
                CreateSlopeObject("Slope", parent.transform, 30f, 6f, true);
                return parent;
            }
        };

        /// <summary>
        /// 下坡 - 加速滑行
        /// </summary>
        public static LevelElementTemplate SlopeDown = new LevelElementTemplate
        {
            name = "下坡",
            emoji = "📉",
            category = LevelElementCategory.Terrain,
            description = "30度下坡，推车时会加速",
            coopTip = "下坡冲刺可以利用惯性跳过障碍",
            designNotes = "下坡后可接跳跃点，利用动量到达远处。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("SlopeDown");
                parent.transform.position = basePos;
                CreateSlopeObject("Slope", parent.transform, -30f, 6f, true);
                return parent;
            }
        };

        /// <summary>
        /// 深坑 - 需要跳跃或平台才能通过
        /// </summary>
        public static LevelElementTemplate Pit = new LevelElementTemplate
        {
            name = "深坑",
            emoji = "🕳️",
            category = LevelElementCategory.Terrain,
            description = "深坑障碍，掉下去需要绕路回来",
            coopTip = "一人可以当人肉桥让另一人通过（如果有推车）",
            designNotes = "深坑宽度决定难度。配合移动平台或传送门使用。",
            CreateInScene = (basePos) =>
            {
                var pit = CreatePitObject("Pit", basePos, 4f, 3f);
                return pit;
            }
        };

        /// <summary>
        /// 冰面 - 滑动，难以控制
        /// </summary>
        public static LevelElementTemplate IceSurface = new LevelElementTemplate
        {
            name = "冰面",
            emoji = "🧊",
            category = LevelElementCategory.Terrain,
            description = "低摩擦冰面，移动时会滑行",
            coopTip = "两人一起可以互相拉住对方",
            designNotes = "冰面长度和后方障碍决定难度。",
            CreateInScene = (basePos) =>
            {
                var ice = CreateIceSurfaceObject("IceSurface", basePos, 8f);
                return ice;
            }
        };

        // ==================== 机关类 (Mechanism) ====================

        /// <summary>
        /// 压力板门 - 踩住才开门
        /// </summary>
        public static LevelElementTemplate PressureDoor = new LevelElementTemplate
        {
            name = "压力板门",
            emoji = "🚪",
            category = LevelElementCategory.Mechanism,
            description = "压力板控制的门，松开压力板门会关闭",
            coopTip = "一人踩住压力板，另一人通过门",
            designNotes = "经典双人合作设计。压力板和门的距离影响策略。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("PressureDoor_Setup");
                parent.transform.position = basePos;

                // 压力板
                var plate = CreatePressurePlate("DoorPlate", parent.transform, new Vector3(-3, 0, 0));

                // 门（用一个方块表示）
                var door = CreateDoorObject("Door", parent.transform, new Vector3(3, 0, 0));

                // 链接
                plate.GetComponent<PressurePlate>().linkedObjects = new GameObject[] { door };

                return parent;
            }
        };

        /// <summary>
        /// 跷跷板 - 平衡机制
        /// </summary>
        public static LevelElementTemplate Seesaw = new LevelElementTemplate
        {
            name = "跷跷板",
            emoji = "⚖️",
            category = LevelElementCategory.Mechanism,
            description = "一端下沉时另一端上升，可以用来弹射",
            coopTip = "一人跳到一端，弹射另一人到高处",
            designNotes = "物理玩法的核心。需要精确的质量和力量计算。",
            CreateInScene = (basePos) =>
            {
                var seesaw = CreateSeesawObject("Seesaw", basePos, 6f);
                return seesaw;
            }
        };

        /// <summary>
        /// 传送带 - 自动移动
        /// </summary>
        public static LevelElementTemplate ConveyorBelt = new LevelElementTemplate
        {
            name = "传送带",
            emoji = "➡️",
            category = LevelElementCategory.Mechanism,
            description = "自动向一个方向移动玩家和物体",
            coopTip = "可以用来快速移动，但逆向行走会很慢",
            designNotes = "传送带速度和方向可调。可以设计逆向挑战。",
            CreateInScene = (basePos) =>
            {
                var belt = CreateConveyorBeltObject("ConveyorBelt", basePos, 6f, 3f, true);
                return belt;
            }
        };

        // ==================== 传送门组合 (Portal) ====================

        /// <summary>
        /// 传送门链 - 连续传送
        /// </summary>
        public static LevelElementTemplate PortalChain = new LevelElementTemplate
        {
            name = "传送门链",
            emoji = "🔗",
            category = LevelElementCategory.Portal,
            description = "连续3个传送门，形成快速通道",
            coopTip = "两人可以同时进入，保持队形",
            designNotes = "用于快速穿越大段距离。可以在中间加入分支。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("PortalChain");
                parent.transform.position = basePos;

                // 创建传送门链 A -> B -> C
                var portalA = CreatePortalObject("Chain_A", parent.transform, new Vector3(0, 0, 0));
                var portalB = CreatePortalObject("Chain_B", parent.transform, new Vector3(5, 2, 0));
                var portalC = CreatePortalObject("Chain_C", parent.transform, new Vector3(10, 0, 0));

                portalA.GetComponent<Portal>().targetPoint = portalB.transform;
                portalB.GetComponent<Portal>().targetPoint = portalC.transform;

                // 设置颜色渐变
                portalA.GetComponent<Portal>().portalColor = new Color(0.3f, 0.5f, 1f);
                portalB.GetComponent<Portal>().portalColor = new Color(0.5f, 0.3f, 1f);
                portalC.GetComponent<Portal>().portalColor = new Color(0.8f, 0.3f, 1f);

                return parent;
            }
        };

        /// <summary>
        /// 动量跳台 - 传送门加速跳跃
        /// </summary>
        public static LevelElementTemplate MomentumLaunch = new LevelElementTemplate
        {
            name = "动量跳台",
            emoji = "🚀",
            category = LevelElementCategory.Portal,
            description = "斜向下的入口 + 斜向上的出口 = 超级跳跃",
            coopTip = "先让一人测试，确认落点安全",
            designNotes = "角度和动量倍率需要精确计算。落点要有明显标记。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("MomentumLaunch");
                parent.transform.position = basePos;

                // 入口（向下倾斜）
                var portalIn = CreatePortalObject("Launch_In", parent.transform, new Vector3(0, 2, 0));
                var compIn = portalIn.GetComponent<Portal>();
                compIn.preserveMomentum = true;
                compIn.momentumMultiplier = 2.5f;
                compIn.portalColor = new Color(1f, 0.5f, 0.2f);
                portalIn.transform.rotation = Quaternion.Euler(0, 0, -45);

                // 出口（向上倾斜）
                var portalOut = CreatePortalObject("Launch_Out", parent.transform, new Vector3(3, 0, 0));
                var compOut = portalOut.GetComponent<Portal>();
                compOut.direction = PortalDirection.OneWayOut;
                compOut.portalColor = new Color(0.2f, 1f, 0.5f);
                portalOut.transform.rotation = Quaternion.Euler(0, 0, 60);

                compIn.targetPoint = portalOut.transform;

                // 目标平台标记
                var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "TargetPlatform";
                target.transform.parent = parent.transform;
                target.transform.localPosition = new Vector3(8, 6, 0);
                target.transform.localScale = new Vector3(3, 0.5f, 1);
                Object.DestroyImmediate(target.GetComponent<Collider>());

                return parent;
            }
        };

        // ==================== 双人谜题 (CoopPuzzle) ====================

        /// <summary>
        /// 双人压力板桥
        /// </summary>
        public static LevelElementTemplate CoopBridge = new LevelElementTemplate
        {
            name = "双人压力板桥",
            emoji = "🌉",
            category = LevelElementCategory.CoopPuzzle,
            description = "两个压力板控制一座桥，需要两人同时踩才能让第三方（车）通过",
            coopTip = "两人站位，让车自己滚过桥",
            designNotes = "这个设计需要车辆有自主移动能力，或者用重力让车滚过。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("CoopBridge_Setup");
                parent.transform.position = basePos;

                // 两个压力板在深坑两侧
                var plateL = CreatePressurePlate("BridgePlate_L", parent.transform, new Vector3(-4, 0, 0));
                var plateR = CreatePressurePlate("BridgePlate_R", parent.transform, new Vector3(4, 0, 0));

                // 桥（中间，初始隐藏）
                var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = "Bridge";
                bridge.transform.parent = parent.transform;
                bridge.transform.localPosition = new Vector3(0, -1, 0);
                bridge.transform.localScale = new Vector3(6, 0.5f, 1);
                Object.DestroyImmediate(bridge.GetComponent<Collider>());

                // 链接
                plateL.GetComponent<PressurePlate>().linkedObjects = new GameObject[] { bridge };
                plateR.GetComponent<PressurePlate>().linkedObjects = new GameObject[] { bridge };

                return parent;
            }
        };

        /// <summary>
        /// 高低接力
        /// </summary>
        public static LevelElementTemplate HighLowRelay = new LevelElementTemplate
        {
            name = "高低接力",
            emoji = "🔄",
            category = LevelElementCategory.CoopPuzzle,
            description = "一人在高处，一人在低处，需要配合才能都到达目标",
            coopTip = "高处的人先拿钥匙，丢给低处的人开门",
            designNotes = "垂直空间的合作设计。需要沟通和物品传递。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("HighLowRelay");
                parent.transform.position = basePos;

                // 高层平台
                var highPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                highPlatform.name = "HighPlatform";
                highPlatform.transform.parent = parent.transform;
                highPlatform.transform.localPosition = new Vector3(0, 4, 0);
                highPlatform.transform.localScale = new Vector3(4, 0.5f, 1);
                Object.DestroyImmediate(highPlatform.GetComponent<Collider>());

                // 低层起点
                var lowStart = new GameObject("LowStart");
                lowStart.transform.parent = parent.transform;
                lowStart.transform.localPosition = new Vector3(-3, 0, 0);

                // 高处的道具（需要丢下来）
                var shovel = LoadItem("Shovel");
                if (shovel != null)
                    CreateItemPickup(shovel, parent.transform, new Vector3(1, 5, 0));

                // 低处的石头障碍
                var rock = LoadItem("Rock");
                if (rock != null)
                    CreateItemPickup(rock, parent.transform, new Vector3(5, 0, 0));

                // 目标奖励（石头后面）
                var cherry = LoadItem("Cherry");
                if (cherry != null)
                    CreateItemPickup(cherry, parent.transform, new Vector3(7, 0, 0));

                return parent;
            }
        };

        /// <summary>
        /// 分头行动
        /// </summary>
        public static LevelElementTemplate SplitPath = new LevelElementTemplate
        {
            name = "分头行动",
            emoji = "🔀",
            category = LevelElementCategory.CoopPuzzle,
            description = "两条独立路线，各有机关，最后汇合才能开启宝箱",
            coopTip = "各走各的路，在终点会合",
            designNotes = "平行设计，两条路难度应该相当。汇合点是高潮。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("SplitPath");
                parent.transform.position = basePos;

                // 分叉点标记
                var fork = new GameObject("ForkPoint");
                fork.transform.parent = parent.transform;
                fork.transform.localPosition = Vector3.zero;

                // 上路（绿熊）
                var upperPath = new GameObject("UpperPath");
                upperPath.transform.parent = parent.transform;
                upperPath.transform.localPosition = new Vector3(3, 2, 0);

                var greenApple = LoadItem("GreenApple");
                if (greenApple != null)
                    CreateItemPickup(greenApple, parent.transform, new Vector3(5, 2, 0));

                // 下路（红熊）
                var lowerPath = new GameObject("LowerPath");
                lowerPath.transform.parent = parent.transform;
                lowerPath.transform.localPosition = new Vector3(3, -2, 0);

                var redApple = LoadItem("RedApple");
                if (redApple != null)
                    CreateItemPickup(redApple, parent.transform, new Vector3(5, -2, 0));

                // 汇合点的双人压力板
                var plateUp = CreatePressurePlate("MergePlate_Up", parent.transform, new Vector3(8, 1, 0));
                var plateDown = CreatePressurePlate("MergePlate_Down", parent.transform, new Vector3(8, -1, 0));

                // 宝箱
                var giftBox = LoadItem("GiftBox");
                if (giftBox != null)
                    CreateItemPickup(giftBox, parent.transform, new Vector3(10, 0, 0));

                return parent;
            }
        };

        // ==================== 经典设计 (ClassicDesign) ====================

        /// <summary>
        /// 城堡关卡布局 - 经典平台游戏结构
        /// </summary>
        public static LevelElementTemplate CastleLayout = new LevelElementTemplate
        {
            name = "城堡关卡",
            emoji = "🏰",
            category = LevelElementCategory.ClassicDesign,
            description = "经典超级马里奥风格：平台、深坑、移动平台、终点奖励",
            coopTip = "一人在前探路，一人保护后方",
            designNotes = "黄金比例：60%平台、20%深坑、20%机关",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("CastleLayout");
                parent.transform.position = basePos;

                // 起点平台
                CreatePlatformObject("StartPlatform", parent.transform, new Vector3(0, 0, 0), 4f);

                // 第一段跳跃
                CreatePlatformObject("Platform_1", parent.transform, new Vector3(5, 1, 0), 2f);
                CreatePlatformObject("Platform_2", parent.transform, new Vector3(8, 2, 0), 2f);

                // 移动平台区域
                CreateMovingPlatform("MovePlat", parent.transform,
                    new Vector3(11, 2, 0), new Vector3(14, 4, 0), 2f);

                // 上层平台
                CreatePlatformObject("Platform_3", parent.transform, new Vector3(17, 4, 0), 3f);

                // 下坡冲刺
                CreateSlopeObject("FinishSlope", parent.transform, -20f, 4f, false);
                parent.transform.Find("FinishSlope").localPosition = new Vector3(21, 4, 0);

                // 终点奖励
                var cola = LoadItem("Cola");
                var cherry = LoadItem("Cherry");
                if (cola != null)
                    CreateItemPickup(cola, parent.transform, new Vector3(26, 3, 0));
                if (cherry != null)
                    CreateItemPickup(cherry, parent.transform, new Vector3(27, 3, 0));

                return parent;
            }
        };

        /// <summary>
        /// 地下洞穴 - 垂直探索
        /// </summary>
        public static LevelElementTemplate CaveLayout = new LevelElementTemplate
        {
            name = "地下洞穴",
            emoji = "🕳️",
            category = LevelElementCategory.ClassicDesign,
            description = "垂直向下探索，传送门返回",
            coopTip = "一人先跳下探路，确认安全后再叫队友",
            designNotes = "垂直探索增加紧张感。确保有返回路径。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("CaveLayout");
                parent.transform.position = basePos;

                // 入口
                CreatePlatformObject("Entrance", parent.transform, new Vector3(0, 0, 0), 4f);

                // 螺旋下降的平台
                CreatePlatformObject("Cave_1", parent.transform, new Vector3(3, -2, 0), 2f);
                CreatePlatformObject("Cave_2", parent.transform, new Vector3(0, -4, 0), 2f);
                CreatePlatformObject("Cave_3", parent.transform, new Vector3(-3, -6, 0), 2f);
                CreatePlatformObject("Cave_4", parent.transform, new Vector3(0, -8, 0), 3f);

                // 底部宝藏
                var gummyFish = LoadItem("GummyFish");
                if (gummyFish != null)
                    CreateItemPickup(gummyFish, parent.transform, new Vector3(0, -7, 0));

                // 返回传送门
                var portalIn = CreatePortalObject("Return_In", parent.transform, new Vector3(2, -8, 0));
                var portalOut = CreatePortalObject("Return_Out", parent.transform, new Vector3(3, 1, 0));
                portalIn.GetComponent<Portal>().targetPoint = portalOut.transform;
                portalIn.GetComponent<Portal>().portalColor = new Color(0.3f, 1f, 0.5f);

                return parent;
            }
        };

        /// <summary>
        /// 竞速跑道 - 双人竞技
        /// </summary>
        public static LevelElementTemplate RaceTrack = new LevelElementTemplate
        {
            name = "竞速跑道",
            emoji = "🏁",
            category = LevelElementCategory.ClassicDesign,
            description = "平行双跑道，看谁先到终点",
            coopTip = "可以选择竞争或合作（帮队友清除障碍）",
            designNotes = "两条跑道难度要平衡。中间可以有互动点。",
            CreateInScene = (basePos) =>
            {
                var parent = new GameObject("RaceTrack");
                parent.transform.position = basePos;

                // 上跑道（绿熊）
                CreatePlatformObject("Track_Green", parent.transform, new Vector3(4, 2, 0), 8f);
                var greenItem = LoadItem("GreenMelon");
                if (greenItem != null)
                    CreateItemPickup(greenItem, parent.transform, new Vector3(6, 3, 0));

                // 下跑道（红熊）
                CreatePlatformObject("Track_Red", parent.transform, new Vector3(4, -2, 0), 8f);
                var redItem = LoadItem("Strawberry");
                if (redItem != null)
                    CreateItemPickup(redItem, parent.transform, new Vector3(6, -1, 0));

                // 终点线（汇合）
                CreatePlatformObject("FinishLine", parent.transform, new Vector3(12, 0, 0), 4f);

                // 终点奖励
                var cola = LoadItem("Cola");
                if (cola != null)
                    CreateItemPickup(cola, parent.transform, new Vector3(13, 1, 0));

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

        private static GameObject CreatePlatformObject(string name, Transform parent, Vector3 localPos, float width)
        {
            var platform = new GameObject(name);
            platform.transform.parent = parent;
            platform.transform.localPosition = localPos;

            var sr = platform.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.4f, 0.3f, 0.2f);
            sr.sortingOrder = 0;

            // 创建简单的平台sprite
            int w = (int)(width * 32), h = 16;
            Texture2D tex = new Texture2D(w, h);
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);

            var col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.5f);

            return platform;
        }

        private static GameObject CreateMovingPlatform(string name, Transform parent, Vector3 startPos, Vector3 endPos, float speed)
        {
            var platform = CreatePlatformObject(name, parent, startPos, 3f);

            // 添加移动脚本标记（实际移动逻辑需要另外实现）
            var mover = platform.AddComponent<PlatformMover>();
            mover.startPosition = startPos;
            mover.endPosition = endPos;
            mover.moveSpeed = speed;

            platform.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.6f);

            return platform;
        }

        private static void CreateDisappearingPlatformObject(string name, Transform parent, Vector3 localPos,
            float fadeDelay, float fadeDuration, float respawnTime)
        {
            var platform = CreatePlatformObject(name, parent, localPos, 2f);
            platform.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.4f, 0.4f);

            // 添加消失逻辑标记
            var disappear = platform.AddComponent<DisappearingPlatform>();
            disappear.fadeDelay = fadeDelay;
            disappear.fadeDuration = fadeDuration;
            disappear.respawnTime = respawnTime;
        }

        private static GameObject CreateOneWayPlatformObject(string name, Vector3 pos, float width)
        {
            var platform = new GameObject(name);
            platform.transform.position = pos;

            var sr = platform.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0.5f, 0.3f);

            var col = platform.AddComponent<PlatformEffector2D>();
            var boxCol = platform.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(width, 0.3f);
            boxCol.usedByEffector = true;

            return platform;
        }

        private static GameObject CreateBouncePlatformObject(string name, Vector3 pos, float bounceForce)
        {
            var platform = new GameObject(name);
            platform.transform.position = pos;

            var sr = platform.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.5f, 0.8f);

            var col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 0.5f);

            // 添加弹跳逻辑标记
            var bounce = platform.AddComponent<BouncePlatform>();
            bounce.bounceForce = bounceForce;

            return platform;
        }

        private static GameObject CreateSlopeObject(string name, Transform parent, float angle, float length, bool addCollider)
        {
            var slope = new GameObject(name);
            slope.transform.parent = parent;
            slope.transform.localPosition = Vector3.zero;
            slope.transform.rotation = Quaternion.Euler(0, 0, angle);

            var sr = slope.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.4f, 0.3f);

            if (addCollider)
            {
                var col = slope.AddComponent<BoxCollider2D>();
                col.size = new Vector2(length, 0.5f);
            }

            return slope;
        }

        private static GameObject CreatePitObject(string name, Vector3 pos, float width, float depth)
        {
            var pit = new GameObject(name);
            pit.transform.position = pos;

            // 左边缘
            var leftEdge = CreatePlatformObject("LeftEdge", pit.transform, new Vector3(-width / 2 - 1, 0, 0), 2f);
            // 右边缘
            var rightEdge = CreatePlatformObject("RightEdge", pit.transform, new Vector3(width / 2 + 1, 0, 0), 2f);
            // 底部（死亡区域或重生点）
            var bottom = new GameObject("PitBottom");
            bottom.transform.parent = pit.transform;
            bottom.transform.localPosition = new Vector3(0, -depth, 0);

            return pit;
        }

        private static GameObject CreateIceSurfaceObject(string name, Vector3 pos, float width)
        {
            var ice = CreatePlatformObject(name, null, pos, width);
            ice.GetComponent<SpriteRenderer>().color = new Color(0.7f, 0.9f, 1f);

            // 添加低摩擦物理材质标记
            var iceSurface = ice.AddComponent<IceSurface>();
            iceSurface.friction = 0.05f;

            return ice;
        }

        private static GameObject CreateDoorObject(string name, Transform parent, Vector3 localPos)
        {
            var door = new GameObject(name);
            door.transform.parent = parent;
            door.transform.localPosition = localPos;

            var sr = door.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.3f, 0.2f);

            var col = door.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 3f);

            // 门逻辑
            var doorComp = door.AddComponent<Door>();

            return door;
        }

        private static GameObject CreateSeesawObject(string name, Vector3 pos, float width)
        {
            var seesaw = new GameObject(name);
            seesaw.transform.position = pos;

            // 主体
            var plank = new GameObject("Plank");
            plank.transform.parent = seesaw.transform;
            var sr = plank.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.5f, 0.3f);

            var rb = plank.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;

            var col = plank.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.3f);

            // 支点
            var pivot = new GameObject("Pivot");
            pivot.transform.parent = seesaw.transform;
            pivot.transform.localPosition = Vector3.zero;

            var hinge = plank.AddComponent<HingeJoint2D>();
            hinge.connectedBody = null;
            hinge.anchor = Vector2.zero;

            return seesaw;
        }

        private static GameObject CreateConveyorBeltObject(string name, Vector3 pos, float width, float speed, bool moveRight)
        {
            var belt = CreatePlatformObject(name, null, pos, width);
            belt.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.3f, 0.4f);

            var conveyor = belt.AddComponent<ConveyorBelt>();
            conveyor.speed = speed;
            conveyor.moveRight = moveRight;

            return belt;
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
            sr.color = new Color(0.6f, 0.6f, 0.6f);
            sr.sortingOrder = 5;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 0.3f);
            col.isTrigger = true;

            return go;
        }

        #endregion
    }

    // ==================== 占位组件（需要实际实现） ====================

    /// <summary>
    /// 移动平台组件
    /// </summary>
    public class PlatformMover : MonoBehaviour
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public float moveSpeed = 2f;

        private bool movingToEnd = true;

        void Update()
        {
            Vector3 target = movingToEnd ? endPosition : startPosition;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.localPosition, target) < 0.1f)
                movingToEnd = !movingToEnd;
        }
    }

    /// <summary>
    /// 消失平台组件
    /// </summary>
    public class DisappearingPlatform : MonoBehaviour
    {
        public float fadeDelay = 1f;
        public float fadeDuration = 1f;
        public float respawnTime = 3f;

        private bool isTriggered = false;
        private SpriteRenderer sr;
        private Collider2D col;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isTriggered)
            {
                isTriggered = true;
                StartCoroutine(FadeAndRespawn());
            }
        }

        System.Collections.IEnumerator FadeAndRespawn()
        {
            yield return new WaitForSeconds(fadeDelay);

            // 淡出
            float elapsed = 0;
            Color originalColor = sr.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1 - (elapsed / fadeDuration);
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            col.enabled = false;
            sr.enabled = false;

            yield return new WaitForSeconds(respawnTime);

            // 重生
            sr.color = originalColor;
            sr.enabled = true;
            col.enabled = true;
            isTriggered = false;
        }
    }

    /// <summary>
    /// 弹跳平台组件
    /// </summary>
    public class BouncePlatform : MonoBehaviour
    {
        public float bounceForce = 10f;

        void OnCollisionEnter2D(Collision2D collision)
        {
            var rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
            }
        }
    }

    /// <summary>
    /// 冰面组件
    /// </summary>
    public class IceSurface : MonoBehaviour
    {
        public float friction = 0.05f;

        void Start()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                var material = new PhysicsMaterial2D("Ice");
                material.friction = friction;
                material.bounciness = 0;
                col.sharedMaterial = material;
            }
        }
    }

    /// <summary>
    /// 门组件
    /// </summary>
    public class Door : MonoBehaviour
    {
        public bool isOpen = false;

        public void Open()
        {
            isOpen = true;
            GetComponent<Collider2D>().enabled = false;
            GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.3f, 0.2f, 0.3f);
        }

        public void Close()
        {
            isOpen = false;
            GetComponent<Collider2D>().enabled = true;
            GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.3f, 0.2f, 1f);
        }
    }

    /// <summary>
    /// 传送带组件
    /// </summary>
    public class ConveyorBelt : MonoBehaviour
    {
        public float speed = 3f;
        public bool moveRight = true;

        void OnCollisionStay2D(Collision2D collision)
        {
            var rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float dir = moveRight ? 1 : -1;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x + speed * dir * Time.deltaTime, rb.linearVelocity.y);
            }
        }
    }
}
#endif
