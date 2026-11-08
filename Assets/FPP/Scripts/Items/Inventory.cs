using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    [Min(0)] public int count;
}

/// <summary>
/// Simple slot and weight based inventory. Put it on the FPP Player.
/// You can pre-fill "Slots" in the Inspector for testing.
/// </summary>
public sealed class Inventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int slotCapacity = 12;
    [Tooltip("0 = no weight limit.")]
    [SerializeField, Min(0f)] private float maxWeight = 0f;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    public event Action Changed;

    public IReadOnlyList<InventorySlot> Slots => slots;
    public int SlotCapacity => slotCapacity;
    public float MaxWeight => maxWeight;

    public float TotalWeight
    {
        get
        {
            float total = 0f;
            foreach (InventorySlot s in slots)
                if (s.item != null)
                    total += s.item.Weight * s.count;
            return total;
        }
    }

    /// <summary>Adds items. Returns how many were actually added (0 if full or too heavy).</summary>
    public int Add(ItemData item, int count = 1)
    {
        if (item == null || count <= 0)
            return 0;

        int added = 0;

        for (int i = 0; i < count; i++)
        {
            if (maxWeight > 0f && TotalWeight + item.Weight > maxWeight + 0.0001f)
                break;

            InventorySlot slot = slots.Find(s => s.item == item && s.count < item.MaxStack);
            if (slot == null)
            {
                if (slots.Count >= slotCapacity)
                    break;

                slot = new InventorySlot { item = item, count = 0 };
                slots.Add(slot);
            }

            slot.count++;
            added++;
        }

        if (added > 0)
            Changed?.Invoke();

        return added;
    }

    public int Count(ItemData item)
    {
        int total = 0;
        foreach (InventorySlot s in slots)
            if (s.item == item)
                total += s.count;
        return total;
    }

    public bool Has(ItemData item, int count = 1) => item != null && Count(item) >= count;

    public bool Remove(ItemData item, int count = 1)
    {
        if (!Has(item, count))
            return false;

        for (int i = slots.Count - 1; i >= 0 && count > 0; i--)
        {
            if (slots[i].item != item)
                continue;

            int take = Mathf.Min(count, slots[i].count);
            slots[i].count -= take;
            count -= take;

            if (slots[i].count <= 0)
                slots.RemoveAt(i);
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>Drops one item from a slot into the world in front of 'origin'.</summary>
    public bool DropFromSlot(int index, Transform origin, float throwSpeed = 2f)
    {
        if (index < 0 || index >= slots.Count || origin == null)
            return false;

        ItemData item = slots[index].item;
        if (item == null)
            return false;

        if (item.WorldPrefab == null)
        {
            Notifier.Show($"Can't drop {item.DisplayName}");
            return false;
        }

        Vector3 position = origin.position + origin.forward * 0.9f;
        GameObject spawned = Instantiate(item.WorldPrefab, position, Quaternion.identity);

        Rigidbody body = spawned.GetComponent<Rigidbody>();
        if (body != null)
            body.AddForce(origin.forward * throwSpeed, ForceMode.VelocityChange);

        Remove(item, 1);
        Notifier.Show($"Dropped {item.DisplayName}");
        return true;
    }
}