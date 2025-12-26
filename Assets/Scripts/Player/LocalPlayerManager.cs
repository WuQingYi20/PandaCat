using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using BearCar.Cart;

namespace BearCar.Player
{
    /// <summary>
    /// 本地多人管理器
    /// 管理本地玩家的加入和退出
    /// </summary>
    [DefaultExecutionOrder(-100)] // 确保在其他脚本之前执行
    public class LocalPlayerManager : MonoBehaviour
    {
        public static LocalPlayerManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxLocalPlayers = 2;
        [SerializeField] private bool autoStartTwoPlayers = true;

        [Header("Spawn Points")]
        [SerializeField] private Vector3 player1SpawnPos = new Vector3(-6f, 1f, 0f);
        [SerializeField] private Vector3 player2SpawnPos = new Vector3(-7.5f, 1f, 0f);

        private List<LocalBearController> localPlayers = new List<LocalBearController>();
        private bool isLocalMultiplayerActive = false;
        private bool hasInitialized = false;

        public bool IsLocalMultiplayerActive => isLocalMultiplayerActive;
        public int LocalPlayerCount => localPlayers.Count;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                // 立即禁用 NetworkManager，防止它自动生成玩家
                if (autoStartTwoPlayers)
                {
                    DisableNetworkManager();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 自动启动本地双人模式
            if (autoStartTwoPlayers && !hasInitialized)
            {
                hasInitialized = true;
                InitializeLocalGame();
            }
        }

        /// <summary>
        /// 初始化本地游戏（默认双人）
        /// </summary>
        public void InitializeLocalGame()
        {
            // 禁用 NetworkManager 防止自动生成网络玩家
            DisableNetworkManager();

            // 清理网络玩家（使用 DestroyImmediate 立即删除）
            CleanupNetworkBears();

            // 清理已存在的本地玩家（避免重复）
            var existingLocalBears = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);
            foreach (var bear in existingLocalBears)
            {
                DestroyImmediate(bear.gameObject);
            }
            localPlayers.Clear();

            // 初始化车辆为本地模式
            var cart = FindFirstObjectByType<CartController>();
            if (cart != null)
            {
                cart.InitializeLocalMode();
            }

            // 启动双人
            StartLocalMultiplayerWithBothPlayers();

            // 启动延迟清理协程，捕获任何延迟生成的网络玩家
            StartCoroutine(DelayedCleanup());
        }

        private void CleanupNetworkBears()
        {
            var existingNetBears = FindObjectsByType<BearController>(FindObjectsSortMode.None);
            foreach (var bear in existingNetBears)
            {
                DestroyImmediate(bear.gameObject);
                Debug.Log("[LocalPlayerManager] 立即清理了一个网络 Bear");
            }
        }

        private void HideConnectionPanel()
        {
            // 方法1: 销毁 NetworkUIManager 组件（防止它调用 ShowConnectionPanel）
            var nui = FindFirstObjectByType<BearCar.Network.NetworkUIManager>();
            if (nui != null)
            {
                DestroyImmediate(nui);
                Debug.Log("[LocalPlayerManager] NetworkUIManager 组件已销毁");
            }

            // 方法2: 查找并隐藏所有名为 ConnectionPanel 的对象
            var allObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allObjects)
            {
                if (t.name == "ConnectionPanel" || t.name == "InGamePanel")
                {
                    t.gameObject.SetActive(false);
                    Debug.Log($"[LocalPlayerManager] {t.name} 已隐藏");
                }
            }
        }

        private System.Collections.IEnumerator DelayedCleanup()
        {
            // 等待几帧后再次清理，确保没有遗漏
            yield return null;
            yield return null;
            yield return null;

            // 再次隐藏连接面板（因为 NetworkUIManager.Start() 可能会显示它）
            HideConnectionPanel();

            var strayBears = FindObjectsByType<BearController>(FindObjectsSortMode.None);
            foreach (var bear in strayBears)
            {
                Destroy(bear.gameObject);
                Debug.Log("[LocalPlayerManager] 延迟清理了一个网络 Bear");
            }
        }

        private void DisableNetworkManager()
        {
            // 完全销毁 NetworkManager 及其 GameObject
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                // 先 Shutdown 再销毁
                if (nm.IsListening)
                {
                    nm.Shutdown();
                }

                // 立即销毁整个 NetworkManager GameObject（包含 NetworkGameManager）
                DestroyImmediate(nm.gameObject);
                Debug.Log("[LocalPlayerManager] NetworkManager GameObject 已立即销毁");
            }

            // 隐藏网络连接面板
            HideConnectionPanel();

            // 清理可能已经生成的网络玩家 Bear
            CleanupNetworkBears();
        }

        /// <summary>
        /// 启动本地多人模式（同时生成两个玩家）
        /// </summary>
        public void StartLocalMultiplayerWithBothPlayers()
        {
            if (isLocalMultiplayerActive) return;

            isLocalMultiplayerActive = true;

            // 生成两个玩家
            JoinLocalPlayer(0);
            JoinLocalPlayer(1);

            Debug.Log("[LocalPlayerManager] 本地双人模式已启动");
        }

        /// <summary>
        /// 启动本地多人模式（单人，等待第二人加入）
        /// </summary>
        public void StartLocalMultiplayer()
        {
            if (isLocalMultiplayerActive) return;

            isLocalMultiplayerActive = true;

            // 生成 Player 1
            JoinLocalPlayer(0);

            Debug.Log("[LocalPlayerManager] 本地多人模式已启动，Player 2 按 Enter 加入");
        }

        /// <summary>
        /// 停止本地多人模式
        /// </summary>
        public void StopLocalMultiplayer()
        {
            if (!isLocalMultiplayerActive) return;

            // 移除所有本地玩家
            for (int i = localPlayers.Count - 1; i >= 0; i--)
            {
                LeaveLocalPlayer(localPlayers[i].PlayerIndex);
            }

            isLocalMultiplayerActive = false;
            Debug.Log("[LocalPlayerManager] 本地多人模式已停止");
        }

        /// <summary>
        /// 玩家加入
        /// </summary>
        public void JoinLocalPlayer(int playerIndex)
        {
            if (HasPlayer(playerIndex))
            {
                Debug.LogWarning($"[LocalPlayerManager] Player {playerIndex} 已存在");
                return;
            }

            if (localPlayers.Count >= maxLocalPlayers)
            {
                Debug.LogWarning("[LocalPlayerManager] 已达到最大玩家数");
                return;
            }

            // 创建玩家对象
            GameObject playerObj = new GameObject($"LocalBear_P{playerIndex + 1}");

            // 设置位置
            playerObj.transform.position = playerIndex == 0 ? player1SpawnPos : player2SpawnPos;

            // 添加组件
            var controller = playerObj.AddComponent<LocalBearController>();
            var inputHandler = playerObj.AddComponent<LocalBearInputHandler>();

            // 初始化
            controller.Initialize(playerIndex);
            inputHandler.Initialize(playerIndex);

            localPlayers.Add(controller);

            Debug.Log($"[LocalPlayerManager] Player {playerIndex + 1} 已加入");
        }

        /// <summary>
        /// 玩家退出
        /// </summary>
        public void LeaveLocalPlayer(int playerIndex)
        {
            var player = GetPlayer(playerIndex);
            if (player == null)
            {
                Debug.LogWarning($"[LocalPlayerManager] Player {playerIndex} 不存在");
                return;
            }

            localPlayers.Remove(player);
            Destroy(player.gameObject);

            Debug.Log($"[LocalPlayerManager] Player {playerIndex + 1} 已退出");
        }

        /// <summary>
        /// 检查玩家是否存在
        /// </summary>
        public bool HasPlayer(int playerIndex)
        {
            return GetPlayer(playerIndex) != null;
        }

        /// <summary>
        /// 获取指定玩家
        /// </summary>
        public LocalBearController GetPlayer(int playerIndex)
        {
            foreach (var player in localPlayers)
            {
                if (player.PlayerIndex == playerIndex)
                    return player;
            }
            return null;
        }

        /// <summary>
        /// 获取所有本地玩家
        /// </summary>
        public List<LocalBearController> GetAllLocalPlayers()
        {
            return new List<LocalBearController>(localPlayers);
        }
    }
}
