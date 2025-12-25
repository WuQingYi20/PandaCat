using System.Collections.Generic;
using UnityEngine;

namespace BearCar.Player
{
    /// <summary>
    /// 本地多人管理器
    /// 管理本地玩家的加入和退出
    /// </summary>
    public class LocalPlayerManager : MonoBehaviour
    {
        public static LocalPlayerManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxLocalPlayers = 2;

        [Header("Spawn Points")]
        [SerializeField] private Vector3 player1SpawnPos = new Vector3(-6f, 1f, 0f);
        [SerializeField] private Vector3 player2SpawnPos = new Vector3(-7.5f, 1f, 0f);

        private List<LocalBearController> localPlayers = new List<LocalBearController>();
        private bool isLocalMultiplayerActive = false;

        public bool IsLocalMultiplayerActive => isLocalMultiplayerActive;
        public int LocalPlayerCount => localPlayers.Count;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!isLocalMultiplayerActive) return;

            // 检测 Player 2 动态加入 (Enter 键)
            if (localPlayers.Count < maxLocalPlayers)
            {
                if (Input.GetKeyDown(KeyCode.Return) && !HasPlayer(1))
                {
                    JoinLocalPlayer(1);
                }
            }
        }

        /// <summary>
        /// 启动本地多人模式
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
