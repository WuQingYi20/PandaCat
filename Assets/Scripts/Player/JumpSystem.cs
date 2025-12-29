using UnityEngine;
using BearCar.Core;

namespace BearCar.Player
{
    /// <summary>
    /// 跳跃系统 v2.0 - 参考 Celeste / Hollow Knight 手感
    ///
    /// 核心改进：
    /// 1. 可变跳跃高度 - 短按跳得矮，长按跳得高
    /// 2. 下落加速 - 下落时更重，手感更脆
    /// 3. 跳跃顶点悬停 - 顶点时短暂"漂浮"
    /// 4. 土狼时间 + 跳跃缓冲 - 更宽容的输入
    /// </summary>
    public class JumpSystem : MonoBehaviour
    {
        [Header("=== 基础跳跃 ===")]
        [Tooltip("跳跃高度（单位：Unity单位）")]
        [SerializeField] private float jumpHeight = 2.5f;

        [Tooltip("到达顶点的时间（秒）")]
        [SerializeField] private float timeToApex = 0.35f;

        [Header("=== 手感调整 ===")]
        [Tooltip("下落重力倍数（>1 = 更快下落）")]
        [SerializeField] private float fallMultiplier = 2.5f;

        [Tooltip("短按跳跃重力倍数（释放跳跃键时）")]
        [SerializeField] private float lowJumpMultiplier = 3f;

        [Tooltip("顶点悬停范围（速度小于此值时触发）")]
        [SerializeField] private float apexThreshold = 2f;

        [Tooltip("顶点悬停重力倍数（<1 = 更长悬停）")]
        [SerializeField] private float apexGravityMultiplier = 0.5f;

        [Tooltip("顶点时水平移动加成")]
        [SerializeField] private float apexSpeedBonus = 1.2f;

        [Header("=== 输入宽容 ===")]
        [Tooltip("土狼时间 - 离开平台后仍可跳跃")]
        [SerializeField] private float coyoteTime = 0.12f;

        [Tooltip("跳跃缓冲 - 落地前按跳跃也生效")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("=== 高级 ===")]
        [Tooltip("最大跳跃次数（1=单跳，2=二段跳）")]
        [SerializeField] private int maxJumpCount = 1;

        [Tooltip("最大下落速度")]
        [SerializeField] private float maxFallSpeed = 20f;

        [Tooltip("地面检测层")]
        [SerializeField] private LayerMask groundLayer;

        [Tooltip("地面检测距离")]
        [SerializeField] private float groundCheckDistance = 0.1f;

        // 计算得出的物理值
        private float jumpForce;
        private float gravityScale;
        private float defaultGravityScale;

        // 组件
        private Rigidbody2D rb;
        private Collider2D col;

        // 状态
        private int jumpCount;
        private float lastGroundedTime;
        private float lastJumpPressedTime;
        private bool isGrounded;
        private bool isJumpHeld;
        private bool isAtApex;

        // 公开属性
        public bool IsGrounded => isGrounded;
        public bool IsAtApex => isAtApex;
        public float ApexSpeedBonus => isAtApex ? apexSpeedBonus : 1f;
        public bool CanJump => jumpCount < maxJumpCount &&
                               (isGrounded || Time.time - lastGroundedTime <= coyoteTime);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            if (groundLayer == 0)
            {
                groundLayer = LayerMask.GetMask("Ground");
            }

            // 根据期望的跳跃高度和时间计算物理值
            // 公式来自运动学: h = 1/2 * g * t^2, v = g * t
            CalculateJumpPhysics();
        }

        private void CalculateJumpPhysics()
        {
            // 根据跳跃高度和时间计算重力和初速度
            // g = 2h / t^2
            // v0 = 2h / t
            gravityScale = (2 * jumpHeight) / (timeToApex * timeToApex);
            jumpForce = gravityScale * timeToApex;
            defaultGravityScale = gravityScale / Mathf.Abs(Physics2D.gravity.y);

            if (rb != null)
            {
                rb.gravityScale = defaultGravityScale;
            }

            Debug.Log($"[Jump] 计算物理值: 重力缩放={defaultGravityScale:F2}, 跳跃力={jumpForce:F2}");
        }

        private void Update()
        {
            // 处理重力曲线（在Update中以获得更平滑的效果）
            UpdateGravity();
        }

        private void FixedUpdate()
        {
            CheckGrounded();

            // 跳跃缓冲检查
            if (isGrounded && Time.time - lastJumpPressedTime <= jumpBufferTime)
            {
                ExecuteJump();
                lastJumpPressedTime = 0f;
            }

            // 限制最大下落速度
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }

        private void UpdateGravity()
        {
            if (rb == null) return;

            float targetGravityScale = defaultGravityScale;
            isAtApex = false;

            if (rb.linearVelocity.y < 0)
            {
                // 下落时 - 增加重力让下落更快
                targetGravityScale = defaultGravityScale * fallMultiplier;
            }
            else if (rb.linearVelocity.y > 0 && !isJumpHeld)
            {
                // 上升但松开跳跃键 - 短跳
                targetGravityScale = defaultGravityScale * lowJumpMultiplier;
            }
            else if (Mathf.Abs(rb.linearVelocity.y) < apexThreshold && !isGrounded)
            {
                // 顶点悬停
                targetGravityScale = defaultGravityScale * apexGravityMultiplier;
                isAtApex = true;
            }

            // 平滑过渡重力（避免突变）
            rb.gravityScale = Mathf.Lerp(rb.gravityScale, targetGravityScale, Time.deltaTime * 20f);
        }

        private void CheckGrounded()
        {
            bool wasGrounded = isGrounded;

            if (col != null)
            {
                Vector2 boxCenter = (Vector2)transform.position + Vector2.down * (col.bounds.extents.y);
                Vector2 boxSize = new Vector2(col.bounds.size.x * 0.9f, groundCheckDistance);

                isGrounded = Physics2D.BoxCast(
                    boxCenter,
                    boxSize,
                    0f,
                    Vector2.down,
                    groundCheckDistance,
                    groundLayer
                );
            }
            else
            {
                isGrounded = Physics2D.Raycast(
                    transform.position,
                    Vector2.down,
                    1.1f,
                    groundLayer
                );
            }

            // 刚落地
            if (isGrounded && !wasGrounded)
            {
                OnLand();
            }

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                jumpCount = 0;
            }
        }

        /// <summary>
        /// 按下跳跃键
        /// </summary>
        public void TryJump()
        {
            lastJumpPressedTime = Time.time;
            isJumpHeld = true;

            if (CanJump)
            {
                ExecuteJump();
            }
        }

        /// <summary>
        /// 松开跳跃键（用于可变跳跃高度）
        /// </summary>
        public void ReleaseJump()
        {
            isJumpHeld = false;
        }

        private void ExecuteJump()
        {
            if (rb == null) return;

            // 重置垂直速度后施加跳跃力
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            jumpCount++;
            isGrounded = false;
            lastGroundedTime = 0f; // 消耗土狼时间

            OnJump();
        }

        /// <summary>
        /// 跳跃回调
        /// </summary>
        protected virtual void OnJump()
        {
            // 可用于音效、粒子等
        }

        /// <summary>
        /// 落地回调
        /// </summary>
        protected virtual void OnLand()
        {
            // 可用于落地音效、尘土粒子等
        }

        /// <summary>
        /// 运行时设置跳跃高度
        /// </summary>
        public void SetJumpHeight(float height)
        {
            jumpHeight = height;
            CalculateJumpPhysics();
        }

        /// <summary>
        /// 启用/禁用二段跳
        /// </summary>
        public void SetDoubleJump(bool enabled)
        {
            maxJumpCount = enabled ? 2 : 1;
        }

        /// <summary>
        /// 获取当前是否在跳跃上升阶段
        /// </summary>
        public bool IsRising => rb != null && rb.linearVelocity.y > 0.1f;

        /// <summary>
        /// 获取当前是否在下落阶段
        /// </summary>
        public bool IsFalling => rb != null && rb.linearVelocity.y < -0.1f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器中修改参数时重新计算
            if (Application.isPlaying && rb != null)
            {
                CalculateJumpPhysics();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 可视化跳跃高度
            Vector3 groundPos = transform.position;
            Vector3 apexPos = groundPos + Vector3.up * jumpHeight;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundPos, apexPos);
            Gizmos.DrawWireSphere(apexPos, 0.2f);

            // 显示跳跃高度数值
            UnityEditor.Handles.Label(apexPos + Vector3.right * 0.3f, $"跳跃高度: {jumpHeight}");
        }
#endif
    }
}
