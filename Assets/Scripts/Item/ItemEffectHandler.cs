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
                inventory.OnComboTriggered += HandleComboTriggered;
            }
        }

        private void OnDestroy()
        {
            var inventory = SharedInventory.Instance;
            if (inventory != null)
            {
                inventory.OnItemUsed -= HandleItemUsed;
                inventory.OnComboTriggered -= HandleComboTriggered;
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

        /// <summary>
        /// 处理组合道具触发效果
        /// </summary>
        private void HandleComboTriggered(ItemData item1, ItemData item2)
        {
            if (item1 == null) return;

            Debug.Log($"[ItemEffect] 🚀 组合触发: {item1.itemName} + {item2.itemName}");

            // 根据组合结果类型执行效果
            switch (item1.comboResultType)
            {
                case ItemType.RocketBoost:
                    StartCoroutine(SuperRocketBoostEffect());
                    break;

                case ItemType.SpeedBoost:
                    ApplySpeedBoostToAll(item1);
                    break;

                default:
                    Debug.Log($"[ItemEffect] 组合产生: {item1.comboResultType}");
                    break;
            }
        }

        /// <summary>
        /// 超级火箭推进效果 - 曼妥思+可乐组合
        /// </summary>
        private IEnumerator SuperRocketBoostEffect()
        {
            Debug.Log("[ItemEffect] 🚀🚀🚀 超级火箭推进启动!");

            // 获取车辆
            var cart = FindFirstObjectByType<CartController>();
            if (cart == null) yield break;

            var rb = cart.GetComponent<Rigidbody2D>();
            if (rb == null) yield break;

            float duration = 5f;
            float elapsed = 0f;
            float boostForce = 15000f;

            // 视觉效果 - 屏幕震动
            StartCoroutine(ScreenShakeEffect(duration * 0.5f));

            // 创建粒子效果（如果有粒子系统）
            CreateRocketParticles(cart.transform);

            // 持续推进
            while (elapsed < duration)
            {
                // 施加强大的推力
                rb.AddForce(Vector2.right * boostForce * Time.deltaTime, ForceMode2D.Force);

                // 逐渐减弱
                float t = elapsed / duration;
                boostForce = Mathf.Lerp(15000f, 3000f, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.Log("[ItemEffect] 🚀 火箭推进结束");
        }

        private IEnumerator ScreenShakeEffect(float duration)
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            Vector3 originalPos = cam.transform.position;
            float elapsed = 0f;
            float intensity = 0.3f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                cam.transform.position = originalPos + new Vector3(x, y, 0);

                // 逐渐减弱
                intensity = Mathf.Lerp(0.3f, 0f, elapsed / duration);

                elapsed += Time.deltaTime;
                yield return null;
            }

            cam.transform.position = originalPos;
        }

        private void CreateRocketParticles(Transform target)
        {
            // 创建简单的粒子效果
            var particleGO = new GameObject("RocketParticles");
            particleGO.transform.position = target.position + Vector3.left * 2f;
            particleGO.transform.SetParent(target);

            var ps = particleGO.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 5f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 5f;
            main.startSize = 0.5f;
            main.startColor = new Color(1f, 0.5f, 0.1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 50f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.5f;
            shape.rotation = new Vector3(0, 0, 180);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // 自动销毁
            Destroy(particleGO, 6f);

            Debug.Log("[ItemEffect] 🔥 火箭粒子效果已创建");
        }
    }
}
