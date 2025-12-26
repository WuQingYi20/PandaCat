using UnityEngine;
using System.Linq;

namespace BearCar.Player
{
    /// <summary>
    /// 熊的帧动画控制器
    /// 根据状态切换 Idle / Walk / Push 动画
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BearAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float frameRate = 8f;

        [Header("Sprite Sheets")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] pushFrames;

        [Header("Player Tint")]
        [SerializeField] private Color player1Tint = new Color(0.8f, 0.9f, 1f);    // 偏蓝
        [SerializeField] private Color player2Tint = new Color(1f, 0.85f, 0.7f);   // 偏橙

        private SpriteRenderer spriteRenderer;
        private LocalBearController bearController;
        private Sprite[] currentAnimation;
        private int currentFrame;
        private float frameTimer;
        private bool facingRight = true;
        private int playerIndex = 0;

        public enum AnimState { Idle, Walk, Push }
        private AnimState currentState = AnimState.Idle;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            bearController = GetComponent<LocalBearController>();
        }

        private void Start()
        {
            // 加载动画帧（如果没有在 Inspector 中设置）
            if (idleFrames == null || idleFrames.Length == 0)
            {
                LoadAnimationFrames();
            }

            // 设置玩家颜色
            ApplyPlayerTint();

            // 初始动画
            SetAnimation(AnimState.Idle);
        }

        private void LoadAnimationFrames()
        {
            // 根据玩家索引加载对应的动画套
            // 文件名规则: "-1 " 是 P1 的动画，"-2 " 是 P2 的动画
            string playerSuffix = playerIndex == 0 ? "-1 " : "-2 ";

            idleFrames = LoadPlayerSprites("Animation/Idle", playerSuffix);
            walkFrames = LoadPlayerSprites("Animation/Walk", playerSuffix);
            pushFrames = LoadPlayerSprites("Animation/Push", playerSuffix);

            Debug.Log($"[BearAnimator] P{playerIndex + 1} 加载动画: Idle={idleFrames.Length}, Walk={walkFrames.Length}, Push={pushFrames.Length}");

            if (idleFrames.Length == 0)
            {
                Debug.LogWarning($"[BearAnimator] P{playerIndex + 1} 未找到动画帧，使用备用颜色方块");
            }
        }

        private Sprite[] LoadPlayerSprites(string path, string playerSuffix)
        {
            var allSprites = Resources.LoadAll<Sprite>(path);
            if (allSprites.Length > 0)
            {
                // 过滤出该玩家的动画帧，并按名称排序
                var playerSprites = allSprites
                    .Where(s => s.name.Contains(playerSuffix))
                    .OrderBy(s => s.name)
                    .ToArray();

                if (playerSprites.Length > 0)
                {
                    return playerSprites;
                }

                // 如果没有找到对应玩家的，返回所有的（兼容旧资源）
                return allSprites.OrderBy(s => s.name).ToArray();
            }
            return allSprites;
        }

        private void ApplyPlayerTint()
        {
            if (bearController != null)
            {
                playerIndex = bearController.PlayerIndex;
            }
            spriteRenderer.color = playerIndex == 0 ? player1Tint : player2Tint;
        }

        private void Update()
        {
            if (bearController == null) return;

            // 根据状态决定动画
            AnimState targetState;
            if (bearController.IsAttached && bearController.IsPushing)
            {
                targetState = AnimState.Push;
            }
            else if (Mathf.Abs(bearController.MoveInput.x) > 0.1f && !bearController.IsAttached)
            {
                targetState = AnimState.Walk;
            }
            else
            {
                targetState = AnimState.Idle;
            }

            // 切换动画
            if (targetState != currentState)
            {
                SetAnimation(targetState);
            }

            // 更新朝向
            if (!bearController.IsAttached)
            {
                if (bearController.MoveInput.x > 0.1f) facingRight = true;
                else if (bearController.MoveInput.x < -0.1f) facingRight = false;
            }
            spriteRenderer.flipX = !facingRight;

            // 播放动画帧
            UpdateAnimation();
        }

        private void SetAnimation(AnimState state)
        {
            currentState = state;
            currentFrame = 0;
            frameTimer = 0f;

            currentAnimation = state switch
            {
                AnimState.Idle => idleFrames,
                AnimState.Walk => walkFrames,
                AnimState.Push => pushFrames,
                _ => idleFrames
            };

            if (currentAnimation != null && currentAnimation.Length > 0)
            {
                spriteRenderer.sprite = currentAnimation[0];
            }
        }

        private void UpdateAnimation()
        {
            if (currentAnimation == null || currentAnimation.Length == 0) return;

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / frameRate;

            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrame = (currentFrame + 1) % currentAnimation.Length;
                spriteRenderer.sprite = currentAnimation[currentFrame];
            }
        }

        /// <summary>
        /// 设置玩家索引（用于动画和颜色区分）
        /// </summary>
        public void SetPlayerIndex(int index)
        {
            playerIndex = index;

            // 重新加载该玩家的动画
            LoadAnimationFrames();

            // 应用颜色
            if (spriteRenderer != null)
            {
                ApplyPlayerTint();
            }

            // 重置动画
            SetAnimation(AnimState.Idle);
        }
    }
}
