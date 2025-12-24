using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using BearCar.Core;
using BearCar.Player;

namespace BearCar.Cart
{
    public enum CartState
    {
        Idle,
        Sliding,
        Holding,
        Advancing
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class CartController : NetworkBehaviour
    {
        [SerializeField] private GameConfig config;

        public NetworkVariable<int> ActivePushers = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<CartState> State = new(
            CartState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Rigidbody2D rb;
        private HashSet<BearController> registeredBears = new HashSet<BearController>();
        private float currentSlopeAngle = 0f;

        public override void OnNetworkSpawn()
        {
            rb = GetComponent<Rigidbody2D>();

            if (config == null)
            {
                config = Resources.Load<GameConfig>("GameConfig");
            }

            if (IsServer && config != null)
            {
                rb.mass = config.cartMass;
                rb.linearDamping = config.cartDrag;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;

            DetectSlope();
            ApplyForces();
            UpdateState();
        }

        private void DetectSlope()
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                2f,
                LayerMask.GetMask("Ground")
            );

            if (hit.collider != null)
            {
                currentSlopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            }
            else if (config != null)
            {
                currentSlopeAngle = config.slopeAngle;
            }
        }

        private void ApplyForces()
        {
            if (config == null) return;

            int pushers = registeredBears.Count(b => b.IsPushing.Value && b.HasStamina);
            ActivePushers.Value = pushers;

            float pushForce = pushers switch
            {
                0 => 0f,
                1 => config.singlePushForce,
                _ => config.singlePushForce * config.doublePushMultiplier
            };

            float gravityForce = config.CalculateSlopeGravityForce(rb.mass);
            float netForce = pushForce - gravityForce;

            rb.AddForce(Vector2.right * netForce);
        }

        private void UpdateState()
        {
            State.Value = ActivePushers.Value switch
            {
                0 when currentSlopeAngle > 1f => CartState.Sliding,
                0 => CartState.Idle,
                1 => CartState.Holding,
                _ => CartState.Advancing
            };
        }

        public void RegisterBear(BearController bear)
        {
            if (!IsServer) return;

            registeredBears.Add(bear);
            bear.SetNearCart(true);
        }

        public void UnregisterBear(BearController bear)
        {
            if (!IsServer) return;

            registeredBears.Remove(bear);
            bear.SetNearCart(false);
        }
    }
}
