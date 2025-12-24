using Unity.Netcode;
using UnityEngine;
using BearCar.Core;

namespace BearCar.Player
{
    public class StaminaSystem : NetworkBehaviour
    {
        [SerializeField] private GameConfig config;

        public NetworkVariable<float> CurrentStamina = new(
            5f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> IsExhausted = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private BearController bearController;
        private float exhaustedTimer = 0f;

        public float LocalStamina => CurrentStamina.Value;
        public bool HasStamina => CurrentStamina.Value > 0f && !IsExhausted.Value;

        private void Awake()
        {
            bearController = GetComponent<BearController>();
        }

        public override void OnNetworkSpawn()
        {
            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }

            if (IsServer && config != null)
            {
                CurrentStamina.Value = config.maxStamina;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;
            if (config == null) return;

            if (bearController != null && bearController.IsPushing.Value)
            {
                DrainStamina(config.staminaDrainRate * Time.fixedDeltaTime);
            }
            else
            {
                RecoverStamina(config.staminaRecoveryRate * Time.fixedDeltaTime);
            }
        }

        private void DrainStamina(float amount)
        {
            CurrentStamina.Value = Mathf.Max(0f, CurrentStamina.Value - amount);

            if (CurrentStamina.Value <= 0f && !IsExhausted.Value)
            {
                IsExhausted.Value = true;
                exhaustedTimer = config.exhaustedRecoveryDelay;
            }
        }

        private void RecoverStamina(float amount)
        {
            if (IsExhausted.Value)
            {
                exhaustedTimer -= Time.fixedDeltaTime;
                if (exhaustedTimer > 0f) return;
                IsExhausted.Value = false;
            }

            CurrentStamina.Value = Mathf.Min(config.maxStamina, CurrentStamina.Value + amount);
        }
    }
}
