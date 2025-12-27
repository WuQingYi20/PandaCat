using UnityEngine;
using BearCar.Core;

namespace BearCar.Player
{
    /// <summary>
    /// 本地玩家体力系统 - 不依赖网络
    /// </summary>
    public class LocalStaminaSystem : StaminaBase
    {
        private LocalBearController bearController;
        private float lastLoggedStamina = -1f;
        private bool wasExhausted = false;

        public override float CurrentStaminaValue => currentStamina;
        public override bool IsExhaustedValue => isExhausted;

        // 事件：体力变化时触发
        public event System.Action<float, float, float> OnStaminaChangedEvent; // oldValue, newValue, maxValue

        private void Awake()
        {
            bearController = GetComponent<LocalBearController>();
            LoadConfig();

            // 初始化体力
            currentStamina = config != null ? config.maxStamina : 5f;
            lastLoggedStamina = currentStamina;
        }

        private void FixedUpdate()
        {
            if (config == null) return;

            float oldStamina = currentStamina;

            if (bearController != null && bearController.IsPushing)
            {
                DrainStamina(config.staminaDrainRate * Time.fixedDeltaTime);
            }
            else
            {
                RecoverStamina(config.staminaRecoveryRate * Time.fixedDeltaTime);
            }

            // 检测力竭状态变化
            if (isExhausted && !wasExhausted)
            {
                string playerName = GetPlayerName();
                Debug.Log($"[Stamina] {playerName} 体力耗尽！需要休息恢复");
            }
            else if (!isExhausted && wasExhausted)
            {
                string playerName = GetPlayerName();
                Debug.Log($"[Stamina] {playerName} 恢复了体力");
            }
            wasExhausted = isExhausted;

            // 触发变化事件
            if (Mathf.Abs(currentStamina - oldStamina) > 0.01f)
            {
                OnStaminaChangedEvent?.Invoke(oldStamina, currentStamina, MaxStamina);
            }
        }

        /// <summary>
        /// 立即恢复指定量的体力（用于道具）
        /// </summary>
        public void Recover(float amount)
        {
            float oldStamina = currentStamina;

            if (isExhausted && amount > 0)
            {
                isExhausted = false;
                exhaustedTimer = 0f;
            }

            float maxStamina = config != null ? config.maxStamina : 5f;
            currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);

            string playerName = GetPlayerName();
            string action = amount >= 0 ? "恢复" : "损失";
            float absAmount = Mathf.Abs(amount);

            Debug.Log($"[Stamina] {playerName} {action} {absAmount:F1} 体力 → 当前: {currentStamina:F1}/{maxStamina:F1}");

            // 触发变化事件
            OnStaminaChangedEvent?.Invoke(oldStamina, currentStamina, maxStamina);
        }

        /// <summary>
        /// 获取当前体力百分比
        /// </summary>
        public float GetStaminaPercent()
        {
            float max = config != null ? config.maxStamina : 5f;
            return currentStamina / max;
        }

        private string GetPlayerName()
        {
            if (bearController != null)
            {
                return bearController.PlayerIndex == 0 ? "绿熊" : "红熊";
            }
            return "玩家";
        }
    }
}
