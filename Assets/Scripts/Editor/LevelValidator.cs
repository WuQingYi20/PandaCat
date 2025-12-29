#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Item;
using BearCar.Level;
using System.Collections.Generic;
using System.Linq;

namespace BearCar.Editor
{
    /// <summary>
    /// 关卡验证器 - 全面检查关卡质量
    /// </summary>
    public static class LevelValidator
    {
        // ========== 验证结果 ==========

        public static List<ValidationResult> Results { get; private set; } = new List<ValidationResult>();

        // ========== 验证参数 ==========

        private const float REACHABILITY_CHECK_DISTANCE = 15f; // 可达性检测最大距离
        private const float ITEM_CLUSTER_DISTANCE = 3f;        // 道具聚集检测距离
        private const float AHA_DENSITY_MIN = 0.1f;            // 最低Aha密度 (每10单位)
        private const float AHA_DENSITY_MAX = 0.5f;            // 最高Aha密度
        private const int COLOR_BALANCE_THRESHOLD = 2;         // 颜色平衡阈值

        /// <summary>
        /// 运行完整验证
        /// </summary>
        public static List<ValidationResult> RunFullValidation()
        {
            Results.Clear();

            // 收集场景数据
            var pickups = Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
            var portals = Object.FindObjectsByType<Portal>(FindObjectsSortMode.None);
            var plates = Object.FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);

            // 1. 基础检查
            ValidateColorBalance(pickups);
            ValidatePortalConnections(portals);
            ValidateTrapToolBalance(pickups);

            // 2. 可达性检查
            ValidateReachability(pickups, portals);

            // 3. 难度曲线分析
            ValidateDifficultyCurve(pickups);

            // 4. Aha密度检测
            ValidateAhaDensity(pickups, portals, plates);

            // 5. 自动平衡建议
            GenerateBalanceSuggestions(pickups, portals, plates);

            Debug.Log($"[LevelValidator] 验证完成: {Results.Count} 条结果");
            return Results;
        }

        #region 1. 基础检查

        /// <summary>
        /// 检查道具颜色平衡
        /// </summary>
        private static void ValidateColorBalance(ItemPickup[] pickups)
        {
            int greenCount = pickups.Count(p => p.itemData?.colorAffinity == ColorAffinity.Green);
            int redCount = pickups.Count(p => p.itemData?.colorAffinity == ColorAffinity.Red);
            int neutralCount = pickups.Count(p => p.itemData?.colorAffinity == ColorAffinity.None);

            int diff = Mathf.Abs(greenCount - redCount);

            if (greenCount == 0 && redCount == 0)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Warning,
                    category = "颜色平衡",
                    message = "场景中没有颜色亲和道具，无法触发颜色交换的Aha体验",
                    suggestion = "添加至少2个绿色和2个红色道具"
                });
            }
            else if (diff > COLOR_BALANCE_THRESHOLD)
            {
                string lessColor = greenCount < redCount ? "绿色" : "红色";
                string moreColor = greenCount > redCount ? "绿色" : "红色";
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Warning,
                    category = "颜色平衡",
                    message = $"{lessColor}道具较少 (绿:{greenCount} 红:{redCount})，{lessColor}玩家可能体验不佳",
                    suggestion = $"添加 {diff} 个{lessColor}道具，或减少 {diff} 个{moreColor}道具"
                });
            }
            else
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Pass,
                    category = "颜色平衡",
                    message = $"颜色道具平衡 (绿:{greenCount} 红:{redCount} 中性:{neutralCount})"
                });
            }
        }

        /// <summary>
        /// 检查传送门连接
        /// </summary>
        private static void ValidatePortalConnections(Portal[] portals)
        {
            int disconnectedCount = 0;

            foreach (var portal in portals)
            {
                if (portal.targetPoint == null && portal.direction != PortalDirection.OneWayOut)
                {
                    disconnectedCount++;
                    Results.Add(new ValidationResult
                    {
                        type = ValidationType.Error,
                        category = "传送门",
                        message = $"传送门 '{portal.portalId}' 没有目标点，玩家进入后无法传送",
                        relatedObject = portal.gameObject,
                        suggestion = "设置目标点或删除此传送门"
                    });
                }
            }

            if (disconnectedCount == 0 && portals.Length > 0)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Pass,
                    category = "传送门",
                    message = $"所有 {portals.Length} 个传送门连接正常"
                });
            }
        }

        /// <summary>
        /// 检查陷阱和工具平衡
        /// </summary>
        private static void ValidateTrapToolBalance(ItemPickup[] pickups)
        {
            var traps = pickups.Where(p =>
                p.itemData?.itemType == ItemType.Trap_Nail ||
                p.itemData?.itemType == ItemType.Trap_PermanentNail ||
                p.itemData?.itemType == ItemType.Trap_StickyPad).ToList();

            var tools = pickups.Where(p =>
                p.itemData?.itemType == ItemType.Tool_Shovel).ToList();

            int removableTraps = traps.Count(t => t.itemData.itemType != ItemType.Trap_PermanentNail);

            if (traps.Count > 0 && tools.Count == 0)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Warning,
                    category = "陷阱平衡",
                    message = $"有 {traps.Count} 个陷阱但没有铲子，玩家可能无法通过",
                    suggestion = "添加至少1个铲子，或将部分陷阱改为可绕过"
                });
            }
            else if (removableTraps > 0 && tools.Count < removableTraps / 2)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "陷阱平衡",
                    message = $"可移除陷阱({removableTraps})是铲子({tools.Count})的2倍以上",
                    suggestion = $"建议添加 {(removableTraps / 2) - tools.Count + 1} 把铲子"
                });
            }
            else if (traps.Count > 0)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Pass,
                    category = "陷阱平衡",
                    message = $"陷阱({traps.Count})和工具({tools.Count})比例合理"
                });
            }
        }

        #endregion

        #region 2. 可达性检查

        /// <summary>
        /// 检查道具可达性 - 使用简单的距离和高度差分析
        /// </summary>
        private static void ValidateReachability(ItemPickup[] pickups, Portal[] portals)
        {
            if (pickups.Length == 0) return;

            // 找到起点（假设是最左边的位置或车的位置）
            var cart = Object.FindFirstObjectByType<BearCar.Cart.CartController>();
            Vector3 startPos = cart != null ? cart.transform.position :
                pickups.OrderBy(p => p.transform.position.x).First().transform.position;

            List<ItemPickup> unreachableItems = new List<ItemPickup>();
            List<ItemPickup> hardToReachItems = new List<ItemPickup>();

            foreach (var pickup in pickups)
            {
                float distance = Vector3.Distance(startPos, pickup.transform.position);
                float heightDiff = pickup.transform.position.y - startPos.y;

                // 检查是否有传送门可以到达
                bool hasPortalAccess = portals.Any(p =>
                    Vector3.Distance(p.transform.position, pickup.transform.position) < 3f ||
                    (p.targetPoint != null && Vector3.Distance(p.targetPoint.position, pickup.transform.position) < 3f));

                // 高度差过大且没有传送门
                if (heightDiff > 8f && !hasPortalAccess)
                {
                    unreachableItems.Add(pickup);
                }
                // 高度差较大
                else if (heightDiff > 5f && !hasPortalAccess)
                {
                    hardToReachItems.Add(pickup);
                }
                // 距离太远且是孤立的
                else if (distance > REACHABILITY_CHECK_DISTANCE)
                {
                    bool hasNearbyItems = pickups.Any(other =>
                        other != pickup &&
                        Vector3.Distance(other.transform.position, pickup.transform.position) < 5f);

                    if (!hasNearbyItems && !hasPortalAccess)
                    {
                        hardToReachItems.Add(pickup);
                    }
                }
            }

            if (unreachableItems.Count > 0)
            {
                foreach (var item in unreachableItems)
                {
                    Results.Add(new ValidationResult
                    {
                        type = ValidationType.Error,
                        category = "可达性",
                        message = $"道具 '{item.itemData?.itemName}' 可能无法到达 (高度差过大)",
                        relatedObject = item.gameObject,
                        suggestion = "添加传送门、移动平台或降低高度"
                    });
                }
            }

            if (hardToReachItems.Count > 0)
            {
                foreach (var item in hardToReachItems)
                {
                    Results.Add(new ValidationResult
                    {
                        type = ValidationType.Warning,
                        category = "可达性",
                        message = $"道具 '{item.itemData?.itemName}' 较难到达",
                        relatedObject = item.gameObject,
                        suggestion = "考虑添加辅助到达方式或作为隐藏奖励"
                    });
                }
            }

            int reachableCount = pickups.Length - unreachableItems.Count;
            if (unreachableItems.Count == 0)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Pass,
                    category = "可达性",
                    message = $"所有 {pickups.Length} 个道具都可到达"
                });
            }
        }

        #endregion

        #region 3. 难度曲线分析

        /// <summary>
        /// 分析关卡难度曲线 - 基于道具分布
        /// </summary>
        private static void ValidateDifficultyCurve(ItemPickup[] pickups)
        {
            if (pickups.Length < 3) return;

            // 按X坐标排序（假设关卡从左到右）
            var sortedPickups = pickups.OrderBy(p => p.transform.position.x).ToArray();

            // 分成3段：开始、中间、结尾
            int segmentSize = sortedPickups.Length / 3;
            var startSegment = sortedPickups.Take(segmentSize).ToList();
            var middleSegment = sortedPickups.Skip(segmentSize).Take(segmentSize).ToList();
            var endSegment = sortedPickups.Skip(segmentSize * 2).ToList();

            // 计算每段的"难度值"（陷阱-工具-食物）
            float startDifficulty = CalculateSegmentDifficulty(startSegment);
            float middleDifficulty = CalculateSegmentDifficulty(middleSegment);
            float endDifficulty = CalculateSegmentDifficulty(endSegment);

            // 检查开局难度
            if (startDifficulty > 0.5f)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Warning,
                    category = "难度曲线",
                    message = "关卡开始部分难度较高，可能让新玩家受挫",
                    suggestion = "在开始区域添加更多食物或减少陷阱"
                });
            }

            // 检查中段奖励
            if (middleDifficulty > endDifficulty + 0.3f)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "难度曲线",
                    message = "中段比结尾更难，考虑调整为逐渐上升的难度",
                    suggestion = "将部分中段陷阱移至后段，或在中段添加奖励"
                });
            }

            // 检查道具密度
            float levelWidth = sortedPickups.Last().transform.position.x - sortedPickups.First().transform.position.x;
            float density = pickups.Length / Mathf.Max(levelWidth, 1f);

            if (density < 0.3f)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "难度曲线",
                    message = $"道具密度较低 ({density:F2}/单位)，关卡可能感觉空旷",
                    suggestion = "在道具之间添加中性道具或视觉元素"
                });
            }
            else if (density > 1.5f)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "难度曲线",
                    message = $"道具密度很高 ({density:F2}/单位)，可能让玩家应接不暇",
                    suggestion = "适当减少道具数量或增加间距"
                });
            }
            else
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Pass,
                    category = "难度曲线",
                    message = $"道具密度适中 ({density:F2}/单位)"
                });
            }
        }

        private static float CalculateSegmentDifficulty(List<ItemPickup> segment)
        {
            if (segment.Count == 0) return 0;

            float difficulty = 0;
            foreach (var pickup in segment)
            {
                if (pickup.itemData == null) continue;

                switch (pickup.itemData.itemType)
                {
                    case ItemType.Trap_Nail:
                        difficulty += 1f;
                        break;
                    case ItemType.Trap_PermanentNail:
                        difficulty += 1.5f;
                        break;
                    case ItemType.Trap_StickyPad:
                        difficulty += 0.5f;
                        break;
                    case ItemType.Poison:
                        difficulty += 0.8f;
                        break;
                    case ItemType.Food:
                        difficulty -= 0.3f;
                        break;
                    case ItemType.Tool_Shovel:
                        difficulty -= 0.5f;
                        break;
                }
            }

            return difficulty / segment.Count;
        }

        #endregion

        #region 4. Aha密度检测

        /// <summary>
        /// 检测Aha触发点密度
        /// </summary>
        private static void ValidateAhaDensity(ItemPickup[] pickups, Portal[] portals, PressurePlate[] plates)
        {
            // 统计Aha触发点
            List<AhaPoint> ahaPoints = new List<AhaPoint>();

            // 1. 颜色互换点 - 红绿道具相邻
            foreach (var pickup in pickups)
            {
                if (pickup.itemData?.colorAffinity == ColorAffinity.Green)
                {
                    var nearbyRed = pickups.FirstOrDefault(p =>
                        p.itemData?.colorAffinity == ColorAffinity.Red &&
                        Vector3.Distance(p.transform.position, pickup.transform.position) < ITEM_CLUSTER_DISTANCE);

                    if (nearbyRed != null)
                    {
                        ahaPoints.Add(new AhaPoint
                        {
                            type = AhaCategory.Discovery,
                            position = (pickup.transform.position + nearbyRed.transform.position) / 2,
                            description = "颜色互换"
                        });
                    }
                }
            }

            // 2. 合作传送门
            foreach (var portal in portals)
            {
                if (portal.activation == PortalActivation.CoopTrigger)
                {
                    ahaPoints.Add(new AhaPoint
                    {
                        type = AhaCategory.Synchronization,
                        position = portal.transform.position,
                        description = $"合作传送门 (需{portal.requiredPlayers}人)"
                    });
                }
            }

            // 3. 压力板机关
            foreach (var plate in plates)
            {
                if (plate.linkedObjects != null && plate.linkedObjects.Length > 0)
                {
                    ahaPoints.Add(new AhaPoint
                    {
                        type = AhaCategory.Synchronization,
                        position = plate.transform.position,
                        description = "压力板机关"
                    });
                }
            }

            // 4. 分工合作点 - 铲子和石头/陷阱相邻
            var shovels = pickups.Where(p => p.itemData?.itemType == ItemType.Tool_Shovel);
            var obstacles = pickups.Where(p =>
                p.itemData?.itemType == ItemType.Trap_Nail ||
                p.itemData?.itemType == ItemType.Obstacle_Rock);

            foreach (var shovel in shovels)
            {
                var nearbyObstacle = obstacles.FirstOrDefault(o =>
                    Vector3.Distance(o.transform.position, shovel.transform.position) < 8f);

                if (nearbyObstacle != null)
                {
                    ahaPoints.Add(new AhaPoint
                    {
                        type = AhaCategory.Sacrifice,
                        position = (shovel.transform.position + nearbyObstacle.transform.position) / 2,
                        description = "分工合作"
                    });
                }
            }

            // 5. 组合道具 - 曼妥思和可乐
            var mentos = pickups.FirstOrDefault(p => p.itemData?.itemName == "Mentos");
            var cola = pickups.FirstOrDefault(p => p.itemData?.itemName == "Cola");
            if (mentos != null && cola != null)
            {
                float distance = Vector3.Distance(mentos.transform.position, cola.transform.position);
                if (distance < 10f)
                {
                    ahaPoints.Add(new AhaPoint
                    {
                        type = AhaCategory.Discovery,
                        position = (mentos.transform.position + cola.transform.position) / 2,
                        description = "隐藏组合 (曼妥思+可乐)"
                    });
                }
            }

            // 计算Aha密度
            float levelWidth = pickups.Length > 0
                ? pickups.Max(p => p.transform.position.x) - pickups.Min(p => p.transform.position.x)
                : 10f;
            levelWidth = Mathf.Max(levelWidth, 10f);

            float ahaDensity = ahaPoints.Count / (levelWidth / 10f);

            // 分类统计
            var categoryCount = new Dictionary<AhaCategory, int>();
            foreach (var point in ahaPoints)
            {
                if (!categoryCount.ContainsKey(point.type))
                    categoryCount[point.type] = 0;
                categoryCount[point.type]++;
            }

            // 生成报告
            if (ahaPoints.Count == 0)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Warning,
                    category = "Aha密度",
                    message = "没有检测到Aha触发点，关卡缺少合作高潮",
                    suggestion = "使用Aha模板添加至少1个合作触发点"
                });
            }
            else if (ahaDensity < AHA_DENSITY_MIN)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "Aha密度",
                    message = $"Aha密度较低 ({ahaDensity:F2})，合作体验可能不够充分",
                    suggestion = $"当前有 {ahaPoints.Count} 个Aha点，建议添加更多"
                });
            }
            else if (ahaDensity > AHA_DENSITY_MAX)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "Aha密度",
                    message = $"Aha密度很高 ({ahaDensity:F2})，可能让玩家疲劳",
                    suggestion = "适当减少Aha触发点，保持稀缺感"
                });
            }
            else
            {
                string breakdown = string.Join(", ", categoryCount.Select(kv => $"{GetCategoryEmoji(kv.Key)}{kv.Value}"));
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Pass,
                    category = "Aha密度",
                    message = $"Aha密度适中 ({ahaDensity:F2})，共{ahaPoints.Count}个触发点 [{breakdown}]"
                });
            }

            // 检查类型多样性
            if (ahaPoints.Count >= 3 && categoryCount.Count < 2)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "Aha多样性",
                    message = "Aha类型单一，考虑增加不同类型的合作体验",
                    suggestion = "尝试添加不同类别的Aha模板"
                });
            }
        }

        private static string GetCategoryEmoji(AhaCategory category)
        {
            return category switch
            {
                AhaCategory.Discovery => "🔍",
                AhaCategory.Synchronization => "🤝",
                AhaCategory.Sacrifice => "💚",
                AhaCategory.Rescue => "🆘",
                AhaCategory.Optimization => "🎯",
                _ => "✨"
            };
        }

        #endregion

        #region 5. 自动平衡建议

        /// <summary>
        /// 生成自动平衡建议
        /// </summary>
        private static void GenerateBalanceSuggestions(ItemPickup[] pickups, Portal[] portals, PressurePlate[] plates)
        {
            List<string> suggestions = new List<string>();

            // 计算各项统计
            int greenCount = pickups.Count(p => p.itemData?.colorAffinity == ColorAffinity.Green);
            int redCount = pickups.Count(p => p.itemData?.colorAffinity == ColorAffinity.Red);
            int coopPortalCount = portals.Count(p => p.activation == PortalActivation.CoopTrigger);
            int plateCount = plates.Length;

            // 生成具体建议
            if (greenCount == 0 || redCount == 0)
            {
                suggestions.Add($"+ 添加 {(greenCount == 0 ? "绿色" : "红色")}道具触发颜色互换");
            }

            if (coopPortalCount == 0 && portals.Length > 0)
            {
                suggestions.Add("+ 将一个传送门改为合作传送门");
            }

            if (plateCount == 0 && pickups.Length > 5)
            {
                suggestions.Add("+ 添加压力板机关增加互动");
            }

            if (pickups.Length > 10)
            {
                var giftBoxes = pickups.Where(p => p.itemData?.itemType == ItemType.Special_GiftBox);
                if (!giftBoxes.Any())
                {
                    suggestions.Add("+ 添加礼物盒增加惊喜感");
                }
            }

            // 输出建议
            if (suggestions.Count > 0)
            {
                Results.Add(new ValidationResult
                {
                    type = ValidationType.Suggestion,
                    category = "平衡建议",
                    message = "快速优化建议:",
                    suggestion = string.Join("\n", suggestions)
                });
            }
        }

        #endregion
    }

    /// <summary>
    /// Aha触发点数据
    /// </summary>
    public class AhaPoint
    {
        public AhaCategory type;
        public Vector3 position;
        public string description;
    }
}
#endif
