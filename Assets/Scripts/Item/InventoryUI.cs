using UnityEngine;

namespace BearCar.Item
{
    /// <summary>
    /// 共享道具栏 UI - 使用 OnGUI 绘制
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("=== 位置设置 ===")]
        [Tooltip("UI 位置")]
        public UIPosition position = UIPosition.BottomCenter;

        [Tooltip("距离边缘的偏移")]
        public Vector2 offset = new Vector2(0, 30);

        [Header("=== 槽位外观 ===")]
        [Tooltip("槽位大小")]
        public float slotSize = 50f;

        [Tooltip("槽位间距")]
        public float slotSpacing = 8f;

        [Tooltip("槽位背景颜色")]
        public Color slotColor = new Color(0, 0, 0, 0.6f);

        [Tooltip("选中槽位颜色")]
        public Color selectedColor = new Color(1f, 0.8f, 0.2f, 0.8f);

        [Tooltip("空槽位颜色")]
        public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        [Header("=== 玩家颜色 ===")]
        public Color greenBearColor = new Color(0.3f, 0.9f, 0.4f);
        public Color redBearColor = new Color(0.95f, 0.3f, 0.3f);

        [Header("=== 提示文字 ===")]
        public bool showHints = true;

            private SharedInventory inventory;
        private GUIStyle slotStyle;
        private GUIStyle countStyle;
        private GUIStyle hintStyle;
        private GUIStyle nameStyle;
        private GUIStyle playerHintStyle;
        private GUIStyle affinityStyle;
        private Texture2D slotBgTex;
        private Texture2D selectedBgTex;
        private Texture2D emptyBgTex;

        // 额外缓存的纹理，避免每帧创建
        private Texture2D hintBoxBgTex;
        private Texture2D greenColorBarTex;
        private Texture2D redColorBarTex;
        private Texture2D bothSelectedTex;
        private Texture2D greenDotTex;
        private Texture2D redDotTex;
        private Texture2D itemInfoBgTex;
        private Texture2D itemInfoBgTex2;

        // 形状纹理缓存
        private System.Collections.Generic.Dictionary<string, Texture2D> shapeTextureCache;

        private void Start()
        {
            inventory = SharedInventory.Instance;
            if (inventory == null)
            {
                // 创建共享背包
                var go = new GameObject("SharedInventory");
                inventory = go.AddComponent<SharedInventory>();
            }

            InitStyles();
            Debug.Log($"[InventoryUI] 初始化完成，槽位数: {inventory.SlotCount}");
        }

        private void Update()
        {
            // 确保inventory引用是最新的
            if (inventory == null)
            {
                inventory = SharedInventory.Instance;
            }
        }

        private void InitStyles()
        {
            // 创建纹理 - 一次性创建，避免每帧创建
            slotBgTex = MakeTexture(2, 2, slotColor);
            selectedBgTex = MakeTexture(2, 2, selectedColor);
            emptyBgTex = MakeTexture(2, 2, emptyColor);

            // 额外缓存的纹理
            hintBoxBgTex = MakeTexture(2, 2, new Color(0, 0, 0, 0.6f));
            greenColorBarTex = MakeTexture(1, 1, greenBearColor);
            redColorBarTex = MakeTexture(1, 1, redBearColor);
            bothSelectedTex = MakeTexture(2, 2, new Color(1f, 0.9f, 0.3f, 0.8f));
            greenDotTex = MakeTexture(1, 1, greenBearColor);
            redDotTex = MakeTexture(1, 1, redBearColor);
            itemInfoBgTex = MakeTexture(2, 2, new Color(0, 0, 0, 0.5f));
            itemInfoBgTex2 = MakeTexture(2, 2, new Color(0, 0, 0, 0.6f));

            // 形状纹理缓存
            shapeTextureCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

            slotStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { background = slotBgTex }
            };

            countStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = Color.white },
                padding = new RectOffset(0, 4, 0, 2)
            };

            hintStyle = new GUIStyle
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1, 1, 1, 0.6f) }
            };

            nameStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                wordWrap = true
            };

            playerHintStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 3, 3)
            };

            affinityStyle = new GUIStyle
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1, 1, 0.6f) }
            };
        }

        private void OnGUI()
        {
            if (inventory == null || inventory.Slots == null) return;

            // 确保样式已初始化
            if (slotBgTex == null)
            {
                InitStyles();
            }

            int slotCount = inventory.SlotCount;
            float totalWidth = slotCount * slotSize + (slotCount - 1) * slotSpacing;
            float totalHeight = slotSize;

            // 计算起始位置
            Vector2 startPos = CalculatePosition(totalWidth, totalHeight);

            // 绘制玩家操作提示（在道具栏两侧）
            if (showHints)
            {
                DrawPlayerHints(startPos, totalWidth, totalHeight);
            }

            // 绘制槽位
            for (int i = 0; i < slotCount; i++)
            {
                float x = startPos.x + i * (slotSize + slotSpacing);
                float y = startPos.y;
                Rect slotRect = new Rect(x, y, slotSize, slotSize);

                DrawSlot(slotRect, i);
            }

            // 绘制两个玩家选中的道具信息
            DrawDualPlayerItemInfo(startPos, totalWidth);

            // 绘制切换提示
            DrawRotateHint(startPos, totalWidth);
        }

        private void DrawPlayerHints(Vector2 startPos, float totalWidth, float totalHeight)
        {
            float hintWidth = 90f;
            float hintHeight = 50f;
            float gap = 15f;

            // 左侧：绿熊 (P1)
            Rect p1Rect = new Rect(startPos.x - hintWidth - gap, startPos.y, hintWidth, hintHeight);
            DrawPlayerHintBox(p1Rect, "P1 绿熊", "Tab 使用", greenBearColor);

            // 右侧：红熊 (P2)
            Rect p2Rect = new Rect(startPos.x + totalWidth + gap, startPos.y, hintWidth, hintHeight);
            DrawPlayerHintBox(p2Rect, "P2 红熊", "/ 使用", redBearColor);
        }

        private void DrawPlayerHintBox(Rect rect, string playerName, string actionHint, Color playerColor)
        {
            // 背景 - 使用缓存的纹理
            GUI.DrawTexture(rect, hintBoxBgTex);

            // 顶部颜色条 - 使用缓存的纹理
            Texture2D colorBar = (playerColor == greenBearColor) ? greenColorBarTex : redColorBarTex;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3), colorBar);

            // 玩家名称
            playerHintStyle.normal.textColor = playerColor;
            Rect nameRect = new Rect(rect.x, rect.y + 8, rect.width, 18);
            GUI.Label(nameRect, playerName, playerHintStyle);

            // 操作提示
            playerHintStyle.normal.textColor = Color.white;
            Rect actionRect = new Rect(rect.x, rect.y + 28, rect.width, 18);
            GUI.Label(actionRect, actionHint, playerHintStyle);
        }

        private void DrawDualPlayerItemInfo(Vector2 startPos, float totalWidth)
        {
            var greenSlot = inventory.GetPlayerCurrentSlot(0);
            var redSlot = inventory.GetPlayerCurrentSlot(1);

            float yOffset = slotSize + 6;
            float halfWidth = totalWidth / 2 - 10;

            // 绿熊选中的道具
            if (greenSlot != null && !greenSlot.IsEmpty)
            {
                Rect greenRect = new Rect(startPos.x, startPos.y + yOffset, halfWidth, 36);
                DrawPlayerItemInfo(greenRect, greenSlot.item, greenBearColor, "绿熊", 0);
            }

            // 红熊选中的道具
            if (redSlot != null && !redSlot.IsEmpty)
            {
                Rect redRect = new Rect(startPos.x + halfWidth + 20, startPos.y + yOffset, halfWidth, 36);
                DrawPlayerItemInfo(redRect, redSlot.item, redBearColor, "红熊", 1);
            }
        }

        private void DrawPlayerItemInfo(Rect rect, ItemData item, Color playerColor, string playerName, int playerIndex)
        {
            // 背景 - 使用缓存的纹理
            GUI.DrawTexture(rect, itemInfoBgTex);

            // 顶部颜色条 - 使用缓存的纹理
            Texture2D colorBar = (playerColor == greenBearColor) ? greenColorBarTex : redColorBarTex;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2), colorBar);

            // 道具名称
            var itemNameStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = playerColor }
            };
            Rect nameRect = new Rect(rect.x, rect.y + 4, rect.width, 14);
            GUI.Label(nameRect, $"{playerName}: {item.itemName}", itemNameStyle);

            // 效果值
            float effect = item.GetEffectForPlayer(playerIndex);
            string effectText = effect >= 0 ? $"+{effect}" : $"{effect}";
            var effectStyle = new GUIStyle
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1, 1, 0.6f) }
            };
            Rect effectRect = new Rect(rect.x, rect.y + 18, rect.width, 14);
            GUI.Label(effectRect, effectText, effectStyle);
        }

        private void DrawItemInfo(Vector2 startPos, float totalWidth, ItemData item)
        {
            // 道具名称
            Rect nameRect = new Rect(startPos.x, startPos.y + slotSize + 6, totalWidth, 18);
            GUI.Label(nameRect, item.itemName, nameStyle);

            // 颜色亲和提示
            if (item.colorAffinity != ColorAffinity.None)
            {
                string affinityText;
                if (item.colorAffinity == ColorAffinity.Green)
                {
                    affinityText = $"绿熊+{item.baseEffect}  红熊+{item.baseEffect + item.affinityBonus}";
                }
                else
                {
                    affinityText = $"绿熊+{item.baseEffect + item.affinityBonus}  红熊+{item.baseEffect}";
                }

                Rect affinityRect = new Rect(startPos.x, startPos.y + slotSize + 24, totalWidth, 16);
                GUI.Label(affinityRect, affinityText, affinityStyle);
            }
        }

        private void DrawRotateHint(Vector2 startPos, float totalWidth)
        {
            Rect hintRect = new Rect(startPos.x, startPos.y - 18, totalWidth, 16);
            GUI.Label(hintRect, "Q/R  或  ,/.  切换道具", hintStyle);
        }

        private void DrawSlot(Rect rect, int index)
        {
            var slot = inventory.GetSlot(index);
            bool isGreenSelected = index == inventory.GreenBearIndex;
            bool isRedSelected = index == inventory.RedBearIndex;
            bool isEmpty = slot == null || slot.IsEmpty;

            // 背景颜色 - 使用缓存的纹理
            Texture2D bgTex;
            if (isEmpty)
            {
                bgTex = emptyBgTex;
            }
            else if (isGreenSelected && isRedSelected)
            {
                // 两个玩家都选中 - 使用缓存的金色纹理
                bgTex = bothSelectedTex;
            }
            else if (isGreenSelected || isRedSelected)
            {
                bgTex = selectedBgTex;
            }
            else
            {
                bgTex = slotBgTex;
            }
            GUI.DrawTexture(rect, bgTex);

            // 选中边框 - 双层显示两个玩家
            if (isGreenSelected && isRedSelected)
            {
                // 两个玩家都选中 - 外层绿色，内层红色
                DrawBorder(rect, greenBearColor, 4);
                DrawBorder(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), redBearColor, 2);
            }
            else if (isGreenSelected)
            {
                DrawBorder(rect, greenBearColor, 3);
            }
            else if (isRedSelected)
            {
                DrawBorder(rect, redBearColor, 3);
            }

            // 道具图标
            if (!isEmpty)
            {
                DrawItemIcon(rect, slot.item);

                // 数量
                if (slot.count > 1)
                {
                    GUI.Label(rect, slot.count.ToString(), countStyle);
                }
            }
            else
            {
                // 空槽位显示序号
                var numStyle = new GUIStyle
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1, 1, 1, 0.3f) }
                };
                GUI.Label(rect, (index + 1).ToString(), numStyle);
            }

            // 玩家选中标记（角落小圆点）- 使用缓存的纹理
            if (isGreenSelected)
            {
                Rect greenDotRect = new Rect(rect.x + 2, rect.y + 2, 8, 8);
                GUI.DrawTexture(greenDotRect, greenDotTex);
            }
            if (isRedSelected)
            {
                Rect redDotRect = new Rect(rect.xMax - 10, rect.y + 2, 8, 8);
                GUI.DrawTexture(redDotRect, redDotTex);
            }
        }

        private void DrawItemIcon(Rect rect, ItemData item)
        {
            if (item == null) return;

            // 缩小区域留出边距
            float padding = 8;
            Rect iconRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2,
                rect.height - padding * 2
            );

            if (item.icon != null)
            {
                // 使用图标
                GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                // 绘制形状
                Texture2D shapeTex = CreateShapeTexture(item.shape, item.itemColor);
                GUI.DrawTexture(iconRect, shapeTex, ScaleMode.ScaleToFit);
            }
        }

        private Texture2D CreateShapeTexture(ItemShape shape, Color color)
        {
            // 使用缓存，避免每帧创建纹理
            string key = $"{shape}_{color.r:F2}_{color.g:F2}_{color.b:F2}";

            if (shapeTextureCache != null && shapeTextureCache.TryGetValue(key, out Texture2D cached))
            {
                return cached;
            }

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
                        case ItemShape.Star:
                            float starR = radius * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(angle * 2.5f)));
                            filled = dist < starR;
                            break;
                        case ItemShape.Heart:
                            float nx = dx / radius;
                            float ny = -dy / radius;
                            filled = Mathf.Pow(nx * nx + ny * ny - 1, 3) - nx * nx * ny * ny * ny < 0;
                            break;
                    }

                    pixels[y * size + x] = filled ? color : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // 缓存纹理
            if (shapeTextureCache != null)
            {
                shapeTextureCache[key] = tex;
            }

            return tex;
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            // 使用缓存的纹理，避免每帧创建
            Texture2D borderTex;
            if (color == greenBearColor)
            {
                borderTex = greenColorBarTex;
            }
            else if (color == redBearColor)
            {
                borderTex = redColorBarTex;
            }
            else
            {
                // 对于其他颜色，创建一次并缓存到形状缓存中
                string key = $"border_{color.r:F2}_{color.g:F2}_{color.b:F2}";
                if (!shapeTextureCache.TryGetValue(key, out borderTex))
                {
                    borderTex = MakeTexture(1, 1, color);
                    shapeTextureCache[key] = borderTex;
                }
            }

            // 上
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), borderTex);
            // 下
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), borderTex);
            // 左
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), borderTex);
            // 右
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), borderTex);
        }

        private Vector2 CalculatePosition(float totalWidth, float totalHeight)
        {
            float x = 0, y = 0;

            switch (position)
            {
                case UIPosition.TopLeft:
                    x = offset.x;
                    y = offset.y;
                    break;
                case UIPosition.TopCenter:
                    x = (Screen.width - totalWidth) / 2 + offset.x;
                    y = offset.y;
                    break;
                case UIPosition.TopRight:
                    x = Screen.width - totalWidth - offset.x;
                    y = offset.y;
                    break;
                case UIPosition.BottomLeft:
                    x = offset.x;
                    y = Screen.height - totalHeight - offset.y;
                    break;
                case UIPosition.BottomCenter:
                    x = (Screen.width - totalWidth) / 2 + offset.x;
                    y = Screen.height - totalHeight - offset.y;
                    break;
                case UIPosition.BottomRight:
                    x = Screen.width - totalWidth - offset.x;
                    y = Screen.height - totalHeight - offset.y;
                    break;
            }

            return new Vector2(x, y);
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }

    public enum UIPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}
