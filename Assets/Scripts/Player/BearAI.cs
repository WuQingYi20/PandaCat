using Unity.Netcode;
using UnityEngine;
using BearCar.Cart;

namespace BearCar.Player
{
    /// <summary>
    /// AI 熊助手 - 用于单人测试
    /// 按 T 键生成/销毁 AI 熊
    /// AI 会自动跟随车并推车
    /// </summary>
    public class BearAI : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float pushDistance = 2f;

        private CartController targetCart;
        private bool isPushing = false;
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private BoxCollider2D col;

        // 模拟的推车状态
        public bool IsPushingCart => isPushing;

        private void Start()
        {
            // 设置视觉
            SetupVisuals();

            // 找到车
            targetCart = FindFirstObjectByType<CartController>();

            // 添加物理组件
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            Debug.Log("[BearAI] AI 熊助手已生成！会自动帮你推车。");
        }

        private void SetupVisuals()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            // 创建绿色方块（AI 熊）
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            tex.SetPixels(pixels);
            tex.Apply();

            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            spriteRenderer.color = new Color(0.3f, 0.9f, 0.3f); // 绿色 AI
            spriteRenderer.sortingOrder = 10;
        }

        private void Update()
        {
            if (targetCart == null)
            {
                targetCart = FindFirstObjectByType<CartController>();
                return;
            }

            // 计算到车的距离
            float distanceToCart = transform.position.x - targetCart.transform.position.x;

            // AI 逻辑：跟随车并保持在推车位置
            float targetX = targetCart.transform.position.x - pushDistance;

            if (Mathf.Abs(transform.position.x - targetX) > 0.5f)
            {
                // 移动到推车位置
                float direction = Mathf.Sign(targetX - transform.position.x);
                rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
                isPushing = false;
            }
            else
            {
                // 到达位置，开始推车
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                isPushing = true;
            }

            // 更新颜色表示状态
            spriteRenderer.color = isPushing ? new Color(0.3f, 1f, 0.3f) : new Color(0.5f, 0.7f, 0.5f);
        }

        private void OnDestroy()
        {
            Debug.Log("[BearAI] AI 熊助手已移除。");
        }
    }

    /// <summary>
    /// AI 管理器 - 挂在场景中，按 T 键切换 AI
    /// </summary>
    public class BearAIManager : MonoBehaviour
    {
        private GameObject aiInstance;

        private void Update()
        {
            // 按 T 键切换 AI 熊
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleAI();
            }
        }

        private void ToggleAI()
        {
            if (aiInstance == null)
            {
                // 生成 AI 熊
                aiInstance = new GameObject("BearAI");
                aiInstance.transform.position = new Vector3(-8f, 0f, 0f);
                aiInstance.AddComponent<BearAI>();
                Debug.Log("[BearAIManager] 按 T 键生成了 AI 熊助手");
            }
            else
            {
                // 移除 AI 熊
                Destroy(aiInstance);
                aiInstance = null;
                Debug.Log("[BearAIManager] 按 T 键移除了 AI 熊助手");
            }
        }
    }
}
