using Unity.Netcode;
using UnityEngine;
using BearCar.Player;

namespace BearCar.Cart
{
    [RequireComponent(typeof(Collider2D))]
    public class CartPushZone : NetworkBehaviour
    {
        private CartController cart;

        private void Awake()
        {
            cart = GetComponentInParent<CartController>();

            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;

            if (other.TryGetComponent<BearController>(out var bear))
            {
                cart?.RegisterBear(bear);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsServer) return;

            if (other.TryGetComponent<BearController>(out var bear))
            {
                cart?.UnregisterBear(bear);
            }
        }
    }
}
