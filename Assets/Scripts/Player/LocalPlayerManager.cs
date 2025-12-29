using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using BearCar.Cart;
using BearCar.Item;
using BearCar.UI;

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

        [Header("Spawn Points - 可拖入场景中的空物体")]
        [Tooltip("P1(绿熊) 生成点 - 可拖入场景中的 Transform")]
        [SerializeField] private Transform player1SpawnPoint;
        [Tooltip("P2(红熊) 生成点 - 可拖入场景中的 Transform")]
        [SerializeField] private Transform player2SpawnPoint;

        [Header("Spawn Points - 备用坐标 (无 Transform 时使用)")]
        [SerializeField] private Vector3 player1SpawnPos = new Vector3(-7.5f, 1f, 0f);
        [SerializeField] private Vector3 player2SpawnPos = new Vector3(-6f, 1f, 0f);

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
            // 自动查找场景中的生成点
            FindSpawnPointsInScene();

            // 自动启动本地双人模式
            if (autoStartTwoPlayers && !hasInitialized)
            {
                hasInitialized = true;
                InitializeLocalGame();
            }
        }

        /// <summary>
        /// 自动查找场景中的生成点 (通过名称或标签)
        /// </summary>
        private void FindSpawnPointsInScene()
        {
            // 如果没有手动指定，尝试通过名称查找
            if (player1SpawnPoint == null)
            {
                var p1 = GameObject.Find("P1SpawnPoint") ?? GameObject.Find("Player1Spawn") ?? GameObject.Find("GreenBearSpawn");
                if (p1 != null) player1SpawnPoint = p1.transform;
            }

            if (player2SpawnPoint == null)
            {
                var p2 = GameObject.Find("P2SpawnPoint") ?? GameObject.Find("Player2Spawn") ?? GameObject.Find("RedBearSpawn");
                if (p2 != null) player2SpawnPoint = p2.transform;
            }

            // 输出日志
            string p1Info = player1SpawnPoint != null ? player1SpawnPoint.name : $"备用坐标 {player1SpawnPos}";
            string p2Info = player2SpawnPoint != null ? player2SpawnPoint.name : $"备用坐标 {player2SpawnPos}";
            Debug.Log($"[LocalPlayerManager] 生成点: P1={p1Info}, P2={p2Info}");
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

            // 初始化道具系统
            InitializeItemSystem();

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

        private void InitializeItemSystem()
        {
            // 创建共享背包（如果不存在）
            if (SharedInventory.Instance == null)
            {
                var inventoryObj = new GameObject("ItemManager");
                inventoryObj.AddComponent<SharedInventory>();
                inventoryObj.AddComponent<InventoryUI>();
                inventoryObj.AddComponent<ItemEffectHandler>();
                inventoryObj.AddComponent<ItemEffectLog>();
                inventoryObj.AddComponent<ComboHintUI>(); // 添加组合提示UI
                inventoryObj.AddComponent<TutorialHintUI>(); // 添加新手教程提示
                Debug.Log("[LocalPlayerManager] 道具系统已创建");

                // 添加测试道具以验证UI工作
                StartCoroutine(AddTestItemsDelayed());
            }
            else
            {
                var inventoryObj = SharedInventory.Instance.gameObject;

                // 确保 InventoryUI 存在
                if (inventoryObj.GetComponent<InventoryUI>() == null)
                {
                    inventoryObj.AddComponent<InventoryUI>();
                    Debug.Log("[LocalPlayerManager] InventoryUI 已添加到现有道具系统");
                }

                // 确保 ComboHintUI 存在
                if (inventoryObj.GetComponent<ComboHintUI>() == null)
                {
                    inventoryObj.AddComponent<ComboHintUI>();
                    Debug.Log("[LocalPlayerManager] ComboHintUI 已添加到现有道具系统");
                }

                // 确保 TutorialHintUI 存在
                if (inventoryObj.GetComponent<TutorialHintUI>() == null)
                {
                    inventoryObj.AddComponent<TutorialHintUI>();
                    Debug.Log("[LocalPlayerManager] TutorialHintUI 已添加到现有道具系统");
                }
            }

            // 创建道具生成器（如果不存在）
            if (FindFirstObjectByType<ItemSpawner>() == null)
            {
                var spawnerObj = new GameObject("ItemSpawner");
                spawnerObj.transform.position = new Vector3(0, 2, 0); // 场景中间上方
                var spawner = spawnerObj.AddComponent<ItemSpawner>();
                spawner.spawnInterval = 15f;
                spawner.maxItems = 3;
                spawner.spawnRadius = 8f;
                Debug.Log("[LocalPlayerManager] 道具生成器已创建");
            }
        }

        private System.Collections.IEnumerator AddTestItemsDelayed()
        {
            // 等待一帧确保SharedInventory已完全初始化
            yield return null;

            if (SharedInventory.Instance != null)
            {
                // 尝试加载曼妥思和可乐来测试combo
                var mentos = Resources.Load<ItemData>("Items/Mentos");
                var cola = Resources.Load<ItemData>("Items/Cola");

                if (mentos != null && cola != null)
                {
                    // 确保combo配置正确
                    mentos.isComboTrigger = true;
                    mentos.comboPartner = cola;
                    mentos.comboResultType = ItemType.RocketBoost;

                    cola.isComboTrigger = true;
                    cola.comboPartner = mentos;
                    cola.comboResultType = ItemType.RocketBoost;

                    // 添加到背包
                    SharedInventory.Instance.AddItem(mentos, 1);
                    SharedInventory.Instance.AddItem(cola, 1);
                    Debug.Log("[LocalPlayerManager] 已添加曼妥思和可乐到背包，可以测试combo!");
                    Debug.Log("[LocalPlayerManager] Combo使用方法: 绿熊选曼妥思，红熊选可乐，同时按 Tab 和 / 键触发!");
                }
                else
                {
                    // 如果没有曼妥思/可乐，创建测试道具
                    var testItem = ScriptableObject.CreateInstance<ItemData>();
                    testItem.itemName = "测试药水";
                    testItem.description = "用于测试的道具";
                    testItem.itemType = ItemType.StaminaRecover;
                    testItem.shape = ItemShape.Circle;
                    testItem.itemColor = new Color(0.5f, 0.8f, 1f);
                    testItem.effectValue = 2f;
                    testItem.stackable = true;
                    testItem.maxStack = 5;

                    SharedInventory.Instance.AddItem(testItem, 2);
                    Debug.Log("[LocalPlayerManager] 添加了测试道具到背包");
                }
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

            // 设置位置 - 优先使用场景中的 Transform，否则用备用坐标
            Vector3 spawnPos;
            if (playerIndex == 0)
            {
                spawnPos = player1SpawnPoint != null ? player1SpawnPoint.position : player1SpawnPos;
            }
            else
            {
                spawnPos = player2SpawnPoint != null ? player2SpawnPoint.position : player2SpawnPos;
            }
            playerObj.transform.position = spawnPos;

            // 添加组件
            var controller = playerObj.AddComponent<LocalBearController>();
            var inputHandler = playerObj.AddComponent<LocalBearInputHandler>();

            // 初始化
            controller.Initialize(playerIndex);
            inputHandler.Initialize(playerIndex);

            localPlayers.Add(controller);

            string bearName = playerIndex == 0 ? "绿熊" : "红熊";
            Debug.Log($"[LocalPlayerManager] Player {playerIndex + 1} ({bearName}) 已创建，GameObject: {playerObj.name}, PlayerIndex: {playerIndex}");
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

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器可视化 - 显示生成点位置
        /// </summary>
        private void OnDrawGizmos()
        {
            // P1 生成点 (绿色)
            Vector3 p1Pos = player1SpawnPoint != null ? player1SpawnPoint.position : player1SpawnPos;
            Gizmos.color = new Color(0.3f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(p1Pos, 0.5f);
            Gizmos.DrawIcon(p1Pos + Vector3.up * 0.8f, "d_PlayButton", true);
            UnityEditor.Handles.Label(p1Pos + Vector3.up * 1.2f, "P1 (WASD)");

            // P2 生成点 (红色)
            Vector3 p2Pos = player2SpawnPoint != null ? player2SpawnPoint.position : player2SpawnPos;
            Gizmos.color = new Color(0.9f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(p2Pos, 0.5f);
            Gizmos.DrawIcon(p2Pos + Vector3.up * 0.8f, "d_PlayButton", true);
            UnityEditor.Handles.Label(p2Pos + Vector3.up * 1.2f, "P2 (Arrows)");

            // 连接线
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p1Pos, p2Pos);
        }

        /// <summary>
        /// 创建生成点物体
        /// </summary>
        [UnityEditor.MenuItem("BearCar/创建玩家生成点")]
        public static void CreateSpawnPoints()
        {
            // P1 生成点
            if (GameObject.Find("P1SpawnPoint") == null)
            {
                var p1 = new GameObject("P1SpawnPoint");
                p1.transform.position = new Vector3(-7.5f, 1f, 0f);
                UnityEditor.Undo.RegisterCreatedObjectUndo(p1, "Create P1 Spawn Point");
            }

            // P2 生成点
            if (GameObject.Find("P2SpawnPoint") == null)
            {
                var p2 = new GameObject("P2SpawnPoint");
                p2.transform.position = new Vector3(-6f, 1f, 0f);
                UnityEditor.Undo.RegisterCreatedObjectUndo(p2, "Create P2 Spawn Point");
            }

            Debug.Log("[LocalPlayerManager] 玩家生成点已创建，可在 Scene 视图中拖动调整位置");
        }
#endif
    }
}
