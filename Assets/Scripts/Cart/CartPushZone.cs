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
            // 玩家熊（需要服务器）
            if (IsServer && other.TryGetComponent<BearController>(out var bear))
            {
                cart?.RegisterBear(bear);
            }

            // AI 熊（本地检测）
            if (other.TryGetComponent<BearAI>(out var ai))
            {
                cart?.RegisterAI(ai);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // 玩家熊
            if (IsServer && other.TryGetComponent<BearController>(out var bear))
            {
                cart?.UnregisterBear(bear);
            }

            // AI 熊
            if (other.TryGetComponent<BearAI>(out var ai))
            {
                cart?.UnregisterAI(ai);
            }
        }
    }
}
