using UnityEngine;
using BearCar.Cart;
using BearCar.Player;
using System.Collections.Generic;

namespace BearCar.Core
{
    /// <summary>
    /// 双人合作智能相机
    /// - 以车为主焦点，同时考虑两只熊的位置
    /// - 动态缩放确保所有角色可见
    /// - 平滑过渡避免抖动
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("=== 跟随目标 ===")]
        [SerializeField] private Transform cartTarget;
        [SerializeField] private float cartWeight = 0.6f;      // 车的权重
        [SerializeField] private float playerWeight = 0.2f;    // 每个玩家的权重

        [Header("=== 基础设置 ===")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2, -10);
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float zoomSmoothSpeed = 3f;

        [Header("=== 动态缩放 ===")]
        [SerializeField] private float minOrthoSize = 5f;      // 最小相机大小（聚集时）
        [SerializeField] private float maxOrthoSize = 12f;     // 最大相机大小（分散时）
        [SerializeField] private float paddingX = 2f;          // 水平边距
        [SerializeField] private float paddingY = 1.5f;        // 垂直边距

        [Header("=== 边界限制 ===")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 100f;

        [Header("=== 前瞻 ===")]
        [Tooltip("根据车的速度提前移动相机")]
        [SerializeField] private float lookAheadFactor = 1f;
        [SerializeField] private float lookAheadSmooth = 3f;

        // 缓存
        private Camera cam;
        private CartController cart;
        private List<LocalBearController> players = new List<LocalBearController>();
        private float targetOrthoSize;
        private Vector3 lookAheadOffset;
        private Vector3 currentLookAhead;

        // 刷新控制
        private float lastRefreshTime;
        private const float REFRESH_INTERVAL = 0.5f;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            targetOrthoSize = cam != null ? cam.orthographicSize : minOrthoSize;

            // 初始查找目标
            RefreshTargets();

            // 确保Z轴正确
            if (offset.z >= 0)
            {
                offset.z = -10f;
            }
        }

        private void LateUpdate()
        {
            // 定期刷新目标列表
            if (Time.time - lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshTargets();
                lastRefreshTime = Time.time;
            }

            // 计算焦点和缩放
            Vector3 focusPoint = CalculateFocusPoint();
            float requiredSize = CalculateRequiredSize();

            // 计算前瞻偏移
            UpdateLookAhead();

            // 计算最终目标位置
            Vector3 desiredPosition = focusPoint + offset + currentLookAhead;

            // 应用边界限制
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            }

            // 保持Z轴
            desiredPosition.z = offset.z;

            // 平滑移动
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // 平滑缩放
            if (cam != null && cam.orthographic)
            {
                targetOrthoSize = Mathf.Clamp(requiredSize, minOrthoSize, maxOrthoSize);
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, zoomSmoothSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 刷新跟随目标
        /// </summary>
        private void RefreshTargets()
        {
            // 查找车
            if (cartTarget == null || cart == null)
            {
                cart = FindFirstObjectByType<CartController>();
                if (cart != null)
                {
                    cartTarget = cart.transform;
                }
            }

            // 查找所有本地玩家
            players.Clear();
            var allPlayers = FindObjectsByType<LocalBearController>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                players.Add(player);
            }
        }

        /// <summary>
        /// 计算加权焦点位置
        /// </summary>
        private Vector3 CalculateFocusPoint()
        {
            Vector3 focus = Vector3.zero;
            float totalWeight = 0f;

            // 车的贡献
            if (cartTarget != null)
            {
                focus += cartTarget.position * cartWeight;
                totalWeight += cartWeight;
            }

            // 玩家的贡献
            foreach (var player in players)
            {
                if (player != null)
                {
                    focus += player.transform.position * playerWeight;
                    totalWeight += playerWeight;
                }
            }

            // 归一化
            if (totalWeight > 0)
            {
                focus /= totalWeight;
            }
            else if (cartTarget != null)
            {
                // 回退到只跟随车
                focus = cartTarget.position;
            }

            return focus;
        }

        /// <summary>
        /// 计算需要的相机大小以包含所有目标
        /// </summary>
        private float CalculateRequiredSize()
        {
            if (cam == null || !cam.orthographic) return minOrthoSize;

            // 收集所有目标位置
            List<Vector3> positions = new List<Vector3>();

            if (cartTarget != null)
            {
                positions.Add(cartTarget.position);
            }

            foreach (var player in players)
            {
                if (player != null)
                {
                    positions.Add(player.transform.position);
                }
            }

            if (positions.Count == 0) return minOrthoSize;

            // 计算边界框
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var pos in positions)
            {
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }

            // 添加边距
            float width = (maxX - minX) + paddingX * 2;
            float height = (maxY - minY) + paddingY * 2;

            // 计算需要的正交大小
            float aspectRatio = cam.aspect;
            float sizeForWidth = width / (2f * aspectRatio);
            float sizeForHeight = height / 2f;

            // 取较大值确保都能看到
            return Mathf.Max(sizeForWidth, sizeForHeight);
        }

        /// <summary>
        /// 更新前瞻偏移（根据车的速度）
        /// </summary>
        private void UpdateLookAhead()
        {
            if (cart == null)
            {
                currentLookAhead = Vector3.Lerp(currentLookAhead, Vector3.zero, lookAheadSmooth * Time.deltaTime);
                return;
            }

            var rb = cart.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 根据车的水平速度计算前瞻
                float velocityX = rb.linearVelocity.x;
                lookAheadOffset = new Vector3(velocityX * lookAheadFactor, 0, 0);

                // 限制最大前瞻距离
                lookAheadOffset.x = Mathf.Clamp(lookAheadOffset.x, -3f, 3f);
            }

            // 平滑过渡
            currentLookAhead = Vector3.Lerp(currentLookAhead, lookAheadOffset, lookAheadSmooth * Time.deltaTime);
        }

        /// <summary>
        /// 设置跟随目标（兼容旧接口）
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            cartTarget = newTarget;
            cart = newTarget != null ? newTarget.GetComponent<CartController>() : null;
        }

        /// <summary>
        /// 立即跳转到目标位置（用于场景切换等）
        /// </summary>
        public void SnapToTarget()
        {
            RefreshTargets();
            Vector3 focusPoint = CalculateFocusPoint();
            transform.position = focusPoint + offset;

            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = CalculateRequiredSize();
            }
        }

        /// <summary>
        /// 设置权重
        /// </summary>
        public void SetWeights(float cart, float player)
        {
            cartWeight = Mathf.Clamp01(cart);
            playerWeight = Mathf.Clamp01(player);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 显示相机视野范围
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null || !cam.orthographic) return;

            float size = cam.orthographicSize;
            float aspect = cam.aspect;

            Vector3 center = transform.position;
            center.z = 0;

            // 绘制视野框
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(center, new Vector3(size * 2 * aspect, size * 2, 0.1f));

            // 显示焦点
            if (Application.isPlaying)
            {
                Vector3 focus = CalculateFocusPoint();
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(focus, 0.5f);
            }

            // 显示边距
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            float paddedWidth = (size - paddingY) * 2 * aspect;
            float paddedHeight = (size - paddingY) * 2;
            Gizmos.DrawWireCube(center, new Vector3(paddedWidth, paddedHeight, 0.1f));
        }
#endif
    }
}
