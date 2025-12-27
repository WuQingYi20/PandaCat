using UnityEngine;
using UnityEngine.UI;
using BearCar.Item;
using System.Collections;

namespace BearCar.UI
{
    /// <summary>
    /// 组合道具提示UI - 当背包中有可组合的道具时显示明显提示
    /// </summary>
    public class ComboHintUI : MonoBehaviour
    {
        [Header("=== UI设置 ===")]
        [SerializeField] private float hintDuration = 5f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private Color hintColor = new Color(1f, 0.8f, 0.2f);

        [Header("=== 提示文本 ===")]
        [SerializeField] private string comboReadyText = "两人同时按使用键触发: {0} + {1} = 火箭推进!";
        [SerializeField] private string comboTriggeredText = "🚀 组合触发! {0} + {1}!";

        // 双人同时按键检测
        private float greenPressTime = -1f;
        private float redPressTime = -1f;
        private const float SIMULTANEOUS_THRESHOLD = 0.3f; // 0.3秒内算同时

        private GameObject hintPanel;
        private Text hintText;
        private Image hintBackground;
        private Image comboIcon1;
        private Image comboIcon2;

        private bool isShowingHint = false;
        private Coroutine pulseCoroutine;
        private Coroutine hideCoroutine;

        private ItemData currentItem1;
        private ItemData currentItem2;

        private void Start()
        {
            CreateHintUI();

            // 订阅事件
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                inventory.OnComboReady += OnComboReady;
                inventory.OnComboTriggered += OnComboTriggered;
                inventory.OnItemRemoved += OnItemRemoved;
            }
        }

        private void OnDestroy()
        {
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                inventory.OnComboReady -= OnComboReady;
                inventory.OnComboTriggered -= OnComboTriggered;
                inventory.OnItemRemoved -= OnItemRemoved;
            }
        }

        private void Update()
        {
            if (!isShowingHint) return;

            // 检测绿熊按键 (Tab)
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                greenPressTime = Time.time;
                CheckSimultaneousPress();
            }

            // 检测红熊按键 (/)
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                redPressTime = Time.time;
                CheckSimultaneousPress();
            }
        }

        private void CheckSimultaneousPress()
        {
            // 检查两个玩家是否在短时间内都按下了使用键
            if (greenPressTime < 0 || redPressTime < 0) return;

            float timeDiff = Mathf.Abs(greenPressTime - redPressTime);
            if (timeDiff <= SIMULTANEOUS_THRESHOLD)
            {
                TriggerCombo();
                // 重置按键时间
                greenPressTime = -1f;
                redPressTime = -1f;
            }
        }

        private void CreateHintUI()
        {
            // 查找或创建Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("ComboHintCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // 创建提示面板
            hintPanel = new GameObject("ComboHintPanel");
            hintPanel.transform.SetParent(canvas.transform, false);

            var rectTransform = hintPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.85f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.85f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(500, 80);
            rectTransform.anchoredPosition = Vector2.zero;

            // 背景
            hintBackground = hintPanel.AddComponent<Image>();
            hintBackground.color = new Color(0, 0, 0, 0.8f);

            // 添加Outline效果
            var outline = hintPanel.AddComponent<Outline>();
            outline.effectColor = hintColor;
            outline.effectDistance = new Vector2(3, 3);

            // 创建水平布局
            var layout = hintPanel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 道具图标1
            var icon1GO = CreateIconObject("ComboIcon1", hintPanel.transform);
            comboIcon1 = icon1GO.GetComponent<Image>();

            // 加号
            var plusGO = new GameObject("PlusSign");
            plusGO.transform.SetParent(hintPanel.transform, false);
            var plusText = plusGO.AddComponent<Text>();
            plusText.text = "+";
            plusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            plusText.fontSize = 36;
            plusText.color = Color.white;
            plusText.alignment = TextAnchor.MiddleCenter;
            var plusLayout = plusGO.AddComponent<LayoutElement>();
            plusLayout.preferredWidth = 30;
            plusLayout.preferredHeight = 50;

            // 道具图标2
            var icon2GO = CreateIconObject("ComboIcon2", hintPanel.transform);
            comboIcon2 = icon2GO.GetComponent<Image>();

            // 等号
            var equalsGO = new GameObject("EqualsSign");
            equalsGO.transform.SetParent(hintPanel.transform, false);
            var equalsText = equalsGO.AddComponent<Text>();
            equalsText.text = "=";
            equalsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            equalsText.fontSize = 36;
            equalsText.color = Color.white;
            equalsText.alignment = TextAnchor.MiddleCenter;
            var equalsLayout = equalsGO.AddComponent<LayoutElement>();
            equalsLayout.preferredWidth = 30;
            equalsLayout.preferredHeight = 50;

            // 火箭图标
            var rocketGO = new GameObject("RocketIcon");
            rocketGO.transform.SetParent(hintPanel.transform, false);
            var rocketText = rocketGO.AddComponent<Text>();
            rocketText.text = "🚀";
            rocketText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rocketText.fontSize = 40;
            rocketText.color = Color.white;
            rocketText.alignment = TextAnchor.MiddleCenter;
            var rocketLayout = rocketGO.AddComponent<LayoutElement>();
            rocketLayout.preferredWidth = 50;
            rocketLayout.preferredHeight = 50;

            // 提示文字
            var textGO = new GameObject("HintText");
            textGO.transform.SetParent(hintPanel.transform, false);
            hintText = textGO.AddComponent<Text>();
            hintText.text = "Tab + / 同时按!";
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintText.fontSize = 16;
            hintText.color = hintColor;
            hintText.alignment = TextAnchor.MiddleCenter;
            var textLayout = textGO.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 140;
            textLayout.preferredHeight = 50;

            hintPanel.SetActive(false);
        }

        private GameObject CreateIconObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = Color.white;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 50;
            layout.preferredHeight = 50;

            return go;
        }

        private void OnComboReady(ItemData item1, ItemData item2)
        {
            currentItem1 = item1;
            currentItem2 = item2;

            ShowHint(item1, item2);
        }

        private void OnComboTriggered(ItemData item1, ItemData item2)
        {
            // 显示触发成功提示
            if (hintText != null)
            {
                hintText.text = "成功!";
                hintText.color = Color.green;
            }

            // 播放闪烁效果
            StartCoroutine(TriggerFlashEffect());
        }

        private void OnItemRemoved(int slotIndex, ItemData item)
        {
            // 检查组合是否还有效
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                var (item1, item2) = inventory.GetAvailableCombo();
                if (item1 == null || item2 == null)
                {
                    HideHint();
                }
            }
        }

        private void ShowHint(ItemData item1, ItemData item2)
        {
            if (hintPanel == null) return;

            // 设置图标颜色
            if (comboIcon1 != null)
                comboIcon1.color = item1.itemColor;
            if (comboIcon2 != null)
                comboIcon2.color = item2.itemColor;

            hintPanel.SetActive(true);
            isShowingHint = true;

            // 开始脉冲动画
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseAnimation());

            // 自动隐藏（但如果组合还可用会重新显示）
            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);
            hideCoroutine = StartCoroutine(AutoHideHint());

            Debug.Log($"[ComboHint] 显示组合提示: {item1.itemName} + {item2.itemName}");
        }

        private void HideHint()
        {
            if (hintPanel != null)
            {
                hintPanel.SetActive(false);
            }
            isShowingHint = false;
            currentItem1 = null;
            currentItem2 = null;

            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }

        private IEnumerator PulseAnimation()
        {
            while (isShowingHint && hintBackground != null)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                float alpha = Mathf.Lerp(0.6f, 0.95f, t);
                hintBackground.color = new Color(0, 0, 0, alpha);

                // 边框颜色也脉冲
                var outline = hintPanel?.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = Color.Lerp(hintColor, Color.white, t * 0.5f);
                }

                yield return null;
            }
        }

        private IEnumerator AutoHideHint()
        {
            yield return new WaitForSeconds(hintDuration);

            // 检查组合是否还可用
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                var (item1, item2) = inventory.GetAvailableCombo();
                if (item1 != null && item2 != null)
                {
                    // 组合还可用，重新计时
                    hideCoroutine = StartCoroutine(AutoHideHint());
                }
                else
                {
                    HideHint();
                }
            }
        }

        private IEnumerator TriggerFlashEffect()
        {
            if (hintBackground == null) yield break;

            // 白色闪烁
            for (int i = 0; i < 3; i++)
            {
                hintBackground.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                hintBackground.color = new Color(0, 0, 0, 0.8f);
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.5f);
            HideHint();
        }

        private void TriggerCombo()
        {
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                inventory.TriggerDualPlayerCombo();
            }
        }
    }
}
