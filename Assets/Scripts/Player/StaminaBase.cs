using UnityEngine;
using BearCar.Core;

namespace BearCar.Player
{
    /// <summary>
    /// 体力系统基类 - 网络版和本地版共享逻辑
    /// </summary>
    public abstract class StaminaBase : MonoBehaviour
    {
        [SerializeField] protected GameConfig config;

        protected float currentStamina;
        protected bool isExhausted;
        protected float exhaustedTimer;

        public abstract float CurrentStaminaValue { get; }
        public abstract bool IsExhaustedValue { get; }
        public bool HasStamina => CurrentStaminaValue > 0f && !IsExhaustedValue;

        protected virtual void LoadConfig()
        {
            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }
        }

        protected void DrainStamina(float amount)
        {
            currentStamina = Mathf.Max(0f, currentStamina - amount);

            if (currentStamina <= 0f && !isExhausted)
            {
                isExhausted = true;
                exhaustedTimer = config != null ? config.exhaustedRecoveryDelay : 1f;
            }

            OnStaminaChanged();
        }

        protected void RecoverStamina(float amount)
        {
            if (isExhausted)
            {
                exhaustedTimer -= Time.fixedDeltaTime;
                if (exhaustedTimer > 0f) return;
                isExhausted = false;
            }

            float maxStamina = config != null ? config.maxStamina : 5f;
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);

            OnStaminaChanged();
        }

        protected virtual void OnStaminaChanged() { }
    }
}
