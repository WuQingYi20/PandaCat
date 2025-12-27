using UnityEngine;
using System.Collections.Generic;
using BearCar.Player;

namespace BearCar.Item
{
    /// <summary>
    /// 道具效果日志系统 - 显示道具使用记录
    /// </summary>
    public class ItemEffectLog : MonoBehaviour
    {
        [Header("=== 显示设置 ===")]
        [Tooltip("日志位置")]
        public LogPosition position = LogPosition.TopRight;

        [Tooltip("距离边缘的偏移")]
        public Vector2 offset = new Vector2(15, 15);

        [Tooltip("最大显示条数")]
        public int maxEntries = 4;

        [Tooltip("日志消失时间（秒）")]
        public float fadeTime = 3f;

        [Header("=== 外观设置 ===")]
        [Tooltip("日志宽度")]
        public float logWidth = 240f;

        [Tooltip("每条日志高度")]
        public float entryHeight = 45f;

        [Header("=== 玩家颜色 ===")]
        public Color greenBearColor = new Color(0.3f, 0.9f, 0.4f);
        public Color redBearColor = new Color(0.95f, 0.3f, 0.3f);

        [Tooltip("背景颜色")]
        public Color backgroundColor = new Color(0, 0, 0, 0.7f);

        [Tooltip("标题颜色")]
        public Color titleColor = new Color(1f, 0.9f, 0.3f);

        [Tooltip("效果颜色")]
        public Color effectColor = new Color(0.8f, 1f, 0.8f);

        // 日志条目
        private class LogEntry
        {
            public string playerName;
            public string itemName;
            public string effectText;
            public Color itemColor;
            public float timestamp;
            public float alpha;
            public bool isPickup; // true=拾取, false=使用
        }

        private List<LogEntry> entries = new List<LogEntry>();
        private GUIStyle bgStyle;
        private GUIStyle titleStyle;
        private GUIStyle effectStyle;
        private Texture2D bgTexture;

        private void Start()
        {
            InitStyles();

            // 订阅道具使用事件
            if (SharedInventory.Instance != null)
            {
                SharedInventory.Instance.OnItemUsed += OnItemUsed;
            }
        }

        private void OnDestroy()
        {
            if (SharedInventory.Instance != null)
            {
                SharedInventory.Instance.OnItemUsed -= OnItemUsed;
            }
        }

        private void InitStyles()
        {
            bgTexture = MakeTexture(2, 2, backgroundColor);

            bgStyle = new GUIStyle
            {
                normal = { background = bgTexture },
                padding = new RectOffset(10, 10, 8, 8)
            };

            titleStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = titleColor },
                alignment = TextAnchor.MiddleLeft
            };

            effectStyle = new GUIStyle
            {
                fontSize = 12,
                normal = { textColor = effectColor },
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
        }

        private void OnItemUsed(int playerIndex, int slotIndex, ItemData item)
        {
            if (item == null) return;

            // 使用玩家索引确定名称
            string playerName = playerIndex == 0 ? "绿熊" : "红熊";
            string effectText = item.GetEffectDescription(playerIndex);

            AddLogEntry(playerName, item.itemName, effectText, item.itemColor);
        }

        /// <summary>
        /// 添加使用道具日志
        /// </summary>
        public void AddLog(int playerIndex, ItemData item)
        {
            if (item == null) return;

            string bearType = playerIndex == 0 ? "绿熊" : "红熊";
            string playerName = $"{bearType}";

            // 使用 ItemData 自带的效果描述（支持颜色亲和）
            string effectText = item.GetEffectDescription(playerIndex);

            AddLogEntry(playerName, item.itemName, effectText, item.itemColor, LogType.Use);
        }

        /// <summary>
        /// 添加拾取道具日志
        /// </summary>
        public void AddPickupLog(int playerIndex, ItemData item, int count = 1)
        {
            if (item == null) return;

            string playerName;
            if (playerIndex >= 0)
            {
                string bearType = playerIndex == 0 ? "绿熊" : "红熊";
                playerName = bearType;
            }
            else
            {
                playerName = "玩家";
            }

            string effectText = count > 1 ? $"获得 x{count}" : "已添加到背包";

            AddLogEntry(playerName, item.itemName, effectText, item.itemColor, LogType.Pickup);
        }

        private enum LogType { Use, Pickup }

        private void AddLogEntry(string playerName, string itemName, string effectText, Color color, LogType logType = LogType.Use)
        {
            var entry = new LogEntry
            {
                playerName = playerName,
                itemName = itemName,
                effectText = effectText,
                itemColor = color,
                timestamp = Time.time,
                alpha = 1f,
                isPickup = (logType == LogType.Pickup)
            };

            entries.Insert(0, entry);

            // 限制条目数量
            while (entries.Count > maxEntries)
            {
                entries.RemoveAt(entries.Count - 1);
            }

            string action = logType == LogType.Pickup ? "拾取了" : "使用了";
            Debug.Log($"[ItemLog] {playerName} {action} {itemName}: {effectText}");
        }

        private string GetLastUserName()
        {
            // 简化：返回通用名称，实际使用时由 AddLog 指定
            return "玩家";
        }

        private string GetEffectDescription(ItemData item)
        {
            switch (item.itemType)
            {
                case ItemType.SpeedBoost:
                    return $"速度 x{item.effectValue:F1}，持续 {item.effectDuration}秒";

                case ItemType.StaminaRecover:
                    return $"恢复 {item.effectValue:F1} 点体力";

                case ItemType.Shield:
                    return $"护盾保护 {item.effectDuration}秒";

                case ItemType.Magnet:
                    return $"吸引道具 {item.effectDuration}秒";

                case ItemType.TimeSlowdown:
                    return $"时间减缓至 {item.effectValue:F1}x，{item.effectDuration}秒";

                default:
                    return item.description ?? "未知效果";
            }
        }

        private void Update()
        {
            // 更新淡出效果
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                float age = Time.time - entry.timestamp;

                if (age > fadeTime)
                {
                    entries.RemoveAt(i);
                }
                else if (age > fadeTime * 0.7f)
                {
                    // 最后30%时间开始淡出
                    float fadeProgress = (age - fadeTime * 0.7f) / (fadeTime * 0.3f);
                    entry.alpha = 1f - fadeProgress;
                }
            }
        }

        private void OnGUI()
        {
            if (entries.Count == 0) return;

            if (bgTexture == null)
            {
                InitStyles();
            }

            Vector2 startPos = CalculatePosition();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float y = startPos.y + i * (entryHeight + 5);

                DrawLogEntry(new Rect(startPos.x, y, logWidth, entryHeight), entry);
            }
        }

        private void DrawLogEntry(Rect rect, LogEntry entry)
        {
            // 设置透明度
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, entry.alpha);

            // 背景
            GUI.Box(rect, "", bgStyle);

            // 根据玩家名称确定颜色条颜色
            Color playerColor;
            if (entry.playerName.Contains("绿熊"))
            {
                playerColor = greenBearColor;
            }
            else if (entry.playerName.Contains("红熊"))
            {
                playerColor = redBearColor;
            }
            else
            {
                playerColor = entry.itemColor;
            }

            // 左侧玩家颜色条
            Texture2D colorBar = MakeTexture(1, 1, playerColor);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 4, rect.height), colorBar);

            // 标题行：玩家名 + 动作 + 道具名
            Rect titleRect = new Rect(rect.x + 10, rect.y + 4, rect.width - 16, 18);
            string action = entry.isPickup ? "拾取" : "使用";
            titleStyle.normal.textColor = new Color(playerColor.r, playerColor.g, playerColor.b, entry.alpha);
            GUI.Label(titleRect, $"{entry.playerName} {action} {entry.itemName}", titleStyle);

            // 效果描述
            Rect effectRect = new Rect(rect.x + 10, rect.y + 24, rect.width - 16, 18);
            effectStyle.normal.textColor = new Color(effectColor.r, effectColor.g, effectColor.b, entry.alpha);
            GUI.Label(effectRect, entry.effectText, effectStyle);

            GUI.color = oldColor;
        }

        private Vector2 CalculatePosition()
        {
            float totalHeight = entries.Count * (entryHeight + 5);

            switch (position)
            {
                case LogPosition.TopLeft:
                    return new Vector2(offset.x, offset.y);

                case LogPosition.TopRight:
                    return new Vector2(Screen.width - logWidth - offset.x, offset.y);

                case LogPosition.BottomLeft:
                    return new Vector2(offset.x, Screen.height - totalHeight - offset.y);

                case LogPosition.BottomRight:
                    return new Vector2(Screen.width - logWidth - offset.x, Screen.height - totalHeight - offset.y);

                default:
                    return new Vector2(offset.x, offset.y);
            }
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

    public enum LogPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
