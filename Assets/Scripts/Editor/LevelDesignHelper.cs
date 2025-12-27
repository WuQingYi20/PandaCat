#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Item;
using System.Collections.Generic;

namespace BearCar.Editor
{
    /// <summary>
    /// 关卡设计辅助工具 - Scene 视图中的道具放置面板
    /// </summary>
    public class LevelDesignHelper : EditorWindow
    {
        private static LevelDesignHelper instance;
        private Vector2 scrollPosition;
        private ItemData[] quickAccessItems;
        private int selectedCategory = 0;
        private string[] categories = { "全部", "绿色食物", "红色食物", "中性", "陷阱", "工具", "特殊" };

        [MenuItem("BearCar/关卡设计/快速放置面板 #&P")]
        public static void ShowWindow()
        {
            instance = GetWindow<LevelDesignHelper>("快速放置");
            instance.minSize = new Vector2(200, 300);
            instance.LoadItems();
        }

        private void OnEnable()
        {
            LoadItems();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void LoadItems()
        {
            var items = new List<ItemData>();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Resources/Items" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            quickAccessItems = items.ToArray();
        }

        private void OnGUI()
        {
            // 分类标签
            selectedCategory = GUILayout.Toolbar(selectedCategory, categories);

            EditorGUILayout.Space();

            // 道具列表
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (quickAccessItems == null || quickAccessItems.Length == 0)
            {
                EditorGUILayout.HelpBox("没有找到道具。\n请先创建预设道具：\nBearCar → 道具系统 → 创建所有预设道具", MessageType.Warning);
                if (GUILayout.Button("创建预设道具"))
                {
                    ItemSetupEditor.CreateAllPresetItems();
                    LoadItems();
                }
            }
            else
            {
                foreach (var item in quickAccessItems)
                {
                    if (!MatchesCategory(item)) continue;
                    DrawItemButton(item);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 底部工具
            DrawBottomTools();
        }

        private bool MatchesCategory(ItemData item)
        {
            switch (selectedCategory)
            {
                case 0: return true; // 全部
                case 1: return item.colorAffinity == ColorAffinity.Green && item.itemType == ItemType.Food;
                case 2: return item.colorAffinity == ColorAffinity.Red && item.itemType == ItemType.Food;
                case 3: return item.colorAffinity == ColorAffinity.None &&
                              (item.itemType == ItemType.Food || item.itemType == ItemType.Poison);
                case 4: return item.itemType == ItemType.Trap_Nail ||
                              item.itemType == ItemType.Trap_PermanentNail ||
                              item.itemType == ItemType.Trap_StickyPad;
                case 5: return item.itemType == ItemType.Tool_Shovel ||
                              item.itemType == ItemType.Tool_SoulNet;
                case 6: return item.itemType == ItemType.Special_GiftBox ||
                              item.itemType == ItemType.Special_PuddingPad ||
                              item.itemType == ItemType.RocketBoost;
                default: return true;
            }
        }

        private void DrawItemButton(ItemData item)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 颜色预览
            Rect colorRect = GUILayoutUtility.GetRect(25, 25, GUILayout.Width(25));
            EditorGUI.DrawRect(colorRect, item.itemColor);

            // 名称
            EditorGUILayout.LabelField(GetItemEmoji(item) + " " + item.itemName, GUILayout.ExpandWidth(true));

            // 放置按钮
            if (GUILayout.Button("放置", GUILayout.Width(50)))
            {
                PlaceItem(item);
            }

            EditorGUILayout.EndHorizontal();
        }

        private string GetItemEmoji(ItemData item)
        {
            switch (item.colorAffinity)
            {
                case ColorAffinity.Green: return "🟢";
                case ColorAffinity.Red: return "🔴";
                default:
                    switch (item.itemType)
                    {
                        case ItemType.Poison: return "☠️";
                        case ItemType.Tool_Shovel: return "🔧";
                        case ItemType.Tool_SoulNet: return "👻";
                        case ItemType.Trap_Nail: return "📍";
                        case ItemType.RocketBoost: return "🚀";
                        case ItemType.Special_GiftBox: return "🎁";
                        default: return "⚪";
                    }
            }
        }

        private void PlaceItem(ItemData item)
        {
            GameObject pickup = new GameObject($"Pickup_{item.itemName}");

            // 添加必要组件
            var sr = pickup.AddComponent<SpriteRenderer>();
            var col = pickup.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            var pickupComp = pickup.AddComponent<ItemPickup>();
            pickupComp.itemData = item;

            // 设置视觉效果
            sr.sprite = CreateItemSprite(item.shape);
            sr.color = item.itemColor;
            sr.sortingOrder = 10;
            pickup.transform.localScale = Vector3.one * 0.8f;

            // 放在场景视图中心
            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                Vector3 pos = cam.transform.position + cam.transform.forward * 5f;
                pos.z = 0;
                pickup.transform.position = pos;
            }

            Undo.RegisterCreatedObjectUndo(pickup, "Place Item");
            Selection.activeGameObject = pickup;
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

        private void DrawBottomTools()
        {
            EditorGUILayout.LabelField("🔧 工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("刷新"))
            {
                LoadItems();
            }

            if (GUILayout.Button("浏览器"))
            {
                ItemBrowserWindow.ShowWindow();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("创建生成点"))
            {
                ItemSetupEditor.CreateItemSpawner();
            }

            if (GUILayout.Button("分析场景"))
            {
                AnalyzeScene();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AnalyzeScene()
        {
            var pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);

            int greenCount = 0, redCount = 0, neutralCount = 0;
            int trapCount = 0, toolCount = 0, specialCount = 0;

            foreach (var p in pickups)
            {
                if (p.itemData == null) continue;

                switch (p.itemData.colorAffinity)
                {
                    case ColorAffinity.Green: greenCount++; break;
                    case ColorAffinity.Red: redCount++; break;
                    default: neutralCount++; break;
                }

                switch (p.itemData.itemType)
                {
                    case ItemType.Trap_Nail:
                    case ItemType.Trap_PermanentNail:
                    case ItemType.Trap_StickyPad:
                        trapCount++;
                        break;
                    case ItemType.Tool_Shovel:
                    case ItemType.Tool_SoulNet:
                        toolCount++;
                        break;
                    case ItemType.Special_GiftBox:
                    case ItemType.Special_PuddingPad:
                    case ItemType.RocketBoost:
                        specialCount++;
                        break;
                }
            }

            string report = $"=== 场景道具分析 ===\n\n" +
                           $"总道具数: {pickups.Length}\n\n" +
                           $"颜色分布:\n" +
                           $"  🟢 绿色系: {greenCount}\n" +
                           $"  🔴 红色系: {redCount}\n" +
                           $"  ⚪ 中性: {neutralCount}\n\n" +
                           $"类型分布:\n" +
                           $"  📍 陷阱: {trapCount}\n" +
                           $"  🔧 工具: {toolCount}\n" +
                           $"  ⭐ 特殊: {specialCount}\n\n";

            // 平衡性建议
            if (greenCount != redCount)
            {
                int diff = Mathf.Abs(greenCount - redCount);
                string lessColor = greenCount < redCount ? "绿色" : "红色";
                report += $"⚠️ 建议: {lessColor}道具较少 (差{diff}个)，考虑添加更多{lessColor}道具保持平衡\n";
            }
            else
            {
                report += "✓ 颜色道具数量平衡\n";
            }

            if (trapCount > 0 && toolCount == 0)
            {
                report += "⚠️ 建议: 有陷阱但没有工具，考虑添加铲子\n";
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("场景分析", report, "确定");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            // 在场景中显示道具信息
            var pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);

            foreach (var pickup in pickups)
            {
                if (pickup.itemData == null) continue;

                Vector3 pos = pickup.transform.position;

                // 绘制颜色圆圈
                Handles.color = pickup.itemData.itemColor;
                Handles.DrawWireDisc(pos, Vector3.forward, 0.6f);

                // 绘制亲和指示
                if (pickup.itemData.colorAffinity != ColorAffinity.None)
                {
                    Color affinityColor = pickup.itemData.colorAffinity == ColorAffinity.Green
                        ? new Color(0.2f, 0.8f, 0.3f)
                        : new Color(0.9f, 0.2f, 0.2f);

                    Handles.color = affinityColor;
                    Handles.DrawSolidDisc(pos + Vector3.up * 0.7f, Vector3.forward, 0.15f);
                }

                // 稀有度指示
                if (pickup.itemData.rarity >= ItemRarity.Rare)
                {
                    Handles.color = GetRarityColor(pickup.itemData.rarity);
                    float starSize = pickup.itemData.rarity == ItemRarity.Legendary ? 0.3f : 0.2f;

                    // 绘制星星
                    Vector3 starPos = pos + Vector3.up * 0.9f;
                    Handles.DrawSolidDisc(starPos, Vector3.forward, starSize);
                }
            }
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare: return new Color(0.2f, 0.5f, 1f);
                case ItemRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
                case ItemRarity.Legendary: return new Color(1f, 0.6f, 0.1f);
                default: return Color.gray;
            }
        }
    }

    /// <summary>
    /// 场景视图覆盖 - 显示道具统计
    /// </summary>
    [InitializeOnLoad]
    public static class SceneItemOverlay
    {
        static SceneItemOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            // 只在有道具时显示
            var pickups = Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
            if (pickups.Length == 0) return;

            Handles.BeginGUI();

            // 右上角显示道具统计
            Rect rect = new Rect(sceneView.position.width - 160, 10, 150, 60);

            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);
            GUILayout.Label($"📦 场景道具: {pickups.Length}", EditorStyles.boldLabel);

            int greenCount = 0, redCount = 0;
            foreach (var p in pickups)
            {
                if (p.itemData == null) continue;
                if (p.itemData.colorAffinity == ColorAffinity.Green) greenCount++;
                if (p.itemData.colorAffinity == ColorAffinity.Red) redCount++;
            }

            GUILayout.Label($"🟢 {greenCount}  🔴 {redCount}");
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}
#endif
