using UnityEngine;
using System.Linq;

namespace BearCar.Player
{
    /// <summary>
    /// 熊的帧动画控制器
    /// 根据状态切换 Idle / Walk / Push 动画
    ///
    /// 配置方式（二选一）:
    /// 1. 使用 BearAnimationConfig (推荐) - 在 Inspector 中拖入配置文件
    /// 2. 自动从 Resources/Animation 文件夹加载
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BearAnimator : MonoBehaviour
    {
        [Header("=== 动画配置 (推荐) ===")]
        [Tooltip("拖入 BearAnimationConfig 配置文件\n创建方法: 右键 -> Create -> BearCar -> Bear Animation Config")]
        [SerializeField] private BearAnimationConfig animationConfig;

        [Header("=== 手动配置 (可选) ===")]
        [Tooltip("如果不使用 Config，可以直接在这里拖入动画帧")]
        [SerializeField] private float frameRate = 8f;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] pushFrames;

        [Header("=== 颜色设置 ===")]
        [SerializeField] private Color player1Tint = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color player2Tint = new Color(1f, 0.85f, 0.7f);

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
            // 尝试从 Resources 加载配置（如果 Inspector 中没有设置）
            if (animationConfig == null)
            {
                animationConfig = Resources.Load<BearAnimationConfig>("BearAnimationConfig");
            }

            // 如果有配置文件，使用配置文件的设置
            if (animationConfig != null)
            {
                LoadFromConfig();
            }
            // 否则如果没有手动设置动画帧，尝试从 Resources 加载
            else if (idleFrames == null || idleFrames.Length == 0)
            {
                LoadAnimationFrames();
            }

            ApplyPlayerTint();
            SetAnimation(AnimState.Idle);
        }

        private void LoadFromConfig()
        {
            idleFrames = animationConfig.GetIdleFrames(playerIndex);
            walkFrames = animationConfig.GetWalkFrames(playerIndex);
            pushFrames = animationConfig.GetPushFrames(playerIndex);
            frameRate = animationConfig.frameRate;
            player1Tint = animationConfig.player1Tint;
            player2Tint = animationConfig.player2Tint;

            Debug.Log($"[BearAnimator] P{playerIndex + 1} 从配置加载动画: Idle={idleFrames?.Length ?? 0}, Walk={walkFrames?.Length ?? 0}, Push={pushFrames?.Length ?? 0}");
        }

        private void LoadAnimationFrames()
        {
            // 根据玩家索引加载对应的动画套
            // 文件名规则: "-1 " 是 P1 的动画，"-2 " 是 P2 的动画
            string playerSuffix = playerIndex == 0 ? "-1 " : "-2 ";

            idleFrames = LoadPlayerSprites("Animation/Idle", playerSuffix);
            walkFrames = LoadPlayerSprites("Animation/Walk", playerSuffix);
            pushFrames = LoadPlayerSprites("Animation/Push", playerSuffix);

            Debug.Log($"[BearAnimator] P{playerIndex + 1} 从 Resources 加载动画: Idle={idleFrames.Length}, Walk={walkFrames.Length}, Push={pushFrames.Length}");

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
                var playerSprites = allSprites
                    .Where(s => s.name.Contains(playerSuffix))
                    .OrderBy(s => s.name)
                    .ToArray();

                if (playerSprites.Length > 0)
                {
                    return playerSprites;
                }

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

            if (animationConfig != null)
            {
                spriteRenderer.color = animationConfig.GetPlayerTint(playerIndex);
            }
            else
            {
                spriteRenderer.color = playerIndex == 0 ? player1Tint : player2Tint;
            }
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

            // 如果有配置文件，重新加载
            if (animationConfig != null)
            {
                LoadFromConfig();
            }
            else
            {
                LoadAnimationFrames();
            }

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
