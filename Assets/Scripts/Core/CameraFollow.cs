using UnityEngine;
using BearCar.Cart;

namespace BearCar.Core
{
    /// <summary>
    /// 摄像机跟随 - 跟随车辆移动
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 2, -10);

        [Header("Bounds")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 100f;

        private void Start()
        {
            // 如果没有指定目标，尝试找到车
            if (target == null)
            {
                var cart = FindFirstObjectByType<CartController>();
                if (cart != null)
                {
                    target = cart.transform;
                    Debug.Log("[Camera] 自动跟随车辆");
                }
            }

            // 确保摄像机有正确的Z位置
            if (offset.z >= 0)
            {
                offset.z = -10f;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                // 尝试重新找目标
                var cart = FindFirstObjectByType<CartController>();
                if (cart != null)
                {
                    target = cart.transform;
                }
                return;
            }

            // 计算目标位置
            Vector3 desiredPosition = target.position + offset;

            // 应用边界限制
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            }

            // 保持Z轴位置（2D游戏）
            desiredPosition.z = offset.z;

            // 平滑移动
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }

        /// <summary>
        /// 设置跟随目标
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
