#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using BearCar.Player;

namespace BearCar.Editor
{
    public class BearAnimationConfigEditor : EditorWindow
    {
        [MenuItem("BearCar/创建动画配置")]
        public static void CreateAnimationConfig()
        {
            // 创建配置文件
            string path = "Assets/Resources/BearAnimationConfig.asset";

            // 确保 Resources 文件夹存在
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<BearAnimationConfig>(path);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("BearAnimationConfig 已存在，已选中");
                return;
            }

            // 创建新配置
            var config = ScriptableObject.CreateInstance<BearAnimationConfig>();

            // 自动加载动画帧
            config.player1Idle = LoadSprites("Assets/Resources/Animation/Idle", "-1 ");
            config.player1Walk = LoadSprites("Assets/Resources/Animation/Walk", "-1 ");
            config.player1Push = LoadSprites("Assets/Resources/Animation/Push", "-1 ");

            config.player2Idle = LoadSprites("Assets/Resources/Animation/Idle", "-2 ");
            config.player2Walk = LoadSprites("Assets/Resources/Animation/Walk", "-2 ");
            config.player2Push = LoadSprites("Assets/Resources/Animation/Push", "-2 ");

            config.frameRate = 8f;
            config.player1Tint = Color.white;
            config.player2Tint = Color.white;

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"BearAnimationConfig 创建成功!\n" +
                      $"P1: Idle={config.player1Idle?.Length ?? 0}, Walk={config.player1Walk?.Length ?? 0}, Push={config.player1Push?.Length ?? 0}\n" +
                      $"P2: Idle={config.player2Idle?.Length ?? 0}, Walk={config.player2Walk?.Length ?? 0}, Push={config.player2Push?.Length ?? 0}");
        }

        private static Sprite[] LoadSprites(string folderPath, string playerSuffix)
        {
            // 获取文件夹中的所有资源
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            var sprites = guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                .Where(s => s.name.Contains(playerSuffix))
                .OrderBy(s => s.name)
                .ToArray();

            return sprites;
        }

        [MenuItem("BearCar/刷新动画配置")]
        public static void RefreshAnimationConfig()
        {
            string path = "Assets/Resources/BearAnimationConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<BearAnimationConfig>(path);

            if (config == null)
            {
                Debug.LogWarning("未找到 BearAnimationConfig，请先创建");
                CreateAnimationConfig();
                return;
            }

            // 重新加载动画帧
            config.player1Idle = LoadSprites("Assets/Resources/Animation/Idle", "-1 ");
            config.player1Walk = LoadSprites("Assets/Resources/Animation/Walk", "-1 ");
            config.player1Push = LoadSprites("Assets/Resources/Animation/Push", "-1 ");

            config.player2Idle = LoadSprites("Assets/Resources/Animation/Idle", "-2 ");
            config.player2Walk = LoadSprites("Assets/Resources/Animation/Walk", "-2 ");
            config.player2Push = LoadSprites("Assets/Resources/Animation/Push", "-2 ");

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            Debug.Log("BearAnimationConfig 已刷新!");
        }
    }
}
#endif
