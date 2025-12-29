using UnityEngine;
using BearCar.Player;
using BearCar.Cart;

namespace BearCar.UI
{
    /// <summary>
    /// 新手教程提示UI - 显示按键提示
    /// 可以设置只在新手关卡显示
    /// </summary>
    public class TutorialHintUI : MonoBehaviour
    {
        [Header("=== 教程设置 ===")]
        [Tooltip("是否启用教程提示")]
        public bool enableTutorial = true;

        [Tooltip("是否只在新手关卡显示")]
        public bool onlyInTutorialLevel = false;

        [Tooltip("提示显示距离")]
        public float hintDistance = 2.5f;

        [Tooltip("提示淡出时间")]
        public float fadeTime = 0.3f;

        [Header("=== 外观设置 ===")]
        [Tooltip("提示框背景颜色")]
        public Color backgroundColor = new Color(0, 0, 0, 0.7f);

        [Tooltip("文字颜色")]
        public Color textColor = Color.white;

        [Tooltip("按键高亮颜色")]
        public Color keyColor = new Color(1f, 0.9f, 0.3f);

        // 缓存引用
        private LocalBearController[] players;
        private CartController cart;
        private float lastRefresh;
        private const float REFRESH_INTERVAL = 0.5f;

        // 提示状态
        private float[] hintAlpha = new float[2]; // P1, P2 的提示透明度
        private bool[] hasGrabbedBefore = new bool[2]; // 记录是否已经抓取过

        // GUIStyle 缓存
        private GUIStyle hintStyle;
        private GUIStyle keyStyle;
        private Texture2D bgTexture;

        private void Start()
        {
            RefreshReferences();
            InitStyles();

            // 从 PlayerPrefs 读取是否已完成教程
            hasGrabbedBefore[0] = PlayerPrefs.GetInt("Tutorial_P1_Grabbed", 0) == 1;
            hasGrabbedBefore[1] = PlayerPrefs.GetInt("Tutorial_P2_Grabbed", 0) == 1;
        }

        private void InitStyles()
        {
            // 创建背景纹理
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, backgroundColor);
            bgTexture.Apply();

            hintStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor },
                padding = new RectOffset(10, 10, 8, 8)
            };

            keyStyle = new GUIStyle(hintStyle)
            {
                fontSize = 20,
                normal = { textColor = keyColor }
            };
        }

        private void Update()
        {
            if (!enableTutorial) return;

            // 定期刷新引用
            if (Time.time - lastRefresh > REFRESH_INTERVAL)
            {
                RefreshReferences();
                lastRefresh = Time.time;
            }

            // 更新每个玩家的提示状态
            UpdateHintStates();
        }

        private void RefreshReferences()
        {
            players = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);
            cart = FindFirstObjectByType<CartController>();
        }

        private void UpdateHintStates()
        {
            if (cart == null) return;

            foreach (var player in players)
            {
                if (player == null) continue;

                int idx = player.PlayerIndex;
                if (idx < 0 || idx > 1) continue;

                // 如果已经抓取过且不是教程关卡，不显示
                if (hasGrabbedBefore[idx] && !onlyInTutorialLevel) continue;

                // 计算与车的距离
                float distance = Vector2.Distance(player.transform.position, cart.transform.position);

                // 目标透明度
                float targetAlpha = 0f;

                // 靠近车且没有抓取时显示提示
                if (distance < hintDistance && !player.IsAttached)
                {
                    targetAlpha = 1f;
                }
                // 正在抓取时，短暂显示"推车"提示然后消失
                else if (player.IsAttached && !player.IsPushing)
                {
                    targetAlpha = 0.8f;
                }

                // 平滑过渡
                hintAlpha[idx] = Mathf.MoveTowards(hintAlpha[idx], targetAlpha, Time.deltaTime / fadeTime);

                // 记录首次抓取
                if (player.IsAttached && !hasGrabbedBefore[idx])
                {
                    hasGrabbedBefore[idx] = true;
                    PlayerPrefs.SetInt($"Tutorial_P{idx + 1}_Grabbed", 1);
                    PlayerPrefs.Save();
                }
            }
        }

        private void OnGUI()
        {
            if (!enableTutorial) return;
            if (cart == null || players == null) return;

            foreach (var player in players)
            {
                if (player == null) continue;

                int idx = player.PlayerIndex;
                if (idx < 0 || idx > 1) continue;
                if (hintAlpha[idx] < 0.01f) continue;

                DrawPlayerHint(player, idx);
            }
        }

        private void DrawPlayerHint(LocalBearController player, int playerIndex)
        {
            if (Camera.main == null) return;

            // 计算屏幕位置（玩家头顶上方）
            Vector3 worldPos = player.transform.position + Vector3.up * 1.8f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 转换为 GUI 坐标
            screenPos.y = Screen.height - screenPos.y;

            // 如果在屏幕外，不绘制
            if (screenPos.z < 0) return;

            // 获取按键名称 - 根据玩家颜色/索引
            string grabKey, pushKey;
            Color playerColor;
            string playerName;

            if (playerIndex == 0)
            {
                grabKey = "E";
                pushKey = "A/D";
                playerColor = new Color(0.3f, 0.9f, 0.4f); // 绿色
                playerName = "P1";
            }
            else
            {
                grabKey = "Enter";
                pushKey = "←/→";
                playerColor = new Color(0.9f, 0.4f, 0.3f); // 红色
                playerName = "P2";
            }

            // 根据状态显示不同提示
            string keyText;
            string actionText;

            if (!player.IsAttached)
            {
                keyText = grabKey;
                actionText = "抓车";
            }
            else if (!player.IsPushing)
            {
                keyText = pushKey;
                actionText = "推车";
            }
            else
            {
                return; // 已经在推了，不需要提示
            }

            // 计算提示框大小
            float boxWidth = 140f;
            float boxHeight = 45f;
            Rect boxRect = new Rect(
                screenPos.x - boxWidth / 2,
                screenPos.y - boxHeight / 2,
                boxWidth,
                boxHeight
            );

            float alpha = hintAlpha[playerIndex];

            // 绘制背景（带玩家颜色边框）
            Color bgColor = new Color(0, 0, 0, 0.75f * alpha);
            Color borderColor = new Color(playerColor.r, playerColor.g, playerColor.b, alpha);

            // 边框
            DrawRect(new Rect(boxRect.x - 2, boxRect.y - 2, boxWidth + 4, boxHeight + 4), borderColor);
            // 背景
            DrawRect(boxRect, bgColor);

            // 绘制文字
            var tempStyle = new GUIStyle(hintStyle);
            tempStyle.normal.textColor = new Color(1, 1, 1, alpha);
            tempStyle.fontSize = 14;

            // 第一行：按键
            var keyDisplayStyle = new GUIStyle(keyStyle);
            keyDisplayStyle.normal.textColor = new Color(keyColor.r, keyColor.g, keyColor.b, alpha);
            keyDisplayStyle.fontSize = 18;
            keyDisplayStyle.alignment = TextAnchor.MiddleCenter;

            Rect keyRect = new Rect(boxRect.x, boxRect.y + 2, boxWidth, 24);
            GUI.Label(keyRect, $"[ {keyText} ]", keyDisplayStyle);

            // 第二行：动作
            Rect actionRect = new Rect(boxRect.x, boxRect.y + 24, boxWidth, 20);
            tempStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(actionRect, actionText, tempStyle);
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        /// <summary>
        /// 重置教程进度（用于测试或重新开始）
        /// </summary>
        public void ResetTutorial()
        {
            hasGrabbedBefore[0] = false;
            hasGrabbedBefore[1] = false;
            PlayerPrefs.DeleteKey("Tutorial_P1_Grabbed");
            PlayerPrefs.DeleteKey("Tutorial_P2_Grabbed");
            PlayerPrefs.Save();
            Debug.Log("[Tutorial] 教程进度已重置");
        }

        /// <summary>
        /// 强制显示教程（用于新手关卡）
        /// </summary>
        public void ForceShowTutorial()
        {
            enableTutorial = true;
            onlyInTutorialLevel = true;
            hasGrabbedBefore[0] = false;
            hasGrabbedBefore[1] = false;
        }
    }
}
