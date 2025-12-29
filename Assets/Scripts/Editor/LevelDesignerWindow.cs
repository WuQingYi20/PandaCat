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
    /// 关卡设计器主窗口 - 设计师友好的统一工具
    /// 快捷键: Shift+Alt+L
    /// </summary>
    public class LevelDesignerWindow : EditorWindow
    {
        // 标签页
        private int currentTab = 0;
        private string[] tabNames = { "道具", "传送门", "Aha模板", "验证" };

        // 道具相关
        private ItemData[] allItems;
        private Vector2 itemScrollPos;
        private int itemCategory = 0;
        private string[] itemCategories = { "全部", "绿色", "红色", "中性", "工具", "特殊" };

        // 模板相关
        private Vector2 templateScrollPos;
        private AhaTemplate selectedTemplate;
        private int selectedCategory = 0; // 0=全部, 1-5=具体类别
        private string[] categoryNames = { "全部", "🔍发现", "🤝同步", "💚牺牲", "🆘救援", "🎯优化" };

        // 验证相关
        private Vector2 validationScrollPos;
        private List<ValidationResult> validationResults = new List<ValidationResult>();

        // Scene View 可视化
        private static bool showConnections = true;
        private static bool showCoopZones = true;

        [MenuItem("BearCar/关卡设计/关卡设计器 #&L")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelDesignerWindow>("关卡设计器");
            window.minSize = new Vector2(400, 500);
            window.LoadData();
        }

        private void OnEnable()
        {
            LoadData();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void LoadData()
        {
            // 加载道具
            var items = new List<ItemData>();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Resources/Items" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null) items.Add(item);
            }
            allItems = items.ToArray();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();

            EditorGUILayout.Space(10);

            switch (currentTab)
            {
                case 0: DrawItemsTab(); break;
                case 1: DrawPortalsTab(); break;
                case 2: DrawAhaTemplatesTab(); break;
                case 3: DrawValidationTab(); break;
            }

            DrawFooter();
        }

        #region Header & Footer

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("🎮 Bear Car 关卡设计器", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 场景统计
            var pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
            var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);

            int greenCount = pickups.Count(p => p.itemData?.colorAffinity == ColorAffinity.Green);
            int redCount = pickups.Count(p => p.itemData?.colorAffinity == ColorAffinity.Red);

            GUILayout.Label($"🟢{greenCount} 🔴{redCount} 🌀{portals.Length / 2}对");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(30));
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            showConnections = GUILayout.Toggle(showConnections, "显示连接", GUILayout.Width(80));
            showCoopZones = GUILayout.Toggle(showCoopZones, "显示合作区", GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                LoadData();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 道具标签页

        private void DrawItemsTab()
        {
            // 分类选择
            itemCategory = GUILayout.SelectionGrid(itemCategory, itemCategories, 3);
            EditorGUILayout.Space(5);

            // 道具列表
            itemScrollPos = EditorGUILayout.BeginScrollView(itemScrollPos);

            if (allItems == null || allItems.Length == 0)
            {
                EditorGUILayout.HelpBox("没有找到道具资源", MessageType.Warning);
                if (GUILayout.Button("创建预设道具"))
                {
                    ItemSetupEditor.CreateAllPresetItems();
                    LoadData();
                }
            }
            else
            {
                foreach (var item in allItems)
                {
                    if (!MatchesItemCategory(item)) continue;
                    DrawItemRow(item);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool MatchesItemCategory(ItemData item)
        {
            switch (itemCategory)
            {
                case 0: return true;
                case 1: return item.colorAffinity == ColorAffinity.Green;
                case 2: return item.colorAffinity == ColorAffinity.Red;
                case 3: return item.colorAffinity == ColorAffinity.None &&
                              (item.itemType == ItemType.Food || item.itemType == ItemType.Poison);
                case 4: return item.itemType == ItemType.Tool_Shovel || item.itemType == ItemType.Tool_SoulNet;
                case 5: return item.itemType == ItemType.Special_GiftBox ||
                              item.itemType == ItemType.Special_PuddingPad ||
                              item.itemType == ItemType.RocketBoost;
                default: return true;
            }
        }

        private void DrawItemRow(ItemData item)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 颜色块
            Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            EditorGUI.DrawRect(colorRect, item.itemColor);

            // 名称
            string emoji = GetItemEmoji(item);
            EditorGUILayout.LabelField($"{emoji} {item.itemName}", GUILayout.ExpandWidth(true));

            // 放置按钮
            if (GUILayout.Button("放置", GUILayout.Width(50)))
            {
                PlaceItemInScene(item);
            }

            EditorGUILayout.EndHorizontal();
        }

        private string GetItemEmoji(ItemData item)
        {
            if (item.colorAffinity == ColorAffinity.Green) return "🟢";
            if (item.colorAffinity == ColorAffinity.Red) return "🔴";

            switch (item.itemType)
            {
                case ItemType.Poison: return "☠️";
                case ItemType.Tool_Shovel: return "🔧";
                case ItemType.Tool_SoulNet: return "👻";
                case ItemType.Special_GiftBox: return "🎁";
                case ItemType.RocketBoost: return "🚀";
                default: return "⚪";
            }
        }

        private void PlaceItemInScene(ItemData item)
        {
            GameObject pickup = new GameObject($"Pickup_{item.itemName}");

            var sr = pickup.AddComponent<SpriteRenderer>();
            var col = pickup.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            var pickupComp = pickup.AddComponent<ItemPickup>();
            pickupComp.itemData = item;

            sr.sprite = CreateItemSprite(item.shape);
            sr.color = item.itemColor;
            sr.sortingOrder = 10;
            pickup.transform.localScale = Vector3.one * 0.8f;

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

        #endregion

        #region 传送门标签页

        private void DrawPortalsTab()
        {
            EditorGUILayout.LabelField("🌀 快速创建传送门", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 快速创建按钮
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("🌀 创建传送门对", GUILayout.Height(30)))
            {
                CreatePortalPair();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("⚡ 创建合作传送门 (需要2人)", GUILayout.Height(30)))
            {
                CreateCoopPortal();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("🚀 创建加速传送门 (2x动量)", GUILayout.Height(30)))
            {
                CreateSpeedBoostPortal();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("➡️ 创建单向传送门", GUILayout.Height(30)))
            {
                CreateOneWayPortal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 场景中的传送门列表
            EditorGUILayout.LabelField("📋 场景传送门", EditorStyles.boldLabel);

            var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            if (portals.Length == 0)
            {
                EditorGUILayout.HelpBox("场景中没有传送门", MessageType.Info);
            }
            else
            {
                foreach (var portal in portals)
                {
                    DrawPortalRow(portal);
                }
            }
        }

        private void DrawPortalRow(Portal portal)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 颜色指示
            Rect colorRect = GUILayoutUtility.GetRect(10, 18, GUILayout.Width(10));
            EditorGUI.DrawRect(colorRect, portal.portalColor);

            // 名称和状态
            string status = portal.activation == PortalActivation.CoopTrigger
                ? $"[合作 x{portal.requiredPlayers}]"
                : "";
            EditorGUILayout.LabelField($"{portal.portalId} {status}", GUILayout.ExpandWidth(true));

            // 选择按钮
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                Selection.activeGameObject = portal.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreatePortalPair()
        {
            var parent = new GameObject("PortalPair");

            var portalA = CreatePortal("Portal_A", new Vector3(-3, 0, 0), new Color(0.3f, 0.6f, 1f));
            var portalB = CreatePortal("Portal_B", new Vector3(3, 0, 0), new Color(1f, 0.6f, 0.3f));

            portalA.transform.parent = parent.transform;
            portalB.transform.parent = parent.transform;

            portalA.GetComponent<Portal>().targetPoint = portalB.transform;
            portalB.GetComponent<Portal>().targetPoint = portalA.transform;

            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                Vector3 pos = cam.transform.position + cam.transform.forward * 5f;
                pos.z = 0;
                parent.transform.position = pos;
            }

            Undo.RegisterCreatedObjectUndo(parent, "Create Portal Pair");
            Selection.activeGameObject = parent;
        }

        private void CreateCoopPortal()
        {
            var parent = new GameObject("CoopPortalSetup");

            // 创建压力板
            var plateA = CreatePressurePlate("PressurePlate_A", new Vector3(-2, -1, 0));
            var plateB = CreatePressurePlate("PressurePlate_B", new Vector3(2, -1, 0));

            // 创建传送门
            var portal = CreatePortal("CoopPortal", new Vector3(0, 1, 0), new Color(0.8f, 0.3f, 1f));
            var exit = CreatePortal("CoopPortal_Exit", new Vector3(0, 5, 0), new Color(0.5f, 0.8f, 1f));

            var portalComp = portal.GetComponent<Portal>();
            portalComp.activation = PortalActivation.CoopTrigger;
            portalComp.requiredPlayers = 2;
            portalComp.activationDelay = 1f;
            portalComp.targetPoint = exit.transform;

            exit.GetComponent<Portal>().direction = PortalDirection.OneWayOut;

            // 设置层级
            plateA.transform.parent = parent.transform;
            plateB.transform.parent = parent.transform;
            portal.transform.parent = parent.transform;
            exit.transform.parent = parent.transform;

            // 链接压力板到传送门
            var plateAComp = plateA.GetComponent<PressurePlate>();
            var plateBComp = plateB.GetComponent<PressurePlate>();
            if (plateAComp != null) plateAComp.linkedObjects = new GameObject[] { portal };
            if (plateBComp != null) plateBComp.linkedObjects = new GameObject[] { portal };

            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                Vector3 pos = cam.transform.position + cam.transform.forward * 5f;
                pos.z = 0;
                parent.transform.position = pos;
            }

            Undo.RegisterCreatedObjectUndo(parent, "Create Coop Portal");
            Selection.activeGameObject = parent;
        }

        private void CreateSpeedBoostPortal()
        {
            var parent = new GameObject("SpeedBoostPortal");

            var portalA = CreatePortal("SpeedPortal_In", new Vector3(-2, 0, 0), new Color(1f, 0.8f, 0.2f));
            var portalB = CreatePortal("SpeedPortal_Out", new Vector3(2, 0, 0), new Color(1f, 0.5f, 0.1f));

            portalA.transform.parent = parent.transform;
            portalB.transform.parent = parent.transform;

            var compA = portalA.GetComponent<Portal>();
            compA.targetPoint = portalB.transform;
            compA.preserveMomentum = true;
            compA.momentumMultiplier = 2f;
            compA.momentumDirection = MomentumDirection.TowardsTarget;

            portalB.GetComponent<Portal>().direction = PortalDirection.OneWayOut;

            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                Vector3 pos = cam.transform.position + cam.transform.forward * 5f;
                pos.z = 0;
                parent.transform.position = pos;
            }

            Undo.RegisterCreatedObjectUndo(parent, "Create Speed Boost Portal");
            Selection.activeGameObject = parent;
        }

        private void CreateOneWayPortal()
        {
            var parent = new GameObject("OneWayPortal");

            var entrance = CreatePortal("OneWay_In", new Vector3(-2, 0, 0), new Color(0.3f, 0.9f, 0.4f));
            var exit = CreatePortal("OneWay_Out", new Vector3(2, 0, 0), new Color(0.9f, 0.3f, 0.3f));

            entrance.transform.parent = parent.transform;
            exit.transform.parent = parent.transform;

            entrance.GetComponent<Portal>().targetPoint = exit.transform;
            exit.GetComponent<Portal>().direction = PortalDirection.OneWayOut;

            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                Vector3 pos = cam.transform.position + cam.transform.forward * 5f;
                pos.z = 0;
                parent.transform.position = pos;
            }

            Undo.RegisterCreatedObjectUndo(parent, "Create One Way Portal");
            Selection.activeGameObject = parent;
        }

        private GameObject CreatePortal(string id, Vector3 localPos, Color color)
        {
            var go = new GameObject(id);
            go.transform.localPosition = localPos;

            var portal = go.AddComponent<Portal>();
            portal.portalId = id;
            portal.portalColor = color;

            return go;
        }

        private GameObject CreatePressurePlate(string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.localPosition = localPos;

            var plate = go.AddComponent<PressurePlate>();

            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.6f, 0.6f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 0.3f);
            col.isTrigger = true;

            return go;
        }

        #endregion

        #region Aha模板标签页

        private void DrawAhaTemplatesTab()
        {
            EditorGUILayout.LabelField("✨ Aha Moment 模板", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("一键放置预设的合作谜题布局，快速创造 Aha Moment！共12个模板，覆盖5种合作类型。", MessageType.Info);

            EditorGUILayout.Space(5);

            // 类别筛选
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("类别筛选:", GUILayout.Width(60));
            selectedCategory = GUILayout.SelectionGrid(selectedCategory, categoryNames, 6);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            templateScrollPos = EditorGUILayout.BeginScrollView(templateScrollPos);

            // 遍历所有12个内置模板
            var allTemplates = AhaTemplates.AllTemplates;
            int displayedCount = 0;

            foreach (var template in allTemplates)
            {
                // 类别筛选
                if (selectedCategory != 0)
                {
                    AhaCategory targetCategory = (AhaCategory)(selectedCategory - 1);
                    if (template.category != targetCategory)
                        continue;
                }

                DrawTemplateCard(template);
                displayedCount++;
            }

            if (displayedCount == 0)
            {
                EditorGUILayout.HelpBox("当前类别没有模板", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // 自定义模板区域
            EditorGUILayout.LabelField("📁 自定义模板", EditorStyles.boldLabel);
            DrawCustomTemplates();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 底部按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("💾 保存选中对象为模板", GUILayout.Height(25)))
            {
                SaveSelectionAsTemplate();
            }

            if (GUILayout.Button("📖 设计原则指南", GUILayout.Height(25)))
            {
                CoopDesignPrinciples.ShowPrinciplesWindow();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTemplateCard(AhaTemplate template)
        {
            EditorGUILayout.BeginVertical("box");

            // 第一行：名称 + 类别标签 + 难度
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{template.emoji} {template.name}", EditorStyles.boldLabel, GUILayout.Width(120));

            // 类别标签（带颜色）
            var categoryStyle = new GUIStyle(EditorStyles.miniLabel);
            categoryStyle.fontStyle = FontStyle.Bold;
            Color categoryColor = GetCategoryColor(template.category);
            categoryStyle.normal.textColor = categoryColor;
            string categoryName = GetCategoryDisplayName(template.category);
            GUILayout.Label($"[{categoryName}]", categoryStyle, GUILayout.Width(60));

            GUILayout.FlexibleSpace();
            GUILayout.Label($"难度: {template.difficulty}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 描述
            EditorGUILayout.LabelField(template.description, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(3);

            // Aha 说明
            var ahaStyle = new GUIStyle(EditorStyles.miniLabel);
            ahaStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);
            EditorGUILayout.LabelField($"💡 Aha: \"{template.ahaHint}\"", ahaStyle);

            // 设计原理（较淡的颜色）
            if (!string.IsNullOrEmpty(template.designRationale))
            {
                var rationaleStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
                rationaleStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                EditorGUILayout.LabelField($"📐 原理: {template.designRationale}", rationaleStyle);
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("放置到场景"))
            {
                PlaceTemplate(template);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private Color GetCategoryColor(AhaCategory category)
        {
            return category switch
            {
                AhaCategory.Discovery => new Color(0.3f, 0.8f, 1f),      // 蓝色 - 发现
                AhaCategory.Synchronization => new Color(0.3f, 1f, 0.5f), // 绿色 - 同步
                AhaCategory.Sacrifice => new Color(1f, 0.5f, 0.8f),      // 粉色 - 牺牲
                AhaCategory.Rescue => new Color(1f, 0.6f, 0.2f),         // 橙色 - 救援
                AhaCategory.Optimization => new Color(0.9f, 0.9f, 0.3f), // 黄色 - 优化
                _ => Color.white
            };
        }

        private string GetCategoryDisplayName(AhaCategory category)
        {
            return category switch
            {
                AhaCategory.Discovery => "发现",
                AhaCategory.Synchronization => "同步",
                AhaCategory.Sacrifice => "牺牲",
                AhaCategory.Rescue => "救援",
                AhaCategory.Optimization => "优化",
                _ => "未知"
            };
        }

        private void PlaceTemplate(AhaTemplate template)
        {
            Vector3 basePos = Vector3.zero;
            if (SceneView.lastActiveSceneView != null)
            {
                var cam = SceneView.lastActiveSceneView.camera;
                basePos = cam.transform.position + cam.transform.forward * 5f;
                basePos.z = 0;
            }

            var parent = template.CreateInScene(basePos);
            Undo.RegisterCreatedObjectUndo(parent, $"Place Template: {template.name}");
            Selection.activeGameObject = parent;

            Debug.Log($"[LevelDesigner] 放置了 Aha 模板: {template.name}");
        }

        private void DrawCustomTemplates()
        {
            string[] guids = AssetDatabase.FindAssets("t:AhaTemplateAsset", new[] { "Assets/Resources/AhaTemplates" });

            if (guids.Length == 0)
            {
                EditorGUILayout.HelpBox("暂无自定义模板\n选择场景中的对象后点击\"保存为模板\"", MessageType.Info);
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AhaTemplateAsset>(path);
                if (asset != null)
                {
                    DrawCustomTemplateRow(asset);
                }
            }
        }

        private void DrawCustomTemplateRow(AhaTemplateAsset asset)
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"📄 {asset.templateName}");

            if (GUILayout.Button("放置", GUILayout.Width(50)))
            {
                // 实例化 prefab
                if (asset.prefab != null)
                {
                    var instance = PrefabUtility.InstantiatePrefab(asset.prefab) as GameObject;
                    if (SceneView.lastActiveSceneView != null)
                    {
                        var cam = SceneView.lastActiveSceneView.camera;
                        Vector3 pos = cam.transform.position + cam.transform.forward * 5f;
                        pos.z = 0;
                        instance.transform.position = pos;
                    }
                    Undo.RegisterCreatedObjectUndo(instance, "Place Custom Template");
                    Selection.activeGameObject = instance;
                }
            }

            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("删除模板", $"确定要删除模板 \"{asset.templateName}\"?", "删除", "取消"))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SaveSelectionAsTemplate()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("保存模板", "请先在场景中选择要保存的对象", "确定");
                return;
            }

            // 创建模板保存窗口
            SaveTemplateWindow.ShowWindow(Selection.gameObjects);
        }

        #endregion

        #region 验证标签页

        private void DrawValidationTab()
        {
            EditorGUILayout.LabelField("✅ 关卡验证", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("全面检查关卡质量：颜色平衡、可达性、难度曲线、Aha密度", MessageType.Info);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("🔍 运行完整验证", GUILayout.Height(35)))
            {
                RunValidation();
            }

            EditorGUILayout.Space(10);

            if (validationResults.Count == 0)
            {
                EditorGUILayout.HelpBox("点击\"运行完整验证\"检查关卡问题", MessageType.Info);
                return;
            }

            // 统计摘要
            DrawValidationSummary();

            EditorGUILayout.Space(5);

            validationScrollPos = EditorGUILayout.BeginScrollView(validationScrollPos);

            // 分类显示结果
            var errors = validationResults.Where(r => r.type == ValidationType.Error).ToList();
            var warnings = validationResults.Where(r => r.type == ValidationType.Warning).ToList();
            var suggestions = validationResults.Where(r => r.type == ValidationType.Suggestion).ToList();
            var passed = validationResults.Where(r => r.type == ValidationType.Pass).ToList();

            if (errors.Count > 0)
            {
                DrawValidationSection("❌ 错误", errors, new Color(1f, 0.3f, 0.3f));
            }

            if (warnings.Count > 0)
            {
                DrawValidationSection("⚠️ 警告", warnings, new Color(1f, 0.8f, 0.2f));
            }

            if (passed.Count > 0)
            {
                DrawValidationSection("✅ 通过", passed, new Color(0.3f, 0.9f, 0.4f));
            }

            if (suggestions.Count > 0)
            {
                DrawValidationSection("💡 建议", suggestions, new Color(0.5f, 0.8f, 1f));
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawValidationSummary()
        {
            var errors = validationResults.Count(r => r.type == ValidationType.Error);
            var warnings = validationResults.Count(r => r.type == ValidationType.Warning);
            var passed = validationResults.Count(r => r.type == ValidationType.Pass);
            var suggestions = validationResults.Count(r => r.type == ValidationType.Suggestion);

            EditorGUILayout.BeginHorizontal("box");

            // 根据结果显示不同颜色的摘要
            Color summaryColor = errors > 0 ? new Color(1f, 0.5f, 0.5f) :
                                 warnings > 0 ? new Color(1f, 0.9f, 0.6f) :
                                 new Color(0.6f, 1f, 0.6f);

            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = summaryColor;

            string status = errors > 0 ? "需要修复" : warnings > 0 ? "有待改进" : "良好";
            GUILayout.Label($"状态: {status}", style);

            GUILayout.FlexibleSpace();
            GUILayout.Label($"❌{errors} ⚠️{warnings} ✅{passed} 💡{suggestions}", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationSection(string title, List<ValidationResult> results, Color titleColor)
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = titleColor;
            EditorGUILayout.LabelField(title, titleStyle);

            foreach (var r in results)
            {
                DrawValidationResult(r);
            }

            EditorGUILayout.Space(5);
        }

        private void DrawValidationResult(ValidationResult result)
        {
            EditorGUILayout.BeginVertical("box");

            // 第一行：类别 + 消息
            EditorGUILayout.BeginHorizontal();

            string icon = result.type switch
            {
                ValidationType.Error => "❌",
                ValidationType.Warning => "⚠️",
                ValidationType.Suggestion => "💡",
                _ => "✅"
            };

            // 类别标签
            if (!string.IsNullOrEmpty(result.category))
            {
                var categoryStyle = new GUIStyle(EditorStyles.miniLabel);
                categoryStyle.fontStyle = FontStyle.Bold;
                GUILayout.Label($"[{result.category}]", categoryStyle, GUILayout.Width(80));
            }

            EditorGUILayout.LabelField($"{icon} {result.message}", EditorStyles.wordWrappedLabel);

            if (result.relatedObject != null)
            {
                if (GUILayout.Button("定位", GUILayout.Width(50)))
                {
                    Selection.activeGameObject = result.relatedObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 第二行：建议（如果有）
            if (!string.IsNullOrEmpty(result.suggestion))
            {
                var suggestionStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
                suggestionStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);
                EditorGUILayout.LabelField($"→ {result.suggestion}", suggestionStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void RunValidation()
        {
            // 使用新的综合验证器
            validationResults = LevelValidator.RunFullValidation();
            Debug.Log($"[LevelDesigner] 验证完成: {validationResults.Count} 条结果");
        }

        #endregion

        #region Scene View 可视化

        private void OnSceneGUI(SceneView sceneView)
        {
            if (showConnections)
            {
                DrawPortalConnections();
                DrawPressurePlateConnections();
            }

            if (showCoopZones)
            {
                DrawCoopZones();
            }
        }

        private void DrawPortalConnections()
        {
            var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);

            foreach (var portal in portals)
            {
                if (portal.targetPoint == null) continue;

                Vector3 start = portal.transform.position;
                Vector3 end = portal.targetPoint.position;

                // 绘制连接线
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
                Handles.DrawDottedLine(start, end, 3f);

                // 绘制箭头
                Vector3 dir = (end - start).normalized;
                Vector3 mid = (start + end) / 2f;

                Handles.color = Color.cyan;
                Handles.DrawLine(mid, mid + Quaternion.Euler(0, 0, 25) * -dir * 0.4f);
                Handles.DrawLine(mid, mid + Quaternion.Euler(0, 0, -25) * -dir * 0.4f);
            }
        }

        private void DrawPressurePlateConnections()
        {
            var plates = FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);

            foreach (var plate in plates)
            {
                if (plate.linkedObjects == null) continue;

                foreach (var linked in plate.linkedObjects)
                {
                    if (linked == null) continue;

                    Handles.color = new Color(1f, 0.8f, 0.2f, 0.6f);
                    Handles.DrawDottedLine(plate.transform.position, linked.transform.position, 2f);
                }
            }
        }

        private void DrawCoopZones()
        {
            var coopPortals = FindObjectsByType<Portal>(FindObjectsSortMode.None)
                .Where(p => p.activation == PortalActivation.CoopTrigger);

            foreach (var portal in coopPortals)
            {
                Handles.color = new Color(0.8f, 0.3f, 1f, 0.2f);
                Handles.DrawSolidDisc(portal.transform.position, Vector3.forward, 2f);

                Handles.color = new Color(0.8f, 0.3f, 1f, 0.8f);
                Handles.Label(portal.transform.position + Vector3.up * 1.5f,
                    $"需要 {portal.requiredPlayers} 人");
            }
        }

        #endregion

        #region 辅助方法

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

                    bool filled = shape switch
                    {
                        ItemShape.Circle => dist < radius,
                        ItemShape.Square => Mathf.Abs(dx) < radius * 0.7f && Mathf.Abs(dy) < radius * 0.7f,
                        ItemShape.Diamond => Mathf.Abs(dx) + Mathf.Abs(dy) < radius,
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

        #endregion
    }

    #region 验证结果类

    public enum ValidationType
    {
        Pass,
        Warning,
        Error,
        Suggestion
    }

    public class ValidationResult
    {
        public ValidationType type;
        public string category;      // 验证类别 (颜色平衡、可达性等)
        public string message;
        public string suggestion;    // 具体修复建议
        public GameObject relatedObject;
    }

    #endregion
}
#endif
