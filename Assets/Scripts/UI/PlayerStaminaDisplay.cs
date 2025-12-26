using UnityEngine;
using BearCar.Player;

namespace BearCar.UI
{
    /// <summary>
    /// 玩家头顶体力条显示
    /// 使用 SpriteRenderer 显示，不需要 Canvas
    /// </summary>
    public class PlayerStaminaDisplay : MonoBehaviour
    {
        [Header("=== 位置设置 ===")]
        [Tooltip("相对于玩家的偏移")]
        public Vector3 offset = new Vector3(0, 1.2f, 0);

        [Header("=== 外观设置 ===")]
        [Tooltip("体力条宽度")]
        public float barWidth = 1f;

        [Tooltip("体力条高度")]
        public float barHeight = 0.15f;

        [Tooltip("满体力颜色")]
        public Color fullColor = new Color(0.2f, 0.9f, 0.3f);

        [Tooltip("中等体力颜色")]
        public Color midColor = new Color(1f, 0.8f, 0.2f);

        [Tooltip("低体力颜色")]
        public Color lowColor = new Color(0.9f, 0.2f, 0.2f);

        [Tooltip("力竭颜色（闪烁）")]
        public Color exhaustedColor = new Color(0.5f, 0.1f, 0.1f);

        [Tooltip("背景颜色")]
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [Header("=== 显示设置 ===")]
        [Tooltip("始终显示")]
        public bool alwaysShow = false;

        [Tooltip("体力变化后显示时间")]
        public float showDuration = 2f;

        [Tooltip("淡出时间")]
        public float fadeDuration = 0.5f;

        // 组件引用
        private LocalStaminaSystem staminaSystem;
        private LocalBearController bearController;
        private Transform barContainer;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer fillRenderer;
        private SpriteRenderer borderRenderer;

        // 状态
        private float lastStamina = -1f;
        private float showTimer = 0f;
        private float currentAlpha = 0f;
        private bool isExhaustedFlashing = false;
        private float flashTimer = 0f;

        private void Start()
        {
            bearController = GetComponent<LocalBearController>();
            staminaSystem = GetComponent<LocalStaminaSystem>();

            if (staminaSystem == null)
            {
                Debug.LogWarning("[StaminaDisplay] 未找到 LocalStaminaSystem");
                enabled = false;
                return;
            }

            CreateBarVisuals();

            // 订阅体力变化事件
            staminaSystem.OnStaminaChangedEvent += OnStaminaChangedByEvent;

            // 初始化
            lastStamina = staminaSystem.CurrentStaminaValue;

            // 游戏开始时显示一下体力条，让玩家知道它存在
            showTimer = 3f;
            currentAlpha = 1f;

            string playerName = bearController != null
                ? (bearController.PlayerIndex == 0 ? "绿熊" : "红熊")
                : "玩家";
            Debug.Log($"[StaminaDisplay] {playerName} 体力条已创建");
        }

        private void OnDestroy()
        {
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChangedEvent -= OnStaminaChangedByEvent;
            }

            if (barContainer != null)
            {
                Destroy(barContainer.gameObject);
            }
        }

        private void OnStaminaChangedByEvent(float oldValue, float newValue, float maxValue)
        {
            // 显示体力条
            showTimer = showDuration;
            lastStamina = newValue;
        }

        private void CreateBarVisuals()
        {
            // 创建容器
            barContainer = new GameObject("StaminaBar").transform;
            barContainer.SetParent(transform);
            barContainer.localPosition = offset;
            barContainer.localRotation = Quaternion.identity;

            // 创建纹理
            Texture2D whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
            Sprite whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            // 背景
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(barContainer);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localScale = new Vector3(barWidth + 0.05f, barHeight + 0.05f, 1f);
            backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = whiteSprite;
            backgroundRenderer.color = backgroundColor;
            backgroundRenderer.sortingOrder = 99;

            // 填充条
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barContainer);
            fillObj.transform.localPosition = new Vector3(-barWidth / 2f, 0, 0);
            fillObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            fillRenderer = fillObj.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = whiteSprite;
            fillRenderer.color = fullColor;
            fillRenderer.sortingOrder = 100;

            // 边框
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(barContainer);
            borderObj.transform.localPosition = Vector3.zero;
            borderObj.transform.localScale = new Vector3(barWidth + 0.08f, barHeight + 0.08f, 1f);
            borderRenderer = borderObj.AddComponent<SpriteRenderer>();
            borderRenderer.sprite = whiteSprite;
            borderRenderer.color = new Color(0, 0, 0, 0.9f);
            borderRenderer.sortingOrder = 98;

            // 玩家颜色标记
            if (bearController != null)
            {
                Color playerColor = bearController.PlayerIndex == 0
                    ? new Color(0.3f, 0.8f, 0.4f) // 绿熊
                    : new Color(0.9f, 0.3f, 0.3f); // 红熊
                borderRenderer.color = new Color(playerColor.r * 0.5f, playerColor.g * 0.5f, playerColor.b * 0.5f, 0.9f);
            }
        }

        private void Update()
        {
            if (staminaSystem == null) return;

            float currentStamina = staminaSystem.CurrentStaminaValue;
            float maxStamina = staminaSystem.MaxStamina;
            bool isExhausted = staminaSystem.IsExhaustedValue;

            // 更新显示计时器
            UpdateVisibility();

            // 更新体力条填充
            UpdateBarFill(currentStamina, maxStamina, isExhausted);

            // 力竭闪烁效果
            UpdateExhaustedFlash(isExhausted);
        }

        private void UpdateVisibility()
        {
            float targetAlpha;

            if (alwaysShow)
            {
                targetAlpha = 1f;
            }
            else if (showTimer > 0)
            {
                showTimer -= Time.deltaTime;

                if (showTimer > fadeDuration)
                {
                    targetAlpha = 1f;
                }
                else
                {
                    targetAlpha = showTimer / fadeDuration;
                }
            }
            else
            {
                targetAlpha = 0f;
            }

            // 平滑过渡
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 10f);

            // 应用透明度
            SetBarAlpha(currentAlpha);
        }

        private void SetBarAlpha(float alpha)
        {
            if (backgroundRenderer != null)
            {
                Color c = backgroundRenderer.color;
                backgroundRenderer.color = new Color(c.r, c.g, c.b, backgroundColor.a * alpha);
            }

            if (fillRenderer != null)
            {
                Color c = fillRenderer.color;
                fillRenderer.color = new Color(c.r, c.g, c.b, alpha);
            }

            if (borderRenderer != null)
            {
                Color c = borderRenderer.color;
                borderRenderer.color = new Color(c.r, c.g, c.b, 0.9f * alpha);
            }
        }

        private void UpdateBarFill(float currentStamina, float maxStamina, bool isExhausted)
        {
            if (fillRenderer == null) return;

            float ratio = Mathf.Clamp01(currentStamina / maxStamina);

            // 更新填充宽度（从左向右填充）
            fillRenderer.transform.localScale = new Vector3(barWidth * ratio, barHeight, 1f);
            fillRenderer.transform.localPosition = new Vector3(-barWidth / 2f + (barWidth * ratio) / 2f, 0, 0);

            // 更新颜色
            Color targetColor;
            if (isExhausted)
            {
                targetColor = exhaustedColor;
            }
            else if (ratio < 0.25f)
            {
                targetColor = lowColor;
            }
            else if (ratio < 0.5f)
            {
                targetColor = midColor;
            }
            else
            {
                targetColor = fullColor;
            }

            fillRenderer.color = new Color(targetColor.r, targetColor.g, targetColor.b, currentAlpha);
        }

        private void UpdateExhaustedFlash(bool isExhausted)
        {
            if (isExhausted)
            {
                // 开始闪烁
                flashTimer += Time.deltaTime * 8f;
                float flash = (Mathf.Sin(flashTimer) + 1f) / 2f;

                if (fillRenderer != null)
                {
                    Color c = Color.Lerp(exhaustedColor, lowColor, flash);
                    fillRenderer.color = new Color(c.r, c.g, c.b, currentAlpha);
                }

                // 力竭时始终显示
                showTimer = showDuration;
            }
            else
            {
                flashTimer = 0f;
            }
        }

        /// <summary>
        /// 强制显示体力条（用于道具效果等）
        /// </summary>
        public void ForceShow(float duration = -1f)
        {
            showTimer = duration > 0 ? duration : showDuration;
        }

        /// <summary>
        /// 设置是否始终显示
        /// </summary>
        public void SetAlwaysShow(bool show)
        {
            alwaysShow = show;
            if (show)
            {
                currentAlpha = 1f;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 在编辑器中显示体力条位置预览
            Vector3 barPos = transform.position + offset;

            Gizmos.color = backgroundColor;
            Gizmos.DrawCube(barPos, new Vector3(barWidth, barHeight, 0.01f));

            Gizmos.color = fullColor;
            Gizmos.DrawCube(barPos, new Vector3(barWidth * 0.7f, barHeight * 0.8f, 0.01f));
        }
#endif
    }
}
