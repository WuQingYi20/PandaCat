#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using BearCar.Core;
using BearCar.Player;
using BearCar.Cart;
using BearCar.Network;
using BearCar.UI;

namespace BearCar.Editor
{
    public class BearCarSceneSetup : EditorWindow
    {
        [MenuItem("BearCar/Setup Scene (一键配置场景)")]
        public static void ShowWindow()
        {
            GetWindow<BearCarSceneSetup>("Bear Car Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Bear Car 场景配置工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "点击下方按钮将自动创建:\n" +
                "• GameConfig 配置文件\n" +
                "• Bear 玩家预制体\n" +
                "• Cart 车辆预制体\n" +
                "• NetworkManager\n" +
                "• 坡道场景\n" +
                "• UI 界面",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("1. 创建 GameConfig", GUILayout.Height(30)))
            {
                CreateGameConfig();
            }

            if (GUILayout.Button("2. 创建 Bear Prefab", GUILayout.Height(30)))
            {
                CreateBearPrefab();
            }

            if (GUILayout.Button("3. 创建 Cart Prefab", GUILayout.Height(30)))
            {
                CreateCartPrefab();
            }

            if (GUILayout.Button("4. 设置 NetworkManager", GUILayout.Height(30)))
            {
                SetupNetworkManager();
            }

            if (GUILayout.Button("5. 创建坡道场景", GUILayout.Height(30)))
            {
                CreateSlopeScene();
            }

            if (GUILayout.Button("6. 创建 UI", GUILayout.Height(30)))
            {
                CreateUI();
            }

            if (GUILayout.Button("7. 设置摄像机跟随", GUILayout.Height(30)))
            {
                SetupCamera();
            }

            GUILayout.Space(20);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("一键全部设置", GUILayout.Height(50)))
            {
                SetupAll();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("配置物理层 (Layers)", GUILayout.Height(30)))
            {
                SetupLayers();
            }
            GUI.backgroundColor = Color.white;
        }

        private static void SetupAll()
        {
            CreateGameConfig();
            CreateBearPrefab();
            CreateCartPrefab();
            SetupNetworkManager();
            CreateSlopeScene();
            CreateUI();
            SetupCamera();

            Debug.Log("Bear Car 场景配置完成!");
            EditorUtility.DisplayDialog("完成", "Bear Car 场景配置完成!\n\n请手动配置物理层 (点击 '配置物理层' 按钮查看说明)", "OK");
        }

        private static void CreateGameConfig()
        {
            string path = "Assets/Resources/GameConfig.asset";

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (existing != null)
            {
                Debug.Log("GameConfig 已存在");
                Selection.activeObject = existing;
                return;
            }

            var config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            Debug.Log("GameConfig 创建成功: " + path);
        }

        private static void CreateBearPrefab()
        {
            string prefabPath = "Assets/Prefabs/Bear.prefab";

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // 创建 Bear GameObject
            GameObject bear = new GameObject("Bear");

            // 添加 NetworkObject
            bear.AddComponent<NetworkObject>();

            // 添加 NetworkTransform
            var networkTransform = bear.AddComponent<Unity.Netcode.Components.NetworkTransform>();

            // 添加 Rigidbody2D
            var rb = bear.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // 添加 BoxCollider2D
            var collider = bear.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 1f);

            // 添加 SpriteRenderer (方块占位)
            var sr = bear.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite(Color.blue);
            sr.sortingOrder = 1;

            // 添加 PlayerInput
            var playerInput = bear.AddComponent<PlayerInput>();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                playerInput.actions = inputActions;
                playerInput.defaultActionMap = "Player";
            }

            // 添加游戏脚本
            bear.AddComponent<BearController>();
            bear.AddComponent<BearInputHandler>();
            bear.AddComponent<StaminaSystem>();

            // 设置 Layer
            bear.layer = LayerMask.NameToLayer("Default");

            // 保存为 Prefab
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            PrefabUtility.SaveAsPrefabAsset(bear, prefabPath);
            DestroyImmediate(bear);

            Debug.Log("Bear Prefab 创建成功: " + prefabPath);
        }

        private static void CreateCartPrefab()
        {
            string prefabPath = "Assets/Prefabs/Cart.prefab";

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // 创建 Cart GameObject
            GameObject cart = new GameObject("Cart");

            // 添加 NetworkObject
            cart.AddComponent<NetworkObject>();

            // 添加 Rigidbody2D
            var rb = cart.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.mass = 10f;
            rb.linearDamping = 0.5f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // 添加 BoxCollider2D
            var collider = cart.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 1f);

            // 添加 SpriteRenderer
            var sr = cart.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite(Color.yellow, 2f, 1f);
            sr.sortingOrder = 0;

            // 添加 CartController
            cart.AddComponent<CartController>();

            // 创建 PushZone 子对象
            GameObject pushZone = new GameObject("PushZone");
            pushZone.transform.SetParent(cart.transform);
            pushZone.transform.localPosition = new Vector3(-1.5f, 0, 0); // 车后方

            var pushCollider = pushZone.AddComponent<BoxCollider2D>();
            pushCollider.isTrigger = true;
            pushCollider.size = new Vector2(1.5f, 2f);

            pushZone.AddComponent<CartPushZone>();

            // 保存为 Prefab
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            PrefabUtility.SaveAsPrefabAsset(cart, prefabPath);
            DestroyImmediate(cart);

            Debug.Log("Cart Prefab 创建成功: " + prefabPath);
        }

        private static void SetupNetworkManager()
        {
            // 检查是否已存在
            var existing = FindFirstObjectByType<NetworkManager>();
            if (existing != null)
            {
                Debug.Log("NetworkManager 已存在");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // 创建 NetworkManager
            GameObject nmObj = new GameObject("NetworkManager");

            var nm = nmObj.AddComponent<NetworkManager>();
            nmObj.AddComponent<UnityTransport>();

            var gameManager = nmObj.AddComponent<NetworkGameManager>();

            // 加载 Bear Prefab
            var bearPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bear.prefab");
            if (bearPrefab != null)
            {
                // 设置 NetworkPrefabs
                var networkPrefabsList = new Unity.Netcode.NetworkPrefabsList();
                networkPrefabsList.Add(new NetworkPrefab { Prefab = bearPrefab });

                // 通过 SerializedObject 设置
                var so = new SerializedObject(nm);
                var prefabsListProp = so.FindProperty("NetworkConfig").FindPropertyRelative("Prefabs");

                // 使用反射或手动添加
                Debug.Log("请手动将 Bear Prefab 添加到 NetworkManager 的 NetworkPrefabs 列表中");
            }

            // 创建生成点（在小车后面，小车在 -4）
            GameObject spawnPoint1 = new GameObject("SpawnPoint1");
            spawnPoint1.transform.position = new Vector3(-6f, 0, 0); // 熊1 在车后
            spawnPoint1.transform.SetParent(nmObj.transform);

            GameObject spawnPoint2 = new GameObject("SpawnPoint2");
            spawnPoint2.transform.position = new Vector3(-7f, 0, 0); // 熊2 在熊1后
            spawnPoint2.transform.SetParent(nmObj.transform);

            // 设置 GameManager 引用 (通过 SerializedObject)
            var gmSo = new SerializedObject(gameManager);
            var bearPrefabProp = gmSo.FindProperty("bearPrefab");
            var spawnPointsProp = gmSo.FindProperty("spawnPoints");

            if (bearPrefab != null)
            {
                bearPrefabProp.objectReferenceValue = bearPrefab;
            }

            spawnPointsProp.arraySize = 2;
            spawnPointsProp.GetArrayElementAtIndex(0).objectReferenceValue = spawnPoint1.transform;
            spawnPointsProp.GetArrayElementAtIndex(1).objectReferenceValue = spawnPoint2.transform;

            gmSo.ApplyModifiedProperties();

            Selection.activeGameObject = nmObj;
            Debug.Log("NetworkManager 创建成功");
        }

        private static void CreateSlopeScene()
        {
            // 删除旧的地面对象
            var oldGround = GameObject.Find("Ground");
            if (oldGround != null) DestroyImmediate(oldGround);

            var oldGoal = GameObject.Find("Goal");
            if (oldGoal != null) DestroyImmediate(oldGoal);

            // 创建地面父对象
            GameObject ground = new GameObject("Ground");

            // ========== 场景布局 ==========
            //
            //  [熊1][熊2][车]  ----平地----  /坡道/  [终点]
            //       -6   -4      -2~4         4~12      14
            //
            // ================================

            // 平地段（起点区域，较长）
            GameObject flatGround = new GameObject("FlatGround");
            flatGround.transform.SetParent(ground.transform);
            flatGround.transform.position = new Vector3(-2, -1.5f, 0);

            var flatSr = flatGround.AddComponent<SpriteRenderer>();
            flatSr.sprite = CreatePlaceholderSprite(new Color(0.4f, 0.3f, 0.2f), 10f, 1f);
            flatSr.sortingOrder = -1;

            var flatCollider = flatGround.AddComponent<BoxCollider2D>();
            flatCollider.size = new Vector2(10f, 1f);

            // 坡道段（在平地右边）
            GameObject slope = new GameObject("Slope");
            slope.transform.SetParent(ground.transform);
            slope.transform.position = new Vector3(8, 0.3f, 0);
            slope.transform.rotation = Quaternion.Euler(0, 0, 15); // 15度坡

            var slopeSr = slope.AddComponent<SpriteRenderer>();
            slopeSr.sprite = CreatePlaceholderSprite(new Color(0.5f, 0.4f, 0.3f), 10f, 0.5f);
            slopeSr.sortingOrder = -1;

            var slopeCollider = slope.AddComponent<BoxCollider2D>();
            slopeCollider.size = new Vector2(10f, 0.5f);

            // 设置 Layer (如果存在)
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                flatGround.layer = groundLayer;
                slope.layer = groundLayer;
            }

            // 创建终点（坡道顶端）
            GameObject goal = new GameObject("Goal");
            goal.transform.position = new Vector3(14, 4, 0);
            var goalSr = goal.AddComponent<SpriteRenderer>();
            goalSr.sprite = CreatePlaceholderSprite(Color.green, 1f, 3f);
            goalSr.sortingOrder = -1;

            // 删除旧的 Cart
            var oldCart = GameObject.Find("Cart");
            if (oldCart != null) DestroyImmediate(oldCart);

            // 在平地上创建 Cart
            var cartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cart.prefab");
            if (cartPrefab != null)
            {
                var cart = (GameObject)PrefabUtility.InstantiatePrefab(cartPrefab);
                cart.transform.position = new Vector3(-4, 0, 0); // 平地上，靠左
                cart.name = "Cart";
            }

            Debug.Log("坡道场景创建成功 - 小车在平地上");
        }

        private static void CreateUI()
        {
            // 检查是否已存在 Canvas
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            GameObject canvasObj;

            if (existingCanvas != null)
            {
                canvasObj = existingCanvas.gameObject;
            }
            else
            {
                canvasObj = new GameObject("Canvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 创建连接面板
            GameObject connectionPanel = new GameObject("ConnectionPanel");
            connectionPanel.transform.SetParent(canvasObj.transform, false);

            var panelRect = connectionPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(300, 200);

            var panelImage = connectionPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            // Host 按钮
            GameObject hostBtn = CreateButton("HostButton", "Host Game", connectionPanel.transform, new Vector2(0, 50));

            // Client 按钮
            GameObject clientBtn = CreateButton("ClientButton", "Join Game", connectionPanel.transform, new Vector2(0, -10));

            // IP 输入框
            GameObject inputField = CreateInputField("AddressInput", "127.0.0.1", connectionPanel.transform, new Vector2(0, -70));

            // 创建游戏中面板
            GameObject inGamePanel = new GameObject("InGamePanel");
            inGamePanel.transform.SetParent(canvasObj.transform, false);
            inGamePanel.SetActive(false);

            var igPanelRect = inGamePanel.AddComponent<RectTransform>();
            igPanelRect.anchorMin = new Vector2(0, 1);
            igPanelRect.anchorMax = new Vector2(0, 1);
            igPanelRect.pivot = new Vector2(0, 1);
            igPanelRect.anchoredPosition = new Vector2(10, -10);
            igPanelRect.sizeDelta = new Vector2(200, 100);

            // 状态文本
            GameObject statusText = CreateText("StatusText", "Connected", inGamePanel.transform, new Vector2(0, -20));

            // 断开按钮
            GameObject disconnectBtn = CreateButton("DisconnectButton", "Disconnect", inGamePanel.transform, new Vector2(0, -60));

            // 添加 NetworkUIManager
            var networkUI = canvasObj.AddComponent<NetworkUIManager>();

            // 设置引用
            var so = new SerializedObject(networkUI);
            so.FindProperty("connectionPanel").objectReferenceValue = connectionPanel;
            so.FindProperty("hostButton").objectReferenceValue = hostBtn.GetComponent<Button>();
            so.FindProperty("clientButton").objectReferenceValue = clientBtn.GetComponent<Button>();
            so.FindProperty("addressInput").objectReferenceValue = inputField.GetComponent<TMP_InputField>();
            so.FindProperty("inGamePanel").objectReferenceValue = inGamePanel;
            so.FindProperty("disconnectButton").objectReferenceValue = disconnectBtn.GetComponent<Button>();
            so.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            // 添加 DebugUI
            var debugUI = new GameObject("DebugUI");
            debugUI.transform.SetParent(canvasObj.transform, false);
            debugUI.AddComponent<DebugUI>();

            // 创建 EventSystem (如果不存在)
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            Debug.Log("UI 创建成功");
        }

        private static void SetupCamera()
        {
            // 找到主摄像机
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("未找到主摄像机，请确保场景中有 MainCamera");
                return;
            }

            // 检查是否已有 CameraFollow
            var existingFollow = mainCamera.GetComponent<CameraFollow>();
            if (existingFollow != null)
            {
                Debug.Log("CameraFollow 已存在于主摄像机上");
                return;
            }

            // 添加 CameraFollow 组件
            var cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();

            // 设置默认参数
            var so = new SerializedObject(cameraFollow);
            so.FindProperty("smoothSpeed").floatValue = 5f;
            so.FindProperty("offset").vector3Value = new Vector3(3, 2, -10); // 稍微向前看
            so.ApplyModifiedProperties();

            Debug.Log("CameraFollow 已添加到主摄像机");
        }

        private static void SetupLayers()
        {
            EditorUtility.DisplayDialog(
                "配置物理层",
                "请手动在 Edit > Project Settings > Tags and Layers 中添加以下层:\n\n" +
                "• Layer 8: Bear\n" +
                "• Layer 9: Cart\n" +
                "• Layer 10: Ground\n" +
                "• Layer 11: PushZone\n\n" +
                "然后在 Edit > Project Settings > Physics 2D 中配置碰撞矩阵:\n" +
                "• Bear 与 Ground, Cart, PushZone 碰撞\n" +
                "• Cart 与 Ground 碰撞\n" +
                "• PushZone 只与 Bear 碰撞 (Trigger)",
                "OK");

            // 打开 Tags and Layers 设置
            SettingsService.OpenProjectSettings("Project/Tags and Layers");
        }

        #region Helper Methods

        private static Sprite CreatePlaceholderSprite(Color color, float width = 1f, float height = 1f)
        {
            int w = Mathf.Max(1, (int)(width * 32));
            int h = Mathf.Max(1, (int)(height * 32));

            Texture2D tex = new Texture2D(w, h);
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        private static GameObject CreateButton(string name, string text, Transform parent, Vector2 position)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(160, 40);

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f);

            btnObj.AddComponent<Button>();

            // 文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18;

            return btnObj;
        }

        private static GameObject CreateInputField(string name, string placeholder, Transform parent, Vector2 position)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);

            var rect = inputObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 35);

            var image = inputObj.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f);

            // Text Area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputObj.transform, false);
            var taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(10, 5);
            taRect.offsetMax = new Vector2(-10, -5);

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            var phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;

            var phTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
            phTmp.text = placeholder;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.5f, 0.5f, 0.5f);
            phTmp.fontSize = 16;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            var txtRect = textObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var txtTmp = textObj.AddComponent<TextMeshProUGUI>();
            txtTmp.fontSize = 16;

            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = taRect;
            inputField.textComponent = txtTmp;
            inputField.placeholder = phTmp;

            return inputObj;
        }

        private static GameObject CreateText(string name, string text, Transform parent, Vector2 position)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(180, 30);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;

            return textObj;
        }

        #endregion
    }
}
#endif
