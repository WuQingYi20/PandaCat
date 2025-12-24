using UnityEngine;
using UnityEngine.UI;
using BearCar.Player;
using BearCar.Core;

namespace BearCar.UI
{
    public class StaminaBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color lowColor = Color.yellow;
        [SerializeField] private Color exhaustedColor = Color.red;

        private StaminaSystem staminaSystem;
        private GameConfig config;

        public void Initialize(StaminaSystem system, GameConfig cfg)
        {
            staminaSystem = system;
            config = cfg;

            if (staminaSystem != null)
            {
                staminaSystem.CurrentStamina.OnValueChanged += OnStaminaChanged;
                staminaSystem.IsExhausted.OnValueChanged += OnExhaustedChanged;

                UpdateBar(staminaSystem.CurrentStamina.Value);
            }
        }

        private void OnDestroy()
        {
            if (staminaSystem != null)
            {
                staminaSystem.CurrentStamina.OnValueChanged -= OnStaminaChanged;
                staminaSystem.IsExhausted.OnValueChanged -= OnExhaustedChanged;
            }
        }

        private void OnStaminaChanged(float previousValue, float newValue)
        {
            UpdateBar(newValue);
        }

        private void OnExhaustedChanged(bool previousValue, bool newValue)
        {
            if (newValue && fillImage != null)
            {
                fillImage.color = exhaustedColor;
            }
        }

        private void UpdateBar(float currentStamina)
        {
            if (fillImage == null || config == null) return;

            float ratio = currentStamina / config.maxStamina;
            fillImage.fillAmount = ratio;

            if (staminaSystem != null && staminaSystem.IsExhausted.Value)
            {
                fillImage.color = exhaustedColor;
            }
            else
            {
                fillImage.color = ratio switch
                {
                    < 0.2f => exhaustedColor,
                    < 0.5f => lowColor,
                    _ => normalColor
                };
            }
        }
    }
}
