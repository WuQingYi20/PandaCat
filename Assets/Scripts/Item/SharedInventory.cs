using UnityEngine;
using System;
using System.Collections.Generic;

namespace BearCar.Item
{
    /// <summary>
    /// 共享道具栏 - 所有玩家共享的道具系统
    /// </summary>
    public class SharedInventory : MonoBehaviour
    {
        public static SharedInventory Instance { get; private set; }

        [Header("=== 槽位设置 ===")]
        [Tooltip("道具槽位数量")]
        [Range(3, 5)]
        [SerializeField] private int slotCount = 4;

        [Header("=== 音效 ===")]
        [SerializeField] private AudioClip rotateSound;
        [SerializeField] private AudioClip emptySound;

        // 道具槽
        private InventorySlot[] slots;
        private int currentIndex = 0;

        // 事件
        public event Action<int> OnSlotChanged;           // 当前选中槽位改变
        public event Action<int, ItemData> OnItemAdded;   // 添加道具
        public event Action<int, ItemData> OnItemRemoved; // 移除道具
        public event Action<int, ItemData> OnItemUsed;    // 使用道具

        public int SlotCount => slotCount;
        public int CurrentIndex => currentIndex;
        public InventorySlot[] Slots => slots;

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSlots();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void InitializeSlots()
        {
            slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                slots[i] = new InventorySlot();
            }
        }

        /// <summary>
        /// 添加道具到背包
        /// </summary>
        public bool AddItem(ItemData item, int count = 1)
        {
            if (item == null) return false;

            // 如果可堆叠，先找已有的槽位
            if (item.stackable)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].item == item && slots[i].count < item.maxStack)
                    {
                        int space = item.maxStack - slots[i].count;
                        int toAdd = Mathf.Min(count, space);
                        slots[i].count += toAdd;
                        OnItemAdded?.Invoke(i, item);
                        Debug.Log($"[Inventory] 堆叠 {item.itemName} x{toAdd} 到槽位 {i}");
                        return true;
                    }
                }
            }

            // 找空槽位
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].item = item;
                    slots[i].count = count;
                    OnItemAdded?.Invoke(i, item);
                    Debug.Log($"[Inventory] 添加 {item.itemName} 到槽位 {i}");
                    return true;
                }
            }

            Debug.Log("[Inventory] 背包已满!");
            return false;
        }

        /// <summary>
        /// 轮换到下一个槽位（共享操作）
        /// </summary>
        public void RotateNext()
        {
            int oldIndex = currentIndex;
            currentIndex = (currentIndex + 1) % slotCount;
            OnSlotChanged?.Invoke(currentIndex);
            PlaySound(rotateSound);
            Debug.Log($"[Inventory] 轮换: {oldIndex} -> {currentIndex}");
        }

        /// <summary>
        /// 轮换到上一个槽位（共享操作）
        /// </summary>
        public void RotatePrev()
        {
            int oldIndex = currentIndex;
            currentIndex = (currentIndex - 1 + slotCount) % slotCount;
            OnSlotChanged?.Invoke(currentIndex);
            PlaySound(rotateSound);
            Debug.Log($"[Inventory] 轮换: {oldIndex} -> {currentIndex}");
        }

        /// <summary>
        /// 使用当前选中的道具
        /// </summary>
        public ItemData UseCurrentItem(int playerIndex)
        {
            return UseItem(currentIndex, playerIndex);
        }

        /// <summary>
        /// 使用指定槽位的道具
        /// </summary>
        public ItemData UseItem(int slotIndex, int playerIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length) return null;

            var slot = slots[slotIndex];
            if (slot.IsEmpty)
            {
                PlaySound(emptySound);
                Debug.Log($"[Inventory] 槽位 {slotIndex} 为空");
                return null;
            }

            ItemData item = slot.item;

            // 消耗道具
            slot.count--;
            if (slot.count <= 0)
            {
                slot.item = null;
                slot.count = 0;
            }

            OnItemUsed?.Invoke(slotIndex, item);
            OnItemRemoved?.Invoke(slotIndex, item);

            if (item.useSound != null)
            {
                PlaySound(item.useSound);
            }

            Debug.Log($"[Inventory] 玩家 {playerIndex} 使用了 {item.itemName}");
            return item;
        }

        /// <summary>
        /// 获取指定槽位
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        /// <summary>
        /// 获取当前选中的槽位
        /// </summary>
        public InventorySlot GetCurrentSlot()
        {
            return slots[currentIndex];
        }

        /// <summary>
        /// 检查背包是否已满
        /// </summary>
        public bool IsFull()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty) return false;
            }
            return true;
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    var item = slots[i].item;
                    slots[i].item = null;
                    slots[i].count = 0;
                    OnItemRemoved?.Invoke(i, item);
                }
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    /// <summary>
    /// 道具槽
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;

        public bool IsEmpty => item == null || count <= 0;
    }
}
