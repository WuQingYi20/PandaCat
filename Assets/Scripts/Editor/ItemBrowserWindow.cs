#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Item;
using System.Collections.Generic;
using System.Linq;

namespace BearCar.Editor
{
    /// <summary>
    /// 道具浏览器窗口 - 设计师友好的道具管理工具
    /// </summary>
    public class ItemBrowserWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private ItemData selectedItem;
        private string searchFilter = "";
        private ItemType? filterType = null;
        private ColorAffinity? filterAffinity = null;
        private ItemRarity? filterRarity = null;

        private List<ItemData> allItems = new List<ItemData>();
        private List<ItemData> filteredItems = new List<ItemData>();

        // 预览相关
        private Texture2D previewTexture;
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private bool stylesInitialized = false;

        // 放置模式
        private bool placementMode = false;
        private ItemData itemToPlace = null;

        [MenuItem("BearCar/道具系统/道具浏览器 #&I")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemBrowserWindow>("道具浏览器");
            window.minSize = new Vector2(800, 500);
            window.RefreshItemList();
        }

        private void OnEnable()
        {
            RefreshItemList();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            stylesInitialized = true;
        }

        private void RefreshItemList()
        {
            allItems.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Resources/Items" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    allItems.Add(item);
                }
            }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            filteredItems = allItems.Where(item =>
            {
                // 搜索过滤
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    if (!item.itemName.Contains(searchFilter) &&
                        !item.description.Contains(searchFilter))
                        return false;
                }

                // 类型过滤
                if (filterType.HasValue && item.itemType != filterType.Value)
                    return false;

                // 亲和过滤
                if (filterAffinity.HasValue && item.colorAffinity != filterAffinity.Value)
                    return false;

                // 稀有度过滤
                if (filterRarity.HasValue && item.rarity != filterRarity.Value)
                    return false;

                return true;
            }).ToList();
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginHorizontal();

            // 左侧：道具列表
            DrawItemList();

            // 右侧：道具详情
            DrawItemDetails();

            EditorGUILayout.EndHorizontal();

            // 底部：快捷操作栏
            DrawBottomToolbar();
        }

        private void DrawItemList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(350));

            // 搜索和过滤
            EditorGUILayout.LabelField("🔍 搜索与过滤", headerStyle);
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUI.BeginChangeCheck();
            searchFilter = EditorGUILayout.TextField("搜索", searchFilter);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(filterType == null, "全部", "Button", GUILayout.Width(50)))
                filterType = null;
            if (GUILayout.Toggle(filterType == ItemType.Food, "食物", "Button", GUILayout.Width(50)))
                filterType = ItemType.Food;
            if (GUILayout.Toggle(filterType == ItemType.Poison, "毒物", "Button", GUILayout.Width(50)))
                filterType = ItemType.Poison;
            if (GUILayout.Toggle(filterType == ItemType.Tool_Shovel || filterType == ItemType.Tool_SoulNet, "工具", "Button", GUILayout.Width(50)))
                filterType = ItemType.Tool_Shovel;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(filterAffinity == null, "全部", "Button", GUILayout.Width(50)))
                filterAffinity = null;
            if (GUILayout.Toggle(filterAffinity == ColorAffinity.Green, "绿色系", "Button", GUILayout.Width(60)))
                filterAffinity = ColorAffinity.Green;
            if (GUILayout.Toggle(filterAffinity == ColorAffinity.Red, "红色系", "Button", GUILayout.Width(60)))
                filterAffinity = ColorAffinity.Red;
            if (GUILayout.Toggle(filterAffinity == ColorAffinity.None, "中性", "Button", GUILayout.Width(50)))
                filterAffinity = ColorAffinity.None;
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                ApplyFilters();
            }

            EditorGUILayout.EndVertical();

            // 道具列表
            EditorGUILayout.LabelField($"📦 道具列表 ({filteredItems.Count})", headerStyle);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var item in filteredItems)
            {
                DrawItemEntry(item);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawItemEntry(ItemData item)
        {
            bool isSelected = selectedItem == item;
            Color bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;

            Rect rect = EditorGUILayout.BeginHorizontal("box", GUILayout.Height(40));

            // 背景
            if (isSelected)
            {
                EditorGUI.DrawRect(rect, bgColor);
            }

            // 颜色预览
            Rect colorRect = new Rect(rect.x + 5, rect.y + 5, 30, 30);
            EditorGUI.DrawRect(colorRect, item.itemColor);

            GUILayout.Space(40);

            // 道具信息
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(GetItemDisplayName(item), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(GetItemTypeLabel(item), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // 稀有度标签
            GUILayout.FlexibleSpace();
            DrawRarityBadge(item.rarity);

            EditorGUILayout.EndHorizontal();

            // 点击选择
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                selectedItem = item;
                Repaint();
            }
        }

        private string GetItemDisplayName(ItemData item)
        {
            string emoji = "";
            switch (item.colorAffinity)
            {
                case ColorAffinity.Green: emoji = "🟢 "; break;
                case ColorAffinity.Red: emoji = "🔴 "; break;
            }
            return emoji + item.itemName;
        }

        private string GetItemTypeLabel(ItemData item)
        {
            switch (item.itemType)
            {
                case ItemType.Food: return "食物";
                case ItemType.Poison: return "毒物";
                case ItemType.RocketBoost: return "推进";
                case ItemType.Trap_Nail: return "陷阱";
                case ItemType.Trap_PermanentNail: return "永久陷阱";
                case ItemType.Tool_Shovel: return "工具";
                case ItemType.Tool_SoulNet: return "复活工具";
                case ItemType.Special_GiftBox: return "礼物盒";
                default: return item.itemType.ToString();
            }
        }

        private void DrawRarityBadge(ItemRarity rarity)
        {
            Color badgeColor;
            string label;
            switch (rarity)
            {
                case ItemRarity.Common:
                    badgeColor = Color.gray;
                    label = "普通";
                    break;
                case ItemRarity.Rare:
                    badgeColor = new Color(0.2f, 0.5f, 1f);
                    label = "稀有";
                    break;
                case ItemRarity.Epic:
                    badgeColor = new Color(0.6f, 0.2f, 0.8f);
                    label = "史诗";
                    break;
                case ItemRarity.Legendary:
                    badgeColor = new Color(1f, 0.6f, 0.1f);
                    label = "传说";
                    break;
                default:
                    badgeColor = Color.gray;
                    label = "?";
                    break;
            }

            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = badgeColor;
            GUILayout.Label(label, "button", GUILayout.Width(50));
            GUI.backgroundColor = oldColor;
        }

        private void DrawItemDetails()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            if (selectedItem == null)
            {
                EditorGUILayout.HelpBox("← 选择一个道具查看详情", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("📋 道具详情", headerStyle);

            EditorGUILayout.BeginVertical(boxStyle);

            // 基本信息
            EditorGUILayout.LabelField("名称", selectedItem.itemName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("描述", selectedItem.description);
            EditorGUILayout.Space();

            // 效果预览
            EditorGUILayout.LabelField("⚡ 效果预览", EditorStyles.boldLabel);
            DrawEffectPreview();
            EditorGUILayout.Space();

            // 颜色亲和说明
            if (selectedItem.colorAffinity != ColorAffinity.None)
            {
                EditorGUILayout.LabelField("🎨 颜色亲和系统", EditorStyles.boldLabel);
                DrawAffinityPreview();
                EditorGUILayout.Space();
            }

            // 特殊属性
            EditorGUILayout.LabelField("🔧 特殊属性", EditorStyles.boldLabel);
            if (selectedItem.isPlaceable)
                EditorGUILayout.LabelField("✓ 可放置在场景中");
            if (selectedItem.isBreakable)
                EditorGUILayout.LabelField("✓ 可被铲子击碎");
            if (selectedItem.isComboTrigger)
                EditorGUILayout.LabelField($"✓ 组合触发器 (需要: {selectedItem.comboPartner?.itemName})");

            EditorGUILayout.EndVertical();

            // 操作按钮
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🎯 放置到场景", GUILayout.Height(30)))
            {
                PlaceItemInScene(selectedItem);
            }

            if (GUILayout.Button("✏️ 编辑道具", GUILayout.Height(30)))
            {
                Selection.activeObject = selectedItem;
            }

            if (GUILayout.Button("📋 复制道具", GUILayout.Height(30)))
            {
                DuplicateItem(selectedItem);
            }

            EditorGUILayout.EndHorizontal();

            // 快速放置模式
            EditorGUILayout.Space();
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = placementMode ? Color.green : Color.white;
            if (GUILayout.Button(placementMode ? "🎯 放置模式开启中 (点击场景放置)" : "🎯 开启快速放置模式", GUILayout.Height(25)))
            {
                placementMode = !placementMode;
                itemToPlace = placementMode ? selectedItem : null;
            }
            GUI.backgroundColor = oldColor;

            EditorGUILayout.EndVertical();
        }

        private void DrawEffectPreview()
        {
            EditorGUILayout.BeginVertical("box");

            switch (selectedItem.itemType)
            {
                case ItemType.Food:
                    EditorGUILayout.LabelField($"基础恢复: +{selectedItem.baseEffect} 体力");
                    if (selectedItem.colorAffinity != ColorAffinity.None)
                    {
                        EditorGUILayout.LabelField($"异色加成: +{selectedItem.affinityBonus} 体力");
                    }
                    break;

                case ItemType.Poison:
                    EditorGUILayout.LabelField($"伤害: -{selectedItem.effectValue} 体力",
                        new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
                    break;

                case ItemType.RocketBoost:
                    EditorGUILayout.LabelField($"推进时间: {selectedItem.effectDuration} 秒");
                    break;

                case ItemType.Trap_Nail:
                    EditorGUILayout.LabelField($"拦截时间: {selectedItem.effectDuration} 秒");
                    break;

                case ItemType.Trap_StickyPad:
                    EditorGUILayout.LabelField($"减速比例: {selectedItem.effectValue * 100}%");
                    break;

                default:
                    EditorGUILayout.LabelField(selectedItem.description);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAffinityPreview()
        {
            EditorGUILayout.BeginVertical("box");

            string affinityColor = selectedItem.colorAffinity == ColorAffinity.Green ? "绿色" : "红色";
            EditorGUILayout.LabelField($"这是一个 {affinityColor} 道具");
            EditorGUILayout.Space();

            // 效果对比表
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("🟢 绿熊吃了:", EditorStyles.boldLabel);
            float greenEffect = selectedItem.GetEffectForPlayer(0);
            EditorGUILayout.LabelField($"体力 +{greenEffect}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("🔴 红熊吃了:", EditorStyles.boldLabel);
            float redEffect = selectedItem.GetEffectForPlayer(1);
            EditorGUILayout.LabelField($"体力 +{redEffect}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // Aha 提示
            EditorGUILayout.Space();
            string betterFor = selectedItem.colorAffinity == ColorAffinity.Green ? "红熊" : "绿熊";
            EditorGUILayout.HelpBox($"💡 Aha: {betterFor}吃这个效果更好！鼓励玩家分享。", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawBottomToolbar()
        {
            EditorGUILayout.BeginHorizontal("toolbar");

            if (GUILayout.Button("刷新列表", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                RefreshItemList();
            }

            if (GUILayout.Button("创建所有预设", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ItemSetupEditor.CreateAllPresetItems();
                RefreshItemList();
            }

            if (GUILayout.Button("打开文档", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ItemSetupEditor.OpenDesignDocument();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"共 {allItems.Count} 个道具", GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        private void PlaceItemInScene(ItemData item)
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

            // 放置在场景视图中心
            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                Vector3 pos = cam.transform.position + cam.transform.forward * 5f;
                pos.z = 0;
                pickup.transform.position = pos;
            }

            Undo.RegisterCreatedObjectUndo(pickup, "Place Item");
            Selection.activeGameObject = pickup;

            Debug.Log($"[道具浏览器] 放置了 {item.itemName}");
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

        private void DuplicateItem(ItemData item)
        {
            string path = AssetDatabase.GetAssetPath(item);
            string newPath = path.Replace(".asset", "_Copy.asset");
            AssetDatabase.CopyAsset(path, newPath);
            AssetDatabase.Refresh();
            RefreshItemList();

            var newItem = AssetDatabase.LoadAssetAtPath<ItemData>(newPath);
            Selection.activeObject = newItem;

            Debug.Log($"[道具浏览器] 复制了 {item.itemName}");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!placementMode || itemToPlace == null) return;

            // 显示放置预览
            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            Vector3 placePos = ray.origin + ray.direction * 10f;
            placePos.z = 0;

            // 绘制预览
            Handles.color = itemToPlace.itemColor;
            Handles.DrawWireDisc(placePos, Vector3.forward, 0.5f);
            Handles.Label(placePos + Vector3.up, $"放置: {itemToPlace.itemName}");

            // 点击放置
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                GameObject pickup = new GameObject($"Pickup_{itemToPlace.itemName}");

                // 添加必要组件
                var sr = pickup.AddComponent<SpriteRenderer>();
                var col = pickup.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
                col.isTrigger = true;

                var pickupComp = pickup.AddComponent<ItemPickup>();
                pickupComp.itemData = itemToPlace;

                // 设置视觉效果
                sr.sprite = CreateItemSprite(itemToPlace.shape);
                sr.color = itemToPlace.itemColor;
                sr.sortingOrder = 10;
                pickup.transform.localScale = Vector3.one * 0.8f;
                pickup.transform.position = placePos;

                Undo.RegisterCreatedObjectUndo(pickup, "Quick Place Item");
                e.Use();

                Debug.Log($"[道具浏览器] 快速放置了 {itemToPlace.itemName} 在 {placePos}");
            }

            // ESC 退出放置模式
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                placementMode = false;
                itemToPlace = null;
                Repaint();
            }

            sceneView.Repaint();
        }
    }
}
#endif
