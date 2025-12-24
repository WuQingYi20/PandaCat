using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BearCar.Network
{
    public class NetworkUIManager : MonoBehaviour
    {
        [Header("Connection Panel")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private TMP_InputField addressInput;

        [Header("In-Game Panel")]
        [SerializeField] private GameObject inGamePanel;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private NetworkGameManager gameManager;

        private void Start()
        {
            gameManager = FindFirstObjectByType<NetworkGameManager>();

            // 自动查找按钮（如果没有手动分配）
            AutoFindUIElements();

            Debug.Log($"[NetworkUI] GameManager found: {gameManager != null}");
            Debug.Log($"[NetworkUI] HostButton assigned: {hostButton != null}");
            Debug.Log($"[NetworkUI] NetworkManager.Singleton: {NetworkManager.Singleton != null}");

            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostClicked);
            else
                Debug.LogError("[NetworkUI] HostButton is NULL!");

            if (clientButton != null)
                clientButton.onClick.AddListener(OnClientClicked);

            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(OnDisconnectClicked);

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
            }
            else
            {
                Debug.LogError("[NetworkUI] NetworkManager.Singleton is NULL! Make sure NetworkManager exists in scene.");
            }

            ShowConnectionPanel();
        }

        private void AutoFindUIElements()
        {
            // 自动查找 ConnectionPanel
            if (connectionPanel == null)
            {
                var panel = transform.Find("ConnectionPanel");
                if (panel != null) connectionPanel = panel.gameObject;
            }

            // 自动查找 InGamePanel
            if (inGamePanel == null)
            {
                var panel = transform.Find("InGamePanel");
                if (panel != null) inGamePanel = panel.gameObject;
            }

            // 在 ConnectionPanel 中查找按钮
            if (connectionPanel != null)
            {
                if (hostButton == null)
                {
                    var btn = connectionPanel.transform.Find("HostButton");
                    if (btn != null) hostButton = btn.GetComponent<Button>();
                }

                if (clientButton == null)
                {
                    var btn = connectionPanel.transform.Find("ClientButton");
                    if (btn != null) clientButton = btn.GetComponent<Button>();
                }

                if (addressInput == null)
                {
                    var input = connectionPanel.transform.Find("AddressInput");
                    if (input != null) addressInput = input.GetComponent<TMP_InputField>();
                }
            }

            // 在 InGamePanel 中查找元素
            if (inGamePanel != null)
            {
                if (disconnectButton == null)
                {
                    var btn = inGamePanel.transform.Find("DisconnectButton");
                    if (btn != null) disconnectButton = btn.GetComponent<Button>();
                }

                if (statusText == null)
                {
                    var txt = inGamePanel.transform.Find("StatusText");
                    if (txt != null) statusText = txt.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
            }
        }

        private void OnHostClicked()
        {
            Debug.Log("[NetworkUI] Host button clicked!");

            if (!EnsureNetworkReady())
            {
                Debug.LogError("[NetworkUI] Cannot start host - NetworkManager not ready!");
                return;
            }

            // 优先使用 GameManager，否则直接用 NetworkManager
            if (gameManager != null)
            {
                gameManager.StartHost();
            }
            else
            {
                NetworkManager.Singleton.StartHost();
                Debug.Log("[NetworkUI] Started Host via NetworkManager.Singleton");
            }
        }

        private void OnClientClicked()
        {
            Debug.Log("[NetworkUI] Client button clicked!");

            if (!EnsureNetworkReady())
            {
                Debug.LogError("[NetworkUI] Cannot start client - NetworkManager not ready!");
                return;
            }

            string address = "127.0.0.1";
            if (addressInput != null && !string.IsNullOrEmpty(addressInput.text))
            {
                address = addressInput.text;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(address, 7777);
            }

            if (gameManager != null)
            {
                gameManager.StartClient(address);
            }
            else
            {
                NetworkManager.Singleton.StartClient();
                Debug.Log($"[NetworkUI] Started Client, connecting to {address}");
            }
        }

        private void OnDisconnectClicked()
        {
            if (gameManager != null)
            {
                gameManager.Disconnect();
            }
            else if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private bool EnsureNetworkReady()
        {
            // 检查 NetworkManager 是否存在
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkUI] NetworkManager.Singleton is null! Creating one...");

                // 自动创建 NetworkManager
                var nmObj = new GameObject("NetworkManager");
                nmObj.AddComponent<NetworkManager>();
                nmObj.AddComponent<UnityTransport>();

                Debug.Log("[NetworkUI] NetworkManager created automatically");
            }

            // 检查 Transport 是否存在
            var nm = NetworkManager.Singleton;
            if (nm.NetworkConfig == null)
            {
                nm.NetworkConfig = new NetworkConfig();
            }

            var transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = nm.gameObject.AddComponent<UnityTransport>();
                Debug.Log("[NetworkUI] UnityTransport added automatically");
            }

            // 设置 Transport
            if (nm.NetworkConfig.NetworkTransport == null)
            {
                nm.NetworkConfig.NetworkTransport = transport;
                Debug.Log("[NetworkUI] NetworkTransport configured");
            }

            return true;
        }

        private void OnConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                ShowInGamePanel();
                UpdateStatus();
            }
        }

        private void OnDisconnected(ulong clientId)
        {
            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsClient)
            {
                ShowConnectionPanel();
            }
        }

        private void ShowConnectionPanel()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(true);

            if (inGamePanel != null)
                inGamePanel.SetActive(false);
        }

        private void ShowInGamePanel()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(false);

            if (inGamePanel != null)
                inGamePanel.SetActive(true);
        }

        private void UpdateStatus()
        {
            if (statusText == null) return;

            var nm = NetworkManager.Singleton;
            if (nm == null) return;

            if (nm.IsHost)
            {
                statusText.text = "Host (等待玩家加入...)";
            }
            else if (nm.IsClient)
            {
                statusText.text = "已连接";
            }
        }

        private void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
                if (statusText != null)
                {
                    statusText.text = $"Host - 玩家数: {playerCount}/2";
                }
            }
        }
    }
}
