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
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 500), boxStyle);
            GUILayout.Space(10);

            GUILayout.Label("=== Bear Car Debug ===", labelStyle);
            GUILayout.Space(5);

            // Network Status
            var nm = NetworkManager.Singleton;
            if (nm != null)
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
                GUILayout.Label($"State: {cart.State.Value}", labelStyle);
                GUILayout.Label($"Active Pushers: {cart.ActivePushers.Value}", labelStyle);
                GUILayout.Label($"Position: {cart.transform.position:F2}", labelStyle);

                var rb = cart.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    GUILayout.Label($"Velocity: {rb.linearVelocity:F2}", labelStyle);
                }
            }

            GUILayout.Space(10);

            // Bears Info
            var bears = FindObjectsByType<BearController>(FindObjectsSortMode.None);
            if (bears.Length > 0)
            {
                GUILayout.Label("=== Bears ===", labelStyle);
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

            GUILayout.Space(10);

            // Controls Help
            GUILayout.Label("=== Controls ===", labelStyle);
            GUILayout.Label("WASD/Arrows: Move", labelStyle);
            GUILayout.Label("E/Hold: Push Cart", labelStyle);

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
