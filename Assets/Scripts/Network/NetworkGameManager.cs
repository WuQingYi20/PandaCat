using System.Linq;
using Unity.Netcode;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

namespace BearCar.Network
{
    public class NetworkGameManager : MonoBehaviour
    {
        [SerializeField] private GameObject bearPrefab;
        [SerializeField] private Transform[] spawnPoints;

        [Header("MPPM Settings")]
        [Tooltip("在 MPPM 模式下自动启动网络")]
        [SerializeField] private bool autoStartInMPPM = true;

        private NetworkManager networkManager;

        private void Start()
        {
            networkManager = NetworkManager.Singleton;

            if (networkManager != null)
            {
                networkManager.OnServerStarted += OnServerStarted;
                networkManager.OnClientConnectedCallback += OnClientConnected;
                networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }

            // MPPM 自动启动
            if (autoStartInMPPM)
            {
                AutoStartForMPPM();
            }
        }

        private void AutoStartForMPPM()
        {
#if UNITY_EDITOR
            var mppmTag = CurrentPlayer.ReadOnlyTags();

            if (mppmTag.Contains("Server"))
            {
                // 主编辑器作为 Server/Host
                StartHost();
                Debug.Log("[MPPM] Auto-started as Host");
            }
            else if (mppmTag.Contains("Client"))
            {
                // 虚拟玩家作为 Client
                StartClient();
                Debug.Log("[MPPM] Auto-started as Client");
            }
            // 如果没有 MPPM 标签，不自动启动（使用 UI 手动连接）
#endif
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= OnServerStarted;
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        public void StartHost()
        {
            if (networkManager != null)
            {
                networkManager.StartHost();
                Debug.Log("Started as Host");
            }
        }

        public void StartClient(string address = "127.0.0.1", ushort port = 7777)
        {
            if (networkManager != null)
            {
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(address, port);
                }

                networkManager.StartClient();
                Debug.Log($"Connecting to {address}:{port}");
            }
        }

        public void Disconnect()
        {
            if (networkManager != null)
            {
                networkManager.Shutdown();
                Debug.Log("Disconnected");
            }
        }

        private void OnServerStarted()
        {
            Debug.Log("Server started successfully");
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected");

            if (networkManager.IsServer)
            {
                SpawnPlayerForClient(clientId);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected");
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (bearPrefab == null)
            {
                Debug.LogError("Bear prefab not assigned!");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
                if (spawnPoints[spawnIndex] != null)
                {
                    spawnPosition = spawnPoints[spawnIndex].position;
                }
            }

            var bear = Instantiate(bearPrefab, spawnPosition, Quaternion.identity);
            var networkObject = bear.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId);
                Debug.Log($"Spawned player for client {clientId} at {spawnPosition}");
            }
            else
            {
                Debug.LogError("Bear prefab missing NetworkObject component!");
                Destroy(bear);
            }
        }
    }
}
