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

        public override float CurrentStaminaValue => currentStamina;
        public override bool IsExhaustedValue => isExhausted;

        private void Awake()
        {
            bearController = GetComponent<LocalBearController>();
            LoadConfig();

            // 初始化体力
            currentStamina = config != null ? config.maxStamina : 5f;
        }

        private void FixedUpdate()
        {
            if (config == null) return;

            if (bearController != null && bearController.IsPushing)
            {
                DrainStamina(config.staminaDrainRate * Time.fixedDeltaTime);
            }
            else
            {
                RecoverStamina(config.staminaRecoveryRate * Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// 立即恢复指定量的体力（用于道具）
        /// </summary>
        public void Recover(float amount)
        {
            if (isExhausted)
            {
                isExhausted = false;
                exhaustedTimer = 0f;
            }

            float maxStamina = config != null ? config.maxStamina : 5f;
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);

            Debug.Log($"[Stamina] 恢复 {amount}，当前: {currentStamina}/{maxStamina}");
        }
    }
}
