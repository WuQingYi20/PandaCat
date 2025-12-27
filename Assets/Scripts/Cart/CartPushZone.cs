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
            if (cart == null) return;

            // 网络玩家熊（需要服务器或本地模式）
            if (other.TryGetComponent<BearController>(out var bear))
            {
                if (IsServer || cart.IsLocalOnlyMode)
                {
                    cart.RegisterBear(bear);
                }
            }

            // 本地玩家熊
            if (other.TryGetComponent<LocalBearController>(out var localBear))
            {
                cart.RegisterLocalBear(localBear);
            }

            // AI 熊
            if (other.TryGetComponent<BearAI>(out var ai))
            {
                cart.RegisterAI(ai);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (cart == null) return;

            // 网络玩家熊
            if (other.TryGetComponent<BearController>(out var bear))
            {
                if (IsServer || cart.IsLocalOnlyMode)
                {
                    cart.UnregisterBear(bear);
                }
            }

            // 本地玩家熊
            if (other.TryGetComponent<LocalBearController>(out var localBear))
            {
                cart.UnregisterLocalBear(localBear);
            }

            // AI 熊
            if (other.TryGetComponent<BearAI>(out var ai))
            {
                cart.UnregisterAI(ai);
            }
        }
    }
}
