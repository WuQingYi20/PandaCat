using UnityEngine;

namespace BearCar.Player
{
    /// <summary>
    /// 熊的动画配置 - 可在 Inspector 中配置
    /// 美术人员可以直接拖拽 Sprite 到对应数组中
    ///
    /// 创建方法: 右键 -> Create -> BearCar -> Bear Animation Config
    /// </summary>
    [CreateAssetMenu(fileName = "BearAnimationConfig", menuName = "BearCar/Bear Animation Config")]
    public class BearAnimationConfig : ScriptableObject
    {
        [Header("=== Player 1 动画 ===")]
        [Tooltip("Player 1 待机动画帧")]
        public Sprite[] player1Idle;

        [Tooltip("Player 1 行走动画帧")]
        public Sprite[] player1Walk;

        [Tooltip("Player 1 推车动画帧")]
        public Sprite[] player1Push;

        [Header("=== Player 2 动画 ===")]
        [Tooltip("Player 2 待机动画帧")]
        public Sprite[] player2Idle;

        [Tooltip("Player 2 行走动画帧")]
        public Sprite[] player2Walk;

        [Tooltip("Player 2 推车动画帧")]
        public Sprite[] player2Push;

        [Header("=== 动画设置 ===")]
        [Tooltip("动画播放速度 (帧/秒)")]
        [Range(1f, 30f)]
        public float frameRate = 8f;

        [Header("=== 玩家颜色 ===")]
        [Tooltip("Player 1 的颜色叠加")]
        public Color player1Tint = Color.white;

        [Tooltip("Player 2 的颜色叠加")]
        public Color player2Tint = Color.white;

        /// <summary>
        /// 获取指定玩家的动画帧
        /// 注意: Player 1 (playerIndex=0) = 绿熊, Player 2 (playerIndex=1) = 红熊
        /// 如果资源中 player1 是红熊图像，这里做反转映射
        /// </summary>
        public Sprite[] GetIdleFrames(int playerIndex)
        {
            // 反转映射：playerIndex=0(绿熊) 使用 player2 动画，playerIndex=1(红熊) 使用 player1 动画
            return playerIndex == 0 ? player2Idle : player1Idle;
        }

        public Sprite[] GetWalkFrames(int playerIndex)
        {
            return playerIndex == 0 ? player2Walk : player1Walk;
        }

        public Sprite[] GetPushFrames(int playerIndex)
        {
            return playerIndex == 0 ? player2Push : player1Push;
        }

        public Color GetPlayerTint(int playerIndex)
        {
            return playerIndex == 0 ? player2Tint : player1Tint;
        }
    }
}
