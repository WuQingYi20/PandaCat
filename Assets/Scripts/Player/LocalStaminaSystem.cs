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
    }
}
