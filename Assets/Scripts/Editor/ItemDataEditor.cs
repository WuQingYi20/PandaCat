#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Item;

namespace BearCar.Editor
{
    /// <summary>
    /// ItemData 自定义 Inspector - 显示更直观的道具信息
    /// </summary>
    [CustomEditor(typeof(ItemData))]
    public class ItemDataEditor : UnityEditor.Editor
    {
        private ItemData item;
        private bool showPreview = true;
        private bool showEffectCalc = true;

        private SerializedProperty itemName;
        private SerializedProperty description;
        private SerializedProperty rarity;
        private SerializedProperty icon;
        private SerializedProperty itemColor;
        private SerializedProperty shape;
        private SerializedProperty itemType;
        private SerializedProperty targetType;
        private SerializedProperty colorAffinity;
        private SerializedProperty baseEffect;
        private SerializedProperty affinityBonus;
        private SerializedProperty effectValue;
        private SerializedProperty effectDuration;
        private SerializedProperty isPlaceable;
        private SerializedProperty isBreakable;
        private SerializedProperty breakRewards;
        private SerializedProperty isComboTrigger;
        private SerializedProperty comboPartner;
        private SerializedProperty comboResultType;
        private SerializedProperty stackable;
        private SerializedProperty maxStack;
        private SerializedProperty pickupSound;
        private SerializedProperty useSound;

        private void OnEnable()
        {
            item = (ItemData)target;

            itemName = serializedObject.FindProperty("itemName");
            description = serializedObject.FindProperty("description");
            rarity = serializedObject.FindProperty("rarity");
            icon = serializedObject.FindProperty("icon");
            itemColor = serializedObject.FindProperty("itemColor");
            shape = serializedObject.FindProperty("shape");
            itemType = serializedObject.FindProperty("itemType");
            targetType = serializedObject.FindProperty("targetType");
            colorAffinity = serializedObject.FindProperty("colorAffinity");
            baseEffect = serializedObject.FindProperty("baseEffect");
            affinityBonus = serializedObject.FindProperty("affinityBonus");
            effectValue = serializedObject.FindProperty("effectValue");
            effectDuration = serializedObject.FindProperty("effectDuration");
            isPlaceable = serializedObject.FindProperty("isPlaceable");
            isBreakable = serializedObject.FindProperty("isBreakable");
            breakRewards = serializedObject.FindProperty("breakRewards");
            isComboTrigger = serializedObject.FindProperty("isComboTrigger");
            comboPartner = serializedObject.FindProperty("comboPartner");
            comboResultType = serializedObject.FindProperty("comboResultType");
            stackable = serializedObject.FindProperty("stackable");
            maxStack = serializedObject.FindProperty("maxStack");
            pickupSound = serializedObject.FindProperty("pickupSound");
            useSound = serializedObject.FindProperty("useSound");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 顶部预览卡片
            DrawPreviewCard();

            EditorGUILayout.Space(10);

            // 基础信息
            DrawSection("📝 基础信息", () =>
            {
                EditorGUILayout.PropertyField(itemName, new GUIContent("道具名称"));
                EditorGUILayout.PropertyField(description, new GUIContent("描述"));
                EditorGUILayout.PropertyField(rarity, new GUIContent("稀有度"));
            });

            // 视觉设置
            DrawSection("🎨 视觉设置", () =>
            {
                EditorGUILayout.PropertyField(icon, new GUIContent("图标 (可选)"));
                EditorGUILayout.PropertyField(itemColor, new GUIContent("道具颜色"));
                EditorGUILayout.PropertyField(shape, new GUIContent("形状"));
            });

            // 类型与目标
            DrawSection("🎯 类型与目标", () =>
            {
                EditorGUILayout.PropertyField(itemType, new GUIContent("道具类型"));
                EditorGUILayout.PropertyField(targetType, new GUIContent("作用对象"));
            });

            // 颜色亲和系统
            DrawSection("💚❤️ 颜色亲和系统", () =>
            {
                EditorGUILayout.PropertyField(colorAffinity, new GUIContent("颜色亲和"));

                if (item.colorAffinity != ColorAffinity.None)
                {
                    EditorGUILayout.PropertyField(baseEffect, new GUIContent("基础效果"));
                    EditorGUILayout.PropertyField(affinityBonus, new GUIContent("异色加成"));

                    // 效果计算器
                    showEffectCalc = EditorGUILayout.Foldout(showEffectCalc, "📊 效果计算器", true);
                    if (showEffectCalc)
                    {
                        EditorGUILayout.BeginVertical("helpbox");
                        DrawEffectCalculator();
                        EditorGUILayout.EndVertical();
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(baseEffect, new GUIContent("效果值"));
                }
            });

            // 效果参数
            DrawSection("⚡ 效果参数", () =>
            {
                EditorGUILayout.PropertyField(effectValue, new GUIContent("效果数值"));
                EditorGUILayout.PropertyField(effectDuration, new GUIContent("持续时间"));
            });

            // 特殊属性
            DrawSection("🔧 特殊属性", () =>
            {
                EditorGUILayout.PropertyField(isPlaceable, new GUIContent("可放置"));
                EditorGUILayout.PropertyField(isBreakable, new GUIContent("可击碎"));

                if (item.isBreakable)
                {
                    EditorGUILayout.PropertyField(breakRewards, new GUIContent("击碎掉落"), true);
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(isComboTrigger, new GUIContent("组合触发器"));

                if (item.isComboTrigger)
                {
                    EditorGUILayout.PropertyField(comboPartner, new GUIContent("组合伙伴"));
                    EditorGUILayout.PropertyField(comboResultType, new GUIContent("组合效果"));
                }
            });

            // 堆叠设置
            DrawSection("📦 堆叠设置", () =>
            {
                EditorGUILayout.PropertyField(stackable, new GUIContent("可堆叠"));
                if (item.stackable)
                {
                    EditorGUILayout.PropertyField(maxStack, new GUIContent("最大堆叠"));
                }
            });

            // 音效
            DrawSection("🔊 音效", () =>
            {
                EditorGUILayout.PropertyField(pickupSound, new GUIContent("拾取音效"));
                EditorGUILayout.PropertyField(useSound, new GUIContent("使用音效"));
            });

            // 快捷操作
            EditorGUILayout.Space(10);
            DrawQuickActions();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewCard()
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "👁️ 道具预览", true);
            if (!showPreview) return;

            EditorGUILayout.BeginVertical("helpbox");

            EditorGUILayout.BeginHorizontal();

            // 颜色预览方块
            Rect colorRect = GUILayoutUtility.GetRect(60, 60, GUILayout.Width(60));
            EditorGUI.DrawRect(colorRect, item.itemColor);

            // 边框
            Handles.color = GetRarityColor(item.rarity);
            Handles.DrawSolidRectangleWithOutline(colorRect, Color.clear, GetRarityColor(item.rarity));

            EditorGUILayout.BeginVertical();

            // 名称和稀有度
            EditorGUILayout.LabelField(item.itemName, EditorStyles.boldLabel);

            // 类型标签
            string typeLabel = GetTypeLabel();
            EditorGUILayout.LabelField(typeLabel, EditorStyles.miniLabel);

            // 亲和标签
            if (item.colorAffinity != ColorAffinity.None)
            {
                string affinityLabel = item.colorAffinity == ColorAffinity.Green ? "🟢 绿色亲和" : "🔴 红色亲和";
                EditorGUILayout.LabelField(affinityLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // 描述
            EditorGUILayout.LabelField(item.description, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawSection(string title, System.Action content)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            content();
            EditorGUILayout.EndVertical();
        }

        private void DrawEffectCalculator()
        {
            EditorGUILayout.LabelField("当绿熊使用:", EditorStyles.boldLabel);
            float greenEffect = item.GetEffectForPlayer(0);
            string greenLabel = item.colorAffinity == ColorAffinity.Green
                ? $"体力 +{greenEffect} (同色，基础值)"
                : $"体力 +{greenEffect} (异色，获得加成!)";
            EditorGUILayout.LabelField($"  → {greenLabel}");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("当红熊使用:", EditorStyles.boldLabel);
            float redEffect = item.GetEffectForPlayer(1);
            string redLabel = item.colorAffinity == ColorAffinity.Red
                ? $"体力 +{redEffect} (同色，基础值)"
                : $"体力 +{redEffect} (异色，获得加成!)";
            EditorGUILayout.LabelField($"  → {redLabel}");

            EditorGUILayout.Space();

            // Aha 提示
            string betterFor = item.colorAffinity == ColorAffinity.Green ? "红熊" : "绿熊";
            EditorGUILayout.HelpBox($"💡 设计意图: 这个道具对{betterFor}更有利，鼓励玩家之间分享！", MessageType.Info);
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("⚡ 快捷操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("放置到场景"))
            {
                PlaceInScene();
            }

            if (GUILayout.Button("在浏览器中查看"))
            {
                ItemBrowserWindow.ShowWindow();
            }

            if (GUILayout.Button("复制道具"))
            {
                DuplicateItem();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void PlaceInScene()
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

        private void DuplicateItem()
        {
            string path = AssetDatabase.GetAssetPath(item);
            string newPath = path.Replace(".asset", "_Copy.asset");
            AssetDatabase.CopyAsset(path, newPath);
            AssetDatabase.Refresh();

            var newItem = AssetDatabase.LoadAssetAtPath<ItemData>(newPath);
            Selection.activeObject = newItem;
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.gray;
                case ItemRarity.Rare: return new Color(0.2f, 0.5f, 1f);
                case ItemRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
                case ItemRarity.Legendary: return new Color(1f, 0.6f, 0.1f);
                default: return Color.white;
            }
        }

        private string GetTypeLabel()
        {
            switch (item.itemType)
            {
                case ItemType.Food: return "🍎 食物";
                case ItemType.Poison: return "☠️ 毒物";
                case ItemType.RocketBoost: return "🚀 推进";
                case ItemType.SpeedBoost: return "⚡ 加速";
                case ItemType.Trap_Nail: return "📍 陷阱";
                case ItemType.Trap_PermanentNail: return "📍 永久陷阱";
                case ItemType.Trap_StickyPad: return "🐭 粘鼠板";
                case ItemType.Tool_Shovel: return "🔧 铲子";
                case ItemType.Tool_SoulNet: return "👻 灵魂网";
                case ItemType.Special_GiftBox: return "🎁 礼物盒";
                case ItemType.Special_PuddingPad: return "🍮 布丁垫";
                case ItemType.Obstacle_Rock: return "🪨 石头";
                default: return item.itemType.ToString();
            }
        }
    }
}
#endif
