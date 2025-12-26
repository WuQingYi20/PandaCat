#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.Level;

namespace BearCar.Editor
{
    public class PortalSetupEditor : EditorWindow
    {
        [MenuItem("BearCar/关卡工具/创建传送门对")]
        public static void CreatePortalPair()
        {
            // 创建父对象
            GameObject portalPair = new GameObject("PortalPair");
            Undo.RegisterCreatedObjectUndo(portalPair, "Create Portal Pair");

            // 创建传送门 A
            GameObject portalA = CreatePortal("Portal_A", new Vector3(-3, 0, 0), new Color(0.3f, 0.7f, 1f, 0.8f));
            portalA.transform.SetParent(portalPair.transform);

            // 创建传送门 B
            GameObject portalB = CreatePortal("Portal_B", new Vector3(3, 0, 0), new Color(1f, 0.5f, 0.3f, 0.8f));
            portalB.transform.SetParent(portalPair.transform);

            // 互相链接
            var compA = portalA.GetComponent<Portal>();
            var compB = portalB.GetComponent<Portal>();
            compA.targetPoint = portalB.transform;
            compB.targetPoint = portalA.transform;
            compA.portalId = "Portal_A";
            compB.portalId = "Portal_B";

            // 选中
            Selection.activeGameObject = portalPair;
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("传送门对已创建! 拖动位置来调整传送门位置。");
        }

        [MenuItem("BearCar/关卡工具/创建单向传送门")]
        public static void CreateOneWayPortal()
        {
            GameObject parent = new GameObject("OneWayPortal");
            Undo.RegisterCreatedObjectUndo(parent, "Create One Way Portal");

            // 入口
            GameObject entrance = CreatePortal("Entrance", new Vector3(-3, 0, 0), new Color(0.3f, 1f, 0.3f, 0.8f));
            entrance.transform.SetParent(parent.transform);
            var entranceComp = entrance.GetComponent<Portal>();
            entranceComp.direction = PortalDirection.Bidirectional;

            // 出口（只出不进）
            GameObject exit = CreatePortal("Exit", new Vector3(3, 0, 0), new Color(1f, 0.3f, 0.3f, 0.8f));
            exit.transform.SetParent(parent.transform);
            var exitComp = exit.GetComponent<Portal>();
            exitComp.direction = PortalDirection.OneWayOut;

            // 链接
            entranceComp.targetPoint = exit.transform;

            Selection.activeGameObject = parent;
            Debug.Log("单向传送门已创建! 绿色入口 -> 红色出口");
        }

        [MenuItem("BearCar/关卡工具/创建合作传送门")]
        public static void CreateCoopPortal()
        {
            GameObject parent = new GameObject("CoopPortal");
            Undo.RegisterCreatedObjectUndo(parent, "Create Coop Portal");

            // 传送门
            GameObject portal = CreatePortal("Portal", Vector3.zero, new Color(0.8f, 0.5f, 1f, 0.8f));
            portal.transform.SetParent(parent.transform);
            var portalComp = portal.GetComponent<Portal>();
            portalComp.activation = PortalActivation.CoopTrigger;
            portalComp.requiredPlayers = 2;
            portalComp.activationDelay = 1f;

            // 出口
            GameObject exit = CreatePortal("Exit", new Vector3(5, 0, 0), new Color(0.5f, 1f, 0.8f, 0.8f));
            exit.transform.SetParent(parent.transform);
            exit.GetComponent<Portal>().direction = PortalDirection.OneWayOut;

            portalComp.targetPoint = exit.transform;

            // 压力板1
            GameObject plate1 = CreatePressurePlate("PressurePlate_P1", new Vector3(-2, -1, 0));
            plate1.transform.SetParent(parent.transform);
            plate1.GetComponent<PressurePlate>().linkedObjects = new[] { portal };

            // 压力板2
            GameObject plate2 = CreatePressurePlate("PressurePlate_P2", new Vector3(2, -1, 0));
            plate2.transform.SetParent(parent.transform);
            plate2.GetComponent<PressurePlate>().linkedObjects = new[] { portal };

            Selection.activeGameObject = parent;
            Debug.Log("合作传送门已创建! 需要两个玩家同时站在压力板上。");
        }

        [MenuItem("BearCar/关卡工具/创建加速传送门")]
        public static void CreateSpeedBoostPortal()
        {
            GameObject parent = new GameObject("SpeedBoostPortal");
            Undo.RegisterCreatedObjectUndo(parent, "Create Speed Boost Portal");

            // 入口
            GameObject entrance = CreatePortal("SpeedPortal_In", new Vector3(-3, 0, 0), new Color(1f, 1f, 0.3f, 0.8f));
            entrance.transform.SetParent(parent.transform);
            var entranceComp = entrance.GetComponent<Portal>();
            entranceComp.preserveMomentum = true;
            entranceComp.momentumMultiplier = 2f;
            entranceComp.momentumDirection = MomentumDirection.TowardsTarget;

            // 出口
            GameObject exit = CreatePortal("SpeedPortal_Out", new Vector3(3, 0, 0), new Color(1f, 0.8f, 0.3f, 0.8f));
            exit.transform.SetParent(parent.transform);
            exit.GetComponent<Portal>().direction = PortalDirection.OneWayOut;

            entranceComp.targetPoint = exit.transform;

            Selection.activeGameObject = parent;
            Debug.Log("加速传送门已创建! 传送后速度 x2，方向朝向出口。");
        }

        [MenuItem("BearCar/关卡工具/创建压力板")]
        public static void CreatePressurePlateMenu()
        {
            GameObject plate = CreatePressurePlate("PressurePlate", Vector3.zero);
            Undo.RegisterCreatedObjectUndo(plate, "Create Pressure Plate");
            Selection.activeGameObject = plate;
        }

        private static GameObject CreatePortal(string name, Vector3 localPos, Color color)
        {
            GameObject portal = new GameObject(name);
            portal.transform.localPosition = localPos;

            var comp = portal.AddComponent<Portal>();
            comp.portalColor = color;
            comp.activeColor = new Color(color.r, color.g + 0.2f, color.b, 0.9f);

            return portal;
        }

        private static GameObject CreatePressurePlate(string name, Vector3 localPos)
        {
            GameObject plate = new GameObject(name);
            plate.transform.localPosition = localPos;
            plate.AddComponent<PressurePlate>();
            return plate;
        }

        [MenuItem("BearCar/关卡工具/保存为 Prefab")]
        public static void SaveAsPrefab()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择要保存的对象", "OK");
                return;
            }

            // 确保 Prefabs/Level 文件夹存在
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Level"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Level");
            }

            string path = $"Assets/Prefabs/Level/{selected.name}.prefab";

            // 检查是否已存在
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                if (!EditorUtility.DisplayDialog("覆盖确认", $"Prefab '{selected.name}' 已存在，是否覆盖?", "覆盖", "取消"))
                {
                    return;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(selected, path);
            Debug.Log($"Prefab 已保存到: {path}");
        }
    }
}
#endif
