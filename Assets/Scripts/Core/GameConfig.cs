using UnityEngine;

namespace BearCar.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BearCar/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Cart Physics")]
        [Tooltip("车辆质量")]
        public float cartMass = 10f;

        [Tooltip("车辆阻力")]
        public float cartDrag = 0.5f;

        [Header("Push Force")]
        [Tooltip("单人推力 (设计为刚好抵消坡道重力)")]
        public float singlePushForce = 50f;

        [Tooltip("双人推力倍数")]
        public float doublePushMultiplier = 2.0f;

        [Header("Slope")]
        [Tooltip("坡道角度 (度)")]
        public float slopeAngle = 30f;

        [Header("Stamina")]
        [Tooltip("最大体力 (秒)")]
        public float maxStamina = 5f;

        [Tooltip("体力消耗速度 (/秒)")]
        public float staminaDrainRate = 1f;

        [Tooltip("体力恢复速度 (/秒)")]
        public float staminaRecoveryRate = 0.5f;

        [Tooltip("体力耗尽后恢复延迟 (秒)")]
        public float exhaustedRecoveryDelay = 1f;

        [Header("Player")]
        [Tooltip("玩家移动速度")]
        public float playerMoveSpeed = 5f;

        public float CalculateSlopeGravityForce(float mass)
        {
            return mass * Mathf.Abs(Physics2D.gravity.y) * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
        }
    }
}
