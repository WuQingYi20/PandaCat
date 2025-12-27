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

        // 双玩家选择指针
        private int greenBearIndex = 0;  // P1 绿熊的选中槽位
        private int redBearIndex = 0;    // P2 红熊的选中槽位

        // 事件
        public event Action<int, int> OnSlotChanged;      // playerIndex, slotIndex
        public event Action<int, ItemData> OnItemAdded;   // 添加道具
        public event Action<int, ItemData> OnItemRemoved; // 移除道具
        public event Action<int, int, ItemData> OnItemUsed;    // playerIndex, slotIndex, item
        public event Action<ItemData, ItemData> OnComboReady;  // 组合道具就绪
        public event Action<int, int, ItemData, ItemData> OnComboTriggered; // p1Index, p2Index, item1, item2

        public int SlotCount => slotCount;
        public int GreenBearIndex => greenBearIndex;
        public int RedBearIndex => redBearIndex;
        public InventorySlot[] Slots => slots;

        // 兼容旧代码
        [System.Obsolete("Use GreenBearIndex or RedBearIndex instead")]
        public int CurrentIndex => greenBearIndex;

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
                        CheckForCombo(item);
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
                    CheckForCombo(item);
                    return true;
                }
            }

            Debug.Log("[Inventory] 背包已满!");
            return false;
        }

        /// <summary>
        /// 检查是否有可用的组合
        /// </summary>
        private void CheckForCombo(ItemData newItem)
        {
            if (newItem == null || !newItem.isComboTrigger || newItem.comboPartner == null)
                return;

            // 检查背包中是否有组合伙伴
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == newItem.comboPartner)
                {
                    Debug.Log($"[Inventory] 🎉 组合就绪: {newItem.itemName} + {newItem.comboPartner.itemName}!");
                    OnComboReady?.Invoke(newItem, newItem.comboPartner);
                    return;
                }
            }

            // 也检查反向组合
            for (int i = 0; i < slots.Length; i++)
            {
                var slotItem = slots[i].item;
                if (slotItem != null && slotItem.isComboTrigger && slotItem.comboPartner == newItem)
                {
                    Debug.Log($"[Inventory] 🎉 组合就绪: {slotItem.itemName} + {newItem.itemName}!");
                    OnComboReady?.Invoke(slotItem, newItem);
                    return;
                }
            }
        }

        /// <summary>
        /// 检查当前是否有可用的组合
        /// </summary>
        public (ItemData, ItemData) GetAvailableCombo()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var item = slots[i].item;
                if (item != null && item.isComboTrigger && item.comboPartner != null)
                {
                    // 检查是否有组合伙伴
                    for (int j = 0; j < slots.Length; j++)
                    {
                        if (i != j && slots[j].item == item.comboPartner)
                        {
                            return (item, item.comboPartner);
                        }
                    }
                }
            }
            return (null, null);
        }

        /// <summary>
        /// 触发组合效果（使用双人组合模式）
        /// </summary>
        [System.Obsolete("Use TriggerDualPlayerCombo() instead")]
        public bool TriggerCombo(int playerIndex)
        {
            return TriggerDualPlayerCombo();
        }

        /// <summary>
        /// 获取指定玩家当前选中的槽位索引
        /// </summary>
        public int GetPlayerIndex(int playerIndex)
        {
            return playerIndex == 0 ? greenBearIndex : redBearIndex;
        }

        /// <summary>
        /// 轮换到下一个槽位（玩家专属）
        /// </summary>
        public void RotateNext(int playerIndex)
        {
            if (playerIndex == 0)
            {
                int oldIndex = greenBearIndex;
                greenBearIndex = (greenBearIndex + 1) % slotCount;
                OnSlotChanged?.Invoke(playerIndex, greenBearIndex);
                Debug.Log($"[Inventory] 绿熊轮换: {oldIndex} -> {greenBearIndex}");
            }
            else
            {
                int oldIndex = redBearIndex;
                redBearIndex = (redBearIndex + 1) % slotCount;
                OnSlotChanged?.Invoke(playerIndex, redBearIndex);
                Debug.Log($"[Inventory] 红熊轮换: {oldIndex} -> {redBearIndex}");
            }

            PlaySound(rotateSound);
            CheckForDualPlayerCombo();
        }

        /// <summary>
        /// 轮换到上一个槽位（玩家专属）
        /// </summary>
        public void RotatePrev(int playerIndex)
        {
            if (playerIndex == 0)
            {
                int oldIndex = greenBearIndex;
                greenBearIndex = (greenBearIndex - 1 + slotCount) % slotCount;
                OnSlotChanged?.Invoke(playerIndex, greenBearIndex);
                Debug.Log($"[Inventory] 绿熊轮换: {oldIndex} -> {greenBearIndex}");
            }
            else
            {
                int oldIndex = redBearIndex;
                redBearIndex = (redBearIndex - 1 + slotCount) % slotCount;
                OnSlotChanged?.Invoke(playerIndex, redBearIndex);
                Debug.Log($"[Inventory] 红熊轮换: {oldIndex} -> {redBearIndex}");
            }

            PlaySound(rotateSound);
            CheckForDualPlayerCombo();
        }

        /// <summary>
        /// 兼容旧API - 轮换下一个（默认绿熊）
        /// </summary>
        [System.Obsolete("Use RotateNext(playerIndex) instead")]
        public void RotateNext() => RotateNext(0);

        /// <summary>
        /// 兼容旧API - 轮换上一个（默认绿熊）
        /// </summary>
        [System.Obsolete("Use RotatePrev(playerIndex) instead")]
        public void RotatePrev() => RotatePrev(0);

        /// <summary>
        /// 使用当前玩家选中的道具
        /// </summary>
        public ItemData UseCurrentItem(int playerIndex)
        {
            int slotIndex = playerIndex == 0 ? greenBearIndex : redBearIndex;
            return UseItem(slotIndex, playerIndex);
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

            OnItemUsed?.Invoke(playerIndex, slotIndex, item);
            OnItemRemoved?.Invoke(slotIndex, item);

            if (item.useSound != null)
            {
                PlaySound(item.useSound);
            }

            string playerName = playerIndex == 0 ? "绿熊" : "红熊";
            Debug.Log($"[Inventory] {playerName} 使用了 {item.itemName}");
            return item;
        }

        /// <summary>
        /// 检查双玩家组合 - 当两个玩家各自选中组合道具的两部分时触发
        /// </summary>
        private void CheckForDualPlayerCombo()
        {
            var greenSlot = GetSlot(greenBearIndex);
            var redSlot = GetSlot(redBearIndex);

            if (greenSlot == null || greenSlot.IsEmpty) return;
            if (redSlot == null || redSlot.IsEmpty) return;

            var greenItem = greenSlot.item;
            var redItem = redSlot.item;

            // 检查是否形成组合
            bool isCombo = false;

            if (greenItem.isComboTrigger && greenItem.comboPartner == redItem)
            {
                isCombo = true;
            }
            else if (redItem.isComboTrigger && redItem.comboPartner == greenItem)
            {
                isCombo = true;
            }

            if (isCombo)
            {
                Debug.Log($"[Inventory] 🎉 双人组合就绪: 绿熊选中 {greenItem.itemName}，红熊选中 {redItem.itemName}!");
                OnComboReady?.Invoke(greenItem, redItem);
            }
        }

        /// <summary>
        /// 触发双人组合效果 - 两个玩家同时消耗各自选中的道具
        /// </summary>
        public bool TriggerDualPlayerCombo()
        {
            var greenSlot = GetSlot(greenBearIndex);
            var redSlot = GetSlot(redBearIndex);

            if (greenSlot == null || greenSlot.IsEmpty) return false;
            if (redSlot == null || redSlot.IsEmpty) return false;

            var greenItem = greenSlot.item;
            var redItem = redSlot.item;

            // 检查是否形成组合
            ItemData triggerItem = null;
            if (greenItem.isComboTrigger && greenItem.comboPartner == redItem)
            {
                triggerItem = greenItem;
            }
            else if (redItem.isComboTrigger && redItem.comboPartner == greenItem)
            {
                triggerItem = redItem;
            }

            if (triggerItem == null)
            {
                Debug.Log("[Inventory] 当前选中的道具无法组合");
                return false;
            }

            // 消耗两个道具
            greenSlot.count--;
            if (greenSlot.count <= 0)
            {
                greenSlot.item = null;
                greenSlot.count = 0;
            }
            OnItemRemoved?.Invoke(greenBearIndex, greenItem);

            redSlot.count--;
            if (redSlot.count <= 0)
            {
                redSlot.item = null;
                redSlot.count = 0;
            }
            OnItemRemoved?.Invoke(redBearIndex, redItem);

            Debug.Log($"[Inventory] 🚀 双人组合触发: {greenItem.itemName} + {redItem.itemName}!");
            OnComboTriggered?.Invoke(greenBearIndex, redBearIndex, greenItem, redItem);

            return true;
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
        /// 获取指定玩家当前选中的槽位
        /// </summary>
        public InventorySlot GetPlayerCurrentSlot(int playerIndex)
        {
            int index = playerIndex == 0 ? greenBearIndex : redBearIndex;
            return slots[index];
        }

        /// <summary>
        /// 兼容旧API - 获取当前选中的槽位（默认绿熊）
        /// </summary>
        [System.Obsolete("Use GetPlayerCurrentSlot(playerIndex) instead")]
        public InventorySlot GetCurrentSlot()
        {
            return slots[greenBearIndex];
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
