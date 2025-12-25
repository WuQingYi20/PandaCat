using UnityEngine;
using BearCar.Core;

namespace BearCar.Player
{
    /// <summary>
    /// 跳跃系统 - 网络和本地玩家共享
    /// 可扩展：二段跳、蓄力跳、墙跳等
    /// </summary>
    public class JumpSystem : MonoBehaviour
    {
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;

        [Header("Advanced (Optional)")]
        [SerializeField] private int maxJumpCount = 1;  // 1 = 单跳, 2 = 二段跳
        [SerializeField] private float coyoteTime = 0.1f;  // 土狼时间（离开地面后仍可跳跃的时间）
        [SerializeField] private float jumpBufferTime = 0.1f;  // 跳跃缓冲（落地前按跳跃也能触发）

        private Rigidbody2D rb;
        private Collider2D col;
        private int jumpCount;
        private float lastGroundedTime;
        private float lastJumpPressedTime;
        private bool isGrounded;

        public bool IsGrounded => isGrounded;
        public bool CanJump => jumpCount < maxJumpCount && (isGrounded || Time.time - lastGroundedTime <= coyoteTime);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            // 如果没有设置 groundLayer，默认使用 Ground 层
            if (groundLayer == 0)
            {
                groundLayer = LayerMask.GetMask("Ground");
            }
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
        }

        private void CheckGrounded()
        {
            bool wasGrounded = isGrounded;

            // 使用 BoxCast 检测地面
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
                // 备用：射线检测
                isGrounded = Physics2D.Raycast(
                    transform.position,
                    Vector2.down,
                    1.1f,
                    groundLayer
                );
            }

            // 更新落地时间
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                jumpCount = 0;
            }
        }

        /// <summary>
        /// 尝试跳跃（由输入系统调用）
        /// </summary>
        public void TryJump()
        {
            lastJumpPressedTime = Time.time;

            if (CanJump)
            {
                ExecuteJump();
            }
        }

        private void ExecuteJump()
        {
            if (rb == null) return;

            // 重置垂直速度后施加跳跃力
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            jumpCount++;
            isGrounded = false;

            OnJump();
        }

        /// <summary>
        /// 跳跃时的回调（可用于音效、动画等）
        /// </summary>
        protected virtual void OnJump()
        {
            // 子类可重写
        }

        /// <summary>
        /// 设置跳跃参数（运行时调整）
        /// </summary>
        public void SetJumpForce(float force)
        {
            jumpForce = force;
        }

        /// <summary>
        /// 启用/禁用二段跳
        /// </summary>
        public void SetDoubleJump(bool enabled)
        {
            maxJumpCount = enabled ? 2 : 1;
        }
    }
}
