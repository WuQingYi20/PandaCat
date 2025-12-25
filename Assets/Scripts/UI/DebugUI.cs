using UnityEngine;
using Unity.Netcode;
using BearCar.Cart;
using BearCar.Player;

namespace BearCar.UI
{
    public class DebugUI : MonoBehaviour
    {
        [SerializeField] private bool showDebugInfo = true;

        private GUIStyle labelStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private GameObject aiInstance;
        private LocalPlayerManager localPlayerManager;

        private void Awake()
        {
            labelStyle = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            boxStyle = new GUIStyle
            {
                normal = { background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.7f)) }
            };

            buttonStyle = new GUIStyle("button")
            {
                fontSize = 14
            };

            // 创建本地玩家管理器
            var managerObj = new GameObject("LocalPlayerManager");
            localPlayerManager = managerObj.AddComponent<LocalPlayerManager>();
        }

        private void Update()
        {
            // 按 T 键切换 AI 熊
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleAI();
            }
        }

        private void ToggleAI()
        {
            if (aiInstance == null)
            {
                aiInstance = new GameObject("BearAI");
                aiInstance.transform.position = new Vector3(-8f, 1f, 0f);
                aiInstance.AddComponent<BearAI>();
            }
            else
            {
                Destroy(aiInstance);
                aiInstance = null;
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 600), boxStyle);
            GUILayout.Space(10);

            GUILayout.Label("=== Bear Car Debug ===", labelStyle);
            GUILayout.Space(5);

            // 本地多人模式按钮
            if (localPlayerManager != null)
            {
                if (!localPlayerManager.IsLocalMultiplayerActive)
                {
                    if (GUILayout.Button("Start Local 2P", buttonStyle))
                    {
                        StartLocalMultiplayer();
                    }
                }
                else
                {
                    GUILayout.Label($"=== Local Mode ({localPlayerManager.LocalPlayerCount}P) ===", labelStyle);
                    if (localPlayerManager.LocalPlayerCount < 2)
                    {
                        GUILayout.Label("Player 2: Press Enter to join!", labelStyle);
                    }

                    if (GUILayout.Button("Stop Local Mode", buttonStyle))
                    {
                        StopLocalMultiplayer();
                    }
                }
                GUILayout.Space(5);
            }

            // Network Status
            var nm = NetworkManager.Singleton;
            bool isLocalMode = localPlayerManager != null && localPlayerManager.IsLocalMultiplayerActive;

            if (isLocalMode)
            {
                GUILayout.Label("Network: Local Only", labelStyle);
            }
            else if (nm != null)
            {
                string networkStatus = nm.IsHost ? "Host" :
                                       nm.IsClient ? "Client" :
                                       nm.IsServer ? "Server" : "Not Connected";
                GUILayout.Label($"Network: {networkStatus}", labelStyle);

                if (nm.IsConnectedClient)
                {
                    GUILayout.Label($"Client ID: {nm.LocalClientId}", labelStyle);
                    GUILayout.Label($"Connected Clients: {nm.ConnectedClientsIds.Count}", labelStyle);
                }
            }
            else
            {
                GUILayout.Label("Network: Not Initialized", labelStyle);
            }

            GUILayout.Space(10);

            // Cart Info
            var cart = FindFirstObjectByType<CartController>();
            if (cart != null)
            {
                GUILayout.Label("=== Cart ===", labelStyle);
                if (!isLocalMode)
                {
                    GUILayout.Label($"State: {cart.State.Value}", labelStyle);
                    GUILayout.Label($"Active Pushers: {cart.ActivePushers.Value}", labelStyle);
                }
                GUILayout.Label($"Position: {cart.transform.position:F2}", labelStyle);

                var rb = cart.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    GUILayout.Label($"Velocity: {rb.linearVelocity:F2}", labelStyle);
                }
            }

            GUILayout.Space(10);

            // Network Bears Info
            var bears = FindObjectsByType<BearController>(FindObjectsSortMode.None);
            if (bears.Length > 0)
            {
                GUILayout.Label("=== Network Bears ===", labelStyle);
                foreach (var bear in bears)
                {
                    var stamina = bear.GetComponent<StaminaSystem>();
                    string staminaStr = stamina != null ? $"{stamina.CurrentStamina.Value:F1}" : "N/A";
                    string exhaustedStr = stamina != null && stamina.IsExhausted.Value ? " [EXHAUSTED]" : "";

                    GUILayout.Label($"Bear {bear.OwnerClientId}:", labelStyle);
                    GUILayout.Label($"  Pushing: {bear.IsPushing.Value}", labelStyle);
                    GUILayout.Label($"  Stamina: {staminaStr}{exhaustedStr}", labelStyle);
                }
            }

            // Local Bears Info
            var localBears = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);
            if (localBears.Length > 0)
            {
                GUILayout.Label("=== Local Bears ===", labelStyle);
                foreach (var bear in localBears)
                {
                    string color = bear.PlayerIndex == 0 ? "(Blue)" : "(Orange)";
                    GUILayout.Label($"Player {bear.PlayerIndex + 1} {color}:", labelStyle);
                    GUILayout.Label($"  Attached: {bear.IsAttached}", labelStyle);
                    GUILayout.Label($"  Pushing: {bear.IsPushing}", labelStyle);

                    // 显示体力
                    if (bear.Stamina != null)
                    {
                        string exhaustedStr = bear.Stamina.IsExhaustedValue ? " [EXHAUSTED]" : "";
                        GUILayout.Label($"  Stamina: {bear.Stamina.CurrentStaminaValue:F1}{exhaustedStr}", labelStyle);
                    }
                }
            }

            GUILayout.Space(10);

            // Controls Help
            GUILayout.Label("=== Controls ===", labelStyle);
            if (isLocalMode)
            {
                GUILayout.Label("P1: WASD + E + Space", labelStyle);
                GUILayout.Label("P2: Arrows + Enter + RShift", labelStyle);
            }
            else
            {
                GUILayout.Label("WASD/Arrows: Move", labelStyle);
                GUILayout.Label("E: Attach/Detach Cart", labelStyle);
                GUILayout.Label("Space: Jump", labelStyle);
            }
            GUILayout.Label("T: Toggle AI Helper", labelStyle);

            // AI 状态
            if (aiInstance != null)
            {
                GUILayout.Space(5);
                GUILayout.Label("=== AI Helper ===", labelStyle);
                var ai = aiInstance.GetComponent<BearAI>();
                if (ai != null)
                {
                    GUILayout.Label($"AI Pushing: {ai.IsPushingCart}", labelStyle);
                }
            }

            GUILayout.EndArea();
        }

        private void StartLocalMultiplayer()
        {
            // 清理已有的网络玩家（避免重复）
            var existingBears = FindObjectsByType<BearController>(FindObjectsSortMode.None);
            foreach (var bear in existingBears)
            {
                Destroy(bear.gameObject);
            }

            // 初始化车辆为本地模式
            var cart = FindFirstObjectByType<CartController>();
            if (cart != null)
            {
                cart.InitializeLocalMode();
            }

            // 启动本地多人
            localPlayerManager.StartLocalMultiplayer();
        }

        private void StopLocalMultiplayer()
        {
            localPlayerManager.StopLocalMultiplayer();
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
