using UnityEngine;
using BearCar.Cart;
using BearCar.Player;

namespace BearCar.UI
{
    /// <summary>
    /// 游戏UI - 显示玩家状态和操作提示
    /// </summary>
    [DefaultExecutionOrder(-200)] // 在 LocalPlayerManager 之前执行
    public class DebugUI : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private bool showUI = true;
        [SerializeField] private bool showDebugInfo = false;

        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle boxStyle;
        private GUIStyle staminaBarStyle;
        private GUIStyle staminaFillStyle;
        private Texture2D staminaBarBg;
        private Texture2D staminaBarFill;

        private void Awake()
        {
            // 确保有 LocalPlayerManager
            if (LocalPlayerManager.Instance == null)
            {
                var managerObj = new GameObject("LocalPlayerManager");
                managerObj.AddComponent<LocalPlayerManager>();
            }
        }

        private void Start()
        {
            InitStyles();
        }

        private void InitStyles()
        {
            labelStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            titleStyle = new GUIStyle
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.9f, 0.3f) }
            };

            boxStyle = new GUIStyle
            {
                normal = { background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.5f)) }
            };

            // 体力条背景
            staminaBarBg = MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            staminaBarFill = MakeTexture(1, 1, new Color(0.3f, 0.8f, 0.3f, 1f));
        }

        private void OnGUI()
        {
            if (!showUI) return;

            DrawPlayerHUD();

            if (showDebugInfo)
            {
                DrawDebugInfo();
            }

            DrawControlsHint();
        }

        private void DrawPlayerHUD()
        {
            var localBears = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);
            if (localBears.Length == 0) return;

            // 左上角 Player 1
            foreach (var bear in localBears)
            {
                if (bear.PlayerIndex == 0)
                {
                    DrawPlayerStatus(bear, new Rect(20, 20, 200, 80), "P1", new Color(0.3f, 0.5f, 1f));
                }
                else if (bear.PlayerIndex == 1)
                {
                    // 右上角 Player 2
                    DrawPlayerStatus(bear, new Rect(Screen.width - 220, 20, 200, 80), "P2", new Color(1f, 0.5f, 0.3f));
                }
            }
        }

        private void DrawPlayerStatus(LocalBearController bear, Rect area, string playerName, Color color)
        {
            GUI.Box(area, GUIContent.none, boxStyle);

            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 10, area.width - 20, area.height - 20));

            // 玩家名称
            var nameStyle = new GUIStyle(labelStyle) { normal = { textColor = color } };
            GUILayout.Label(playerName, nameStyle);

            // 状态
            string status = bear.IsAttached ? (bear.IsPushing ? "Pushing" : "Attached") : "Free";
            GUILayout.Label(status, labelStyle);

            // 体力条
            if (bear.Stamina != null)
            {
                float staminaPercent = bear.Stamina.CurrentStaminaValue / bear.Stamina.MaxStamina;
                Rect barRect = GUILayoutUtility.GetRect(180, 16);

                // 背景
                GUI.DrawTexture(barRect, staminaBarBg);

                // 填充
                Color fillColor = bear.Stamina.IsExhaustedValue
                    ? new Color(0.8f, 0.2f, 0.2f)
                    : Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.3f, 0.8f, 0.3f), staminaPercent);

                var fillTex = MakeTexture(1, 1, fillColor);
                GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width * staminaPercent, barRect.height), fillTex);
            }

            GUILayout.EndArea();
        }

        private void DrawControlsHint()
        {
            float hintWidth = 300;
            float hintHeight = 60;
            Rect hintArea = new Rect(
                (Screen.width - hintWidth) / 2,
                Screen.height - hintHeight - 20,
                hintWidth,
                hintHeight
            );

            GUI.Box(hintArea, GUIContent.none, boxStyle);

            var centerStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };

            GUI.Label(new Rect(hintArea.x, hintArea.y + 10, hintWidth, 20),
                "P1: WASD + E + Space", centerStyle);
            GUI.Label(new Rect(hintArea.x, hintArea.y + 32, hintWidth, 20),
                "P2: Arrows + Enter + RShift", centerStyle);
        }

        private void DrawDebugInfo()
        {
            GUILayout.BeginArea(new Rect(10, 120, 300, 400), boxStyle);
            GUILayout.Space(5);

            GUILayout.Label("=== Debug ===", labelStyle);

            // Cart Info
            var cart = FindFirstObjectByType<CartController>();
            if (cart != null)
            {
                GUILayout.Label($"Cart Pos: {cart.transform.position.x:F1}", labelStyle);
                var rb = cart.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    GUILayout.Label($"Cart Vel: {rb.linearVelocity.x:F2}", labelStyle);
                }
            }

            // Local Bears
            var localBears = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);
            foreach (var bear in localBears)
            {
                GUILayout.Label($"P{bear.PlayerIndex + 1}: Push={bear.IsPushing}, Dir={bear.PushDirection:F1}", labelStyle);
            }

            GUILayout.EndArea();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
