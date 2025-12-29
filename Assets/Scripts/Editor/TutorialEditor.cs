#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BearCar.UI;

namespace BearCar.Editor
{
    /// <summary>
    /// 教程系统编辑器工具
    /// </summary>
    public static class TutorialEditor
    {
        [MenuItem("BearCar/教程/重置教程进度")]
        public static void ResetTutorialProgress()
        {
            PlayerPrefs.DeleteKey("Tutorial_P1_Grabbed");
            PlayerPrefs.DeleteKey("Tutorial_P2_Grabbed");
            PlayerPrefs.Save();

            // 如果正在运行，同时重置运行时状态
            var tutorial = Object.FindFirstObjectByType<TutorialHintUI>();
            if (tutorial != null)
            {
                tutorial.ResetTutorial();
            }

            Debug.Log("[Tutorial] 教程进度已重置，下次游戏将显示新手提示");
            EditorUtility.DisplayDialog("教程重置", "教程进度已重置！\n下次运行游戏时将显示按键提示。", "确定");
        }

        [MenuItem("BearCar/教程/强制显示教程提示")]
        public static void ForceShowTutorial()
        {
            var tutorial = Object.FindFirstObjectByType<TutorialHintUI>();
            if (tutorial != null)
            {
                tutorial.ForceShowTutorial();
                Debug.Log("[Tutorial] 已强制启用教程提示");
            }
            else
            {
                Debug.LogWarning("[Tutorial] 未找到 TutorialHintUI，请先运行游戏");
            }
        }

        [MenuItem("BearCar/教程/禁用教程提示")]
        public static void DisableTutorial()
        {
            var tutorial = Object.FindFirstObjectByType<TutorialHintUI>();
            if (tutorial != null)
            {
                tutorial.enableTutorial = false;
                Debug.Log("[Tutorial] 已禁用教程提示");
            }
            else
            {
                Debug.LogWarning("[Tutorial] 未找到 TutorialHintUI，请先运行游戏");
            }
        }
    }
}
#endif
