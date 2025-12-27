using UnityEngine;
using System.Collections.Generic;
using BearCar.Player;
using BearCar.Cart;

namespace BearCar.Level
{
    /// <summary>
    /// 压力板 - 用于触发传送门和其他机关
    ///
    /// 支持:
    /// - 单人/多人触发
    /// - 持续按压/一次性触发
    /// - 重量检测（车比熊重）
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        [Header("=== 基础设置 ===")]
        [Tooltip("连接的机关（传送门等）")]
        public GameObject[] linkedObjects;

        [Header("=== 触发条件 ===")]
        [Tooltip("需要的重量（熊=1，车=5）")]
        public float requiredWeight = 1f;

        [Tooltip("需要持续按压多久才触发")]
        public float holdTime = 0f;

        [Tooltip("一次性触发还是持续检测")]
        public bool oneShot = false;

        [Header("=== 视觉设置 ===")]
        public Color normalColor = new Color(0.6f, 0.6f, 0.6f);
        public Color pressedColor = new Color(0.3f, 0.8f, 0.3f);
        public Color activatedColor = new Color(0.2f, 1f, 0.4f);

        [Header("=== 音效 ===")]
        public AudioClip pressSound;
        public AudioClip activateSound;

        // 运行时
        private float currentWeight = 0f;
        private float holdTimer = 0f;
        private bool isPressed = false;
        private bool hasActivated = false;
        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        private HashSet<GameObject> objectsOnPlate = new HashSet<GameObject>();

        public bool IsPressed => isPressed;
        public bool HasActivated => hasActivated;

        public System.Action OnPressed;
        public System.Action OnReleased;
        public System.Action OnActivated;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();

            SetupVisuals();
            SetupCollider();
        }

        private void SetupVisuals()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (spriteRenderer.sprite == null)
            {
                // 创建压力板外观
                int w = 64, h = 16;
                Texture2D tex = new Texture2D(w, h);
                Color[] pixels = new Color[w * h];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            }

            spriteRenderer.color = normalColor;
            spriteRenderer.sortingOrder = -1;
        }

        private void SetupCollider()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2f, 0.5f);
            }
            col.isTrigger = true;
        }

        private void Update()
        {
            if (oneShot && hasActivated) return;

            // 检查是否达到重量要求
            bool enoughWeight = currentWeight >= requiredWeight;

            if (enoughWeight)
            {
                if (!isPressed)
                {
                    isPressed = true;
                    OnPressed?.Invoke();
                    PlaySound(pressSound);
                }

                holdTimer += Time.deltaTime;

                if (holdTimer >= holdTime && !hasActivated)
                {
                    Activate();
                }
            }
            else
            {
                if (isPressed)
                {
                    isPressed = false;
                    holdTimer = 0f;
                    OnReleased?.Invoke();

                    // 非一次性触发时，释放后关闭机关
                    if (!oneShot && hasActivated)
                    {
                        Deactivate();
                    }
                }
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer == null) return;

            Color targetColor;
            if (hasActivated)
            {
                targetColor = activatedColor;
            }
            else if (isPressed)
            {
                float progress = holdTime > 0 ? holdTimer / holdTime : 1f;
                targetColor = Color.Lerp(pressedColor, activatedColor, progress);
            }
            else
            {
                targetColor = normalColor;
            }

            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 10f);

            // 按下时略微下沉
            float targetY = isPressed ? -0.05f : 0f;
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * 10f);
        }

        private void Activate()
        {
            hasActivated = true;
            PlaySound(activateSound);
            OnActivated?.Invoke();

            // 激活连接的机关
            foreach (var obj in linkedObjects)
            {
                if (obj == null) continue;

                // 尝试激活传送门
                var portal = obj.GetComponent<Portal>();
                if (portal != null)
                {
                    portal.Activate();
                    continue;
                }

                // 尝试激活其他 PressurePlate 联动的对象
                obj.SendMessage("OnPressurePlateActivated", SendMessageOptions.DontRequireReceiver);
            }

            Debug.Log($"[PressurePlate] 激活! 当前重量: {currentWeight}");
        }

        private void Deactivate()
        {
            hasActivated = false;

            foreach (var obj in linkedObjects)
            {
                if (obj == null) continue;

                var portal = obj.GetComponent<Portal>();
                if (portal != null)
                {
                    portal.Deactivate();
                    continue;
                }

                obj.SendMessage("OnPressurePlateDeactivated", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            float weight = GetObjectWeight(other.gameObject);
            if (weight > 0 && !objectsOnPlate.Contains(other.gameObject))
            {
                objectsOnPlate.Add(other.gameObject);
                currentWeight += weight;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (objectsOnPlate.Contains(other.gameObject))
            {
                objectsOnPlate.Remove(other.gameObject);
                currentWeight -= GetObjectWeight(other.gameObject);
                currentWeight = Mathf.Max(0, currentWeight);
            }
        }

        private float GetObjectWeight(GameObject obj)
        {
            if (obj.GetComponent<CartController>() != null) return 5f;
            if (obj.GetComponent<LocalBearController>() != null) return 1f;
            if (obj.GetComponent<BearController>() != null) return 1f;
            if (obj.GetComponent<BearAI>() != null) return 1f;
            return 0f;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            audioSource.PlayOneShot(clip);
        }

        private void OnDrawGizmos()
        {
            // 绘制压力板
            Gizmos.color = hasActivated ? activatedColor : (isPressed ? pressedColor : normalColor);
            Gizmos.DrawCube(transform.position, new Vector3(2f, 0.3f, 0.1f));

            // 绘制到连接对象的线
            if (linkedObjects != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var obj in linkedObjects)
                {
                    if (obj != null)
                    {
                        Gizmos.DrawLine(transform.position, obj.transform.position);
                    }
                }
            }
        }
    }
}
