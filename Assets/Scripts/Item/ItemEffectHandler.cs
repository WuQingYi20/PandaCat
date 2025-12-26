using UnityEngine;
using System.Collections;
using BearCar.Player;
using BearCar.Cart;

namespace BearCar.Item
{
    /// <summary>
    /// 道具效果处理器 - 处理道具使用效果
    /// </summary>
    public class ItemEffectHandler : MonoBehaviour
    {
        public static ItemEffectHandler Instance { get; private set; }

        [Header("=== 效果设置 ===")]
        [SerializeField] private float defaultSpeedBoostMultiplier = 1.5f;
        [SerializeField] private float defaultEffectDuration = 5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 订阅道具使用事件
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                inventory.OnItemUsed += HandleItemUsed;
            }
        }

        private void OnDestroy()
        {
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                inventory.OnItemUsed -= HandleItemUsed;
            }
        }

        private void HandleItemUsed(int slotIndex, ItemData item)
        {
            if (item == null) return;

            // 获取使用者（这里简化处理，实际应该传入使用者）
            var players = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);

            switch (item.itemType)
            {
                case ItemType.SpeedBoost:
                    ApplySpeedBoostToAll(item);
                    break;

                case ItemType.StaminaRecover:
                    ApplyStaminaRecoverToAll(item);
                    break;

                case ItemType.Shield:
                    // TODO: 实现护盾效果
                    Debug.Log($"[ItemEffect] 护盾效果 - 持续 {item.effectDuration}s");
                    break;

                case ItemType.Magnet:
                    StartCoroutine(MagnetEffect(item));
                    break;

                case ItemType.TimeSlowdown:
                    StartCoroutine(TimeSlowdownEffect(item));
                    break;

                default:
                    Debug.Log($"[ItemEffect] 使用了 {item.itemName}");
                    break;
            }

            // 显示使用效果
            ShowUseEffect(item);
        }

        private void ApplySpeedBoostToAll(ItemData item)
        {
            float multiplier = item.effectValue > 0 ? item.effectValue : defaultSpeedBoostMultiplier;
            float duration = item.effectDuration > 0 ? item.effectDuration : defaultEffectDuration;

            // 给车加速
            var cart = FindFirstObjectByType<CartController>();
            if (cart != null)
            {
                StartCoroutine(CartSpeedBoost(cart, multiplier, duration));
            }

            Debug.Log($"[ItemEffect] 加速 x{multiplier}，持续 {duration}s");
        }

        private IEnumerator CartSpeedBoost(CartController cart, float multiplier, float duration)
        {
            var rb = cart.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 施加瞬间推力
                rb.AddForce(Vector2.right * 5000f * multiplier, ForceMode2D.Impulse);
            }

            yield return new WaitForSeconds(duration);
        }

        private void ApplyStaminaRecoverToAll(ItemData item)
        {
            float amount = item.effectValue > 0 ? item.effectValue : 50f;

            var players = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.Stamina != null)
                {
                    // 恢复体力
                    player.Stamina.Recover(amount);
                }
            }

            Debug.Log($"[ItemEffect] 全体恢复体力 {amount}");
        }

        private IEnumerator MagnetEffect(ItemData item)
        {
            float duration = item.effectDuration > 0 ? item.effectDuration : 5f;
            float range = item.effectValue > 0 ? item.effectValue : 10f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 吸引附近的道具
                var pickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
                var cart = FindFirstObjectByType<CartController>();

                if (cart != null)
                {
                    foreach (var pickup in pickups)
                    {
                        float dist = Vector2.Distance(pickup.transform.position, cart.transform.position);
                        if (dist < range)
                        {
                            Vector2 dir = (cart.transform.position - pickup.transform.position).normalized;
                            pickup.transform.position += (Vector3)dir * Time.deltaTime * 5f;
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.Log("[ItemEffect] 磁铁效果结束");
        }

        private IEnumerator TimeSlowdownEffect(ItemData item)
        {
            float duration = item.effectDuration > 0 ? item.effectDuration : 3f;
            float slowScale = item.effectValue > 0 ? item.effectValue : 0.5f;

            Time.timeScale = slowScale;
            Time.fixedDeltaTime = 0.02f * slowScale;

            Debug.Log($"[ItemEffect] 时间减缓到 {slowScale}x");

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            Debug.Log("[ItemEffect] 时间恢复正常");
        }

        private void ShowUseEffect(ItemData item)
        {
            // 可以在这里添加粒子效果、屏幕闪烁等
            StartCoroutine(FlashEffect(item.itemColor));
        }

        private IEnumerator FlashEffect(Color color)
        {
            // 简单的颜色闪烁效果（可以用 UI 实现）
            yield return new WaitForSeconds(0.1f);
        }
    }
}
