using Unity.Netcode;
using UnityEngine;
using BearCar.Core;

namespace BearCar.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BearController : NetworkBehaviour
    {
        [SerializeField] private GameConfig config;

        public NetworkVariable<bool> IsPushing = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<Vector2> MoveInput = new(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Rigidbody2D rb;
        private StaminaSystem stamina;
        private bool isNearCart = false;
        private bool pushButtonHeld = false;

        public bool HasStamina => stamina != null && stamina.HasStamina;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stamina = GetComponent<StaminaSystem>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                var inputHandler = GetComponent<BearInputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.enabled = true;
                }
            }

            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;

            float moveSpeed = config != null ? config.playerMoveSpeed : 5f;
            Vector2 movement = MoveInput.Value * moveSpeed;
            rb.linearVelocity = new Vector2(movement.x, rb.linearVelocity.y);

            UpdatePushState();
        }

        private void UpdatePushState()
        {
            bool shouldBePushing = isNearCart &&
                                   pushButtonHeld &&
                                   HasStamina;

            if (IsPushing.Value != shouldBePushing)
            {
                IsPushing.Value = shouldBePushing;
            }
        }

        public void SetNearCart(bool value)
        {
            isNearCart = value;
            if (!value && IsServer)
            {
                IsPushing.Value = false;
            }
        }

        [ServerRpc]
        public void SubmitInputServerRpc(Vector2 moveInput, bool isPushHeld)
        {
            MoveInput.Value = moveInput;
            pushButtonHeld = isPushHeld;
        }
    }
}
