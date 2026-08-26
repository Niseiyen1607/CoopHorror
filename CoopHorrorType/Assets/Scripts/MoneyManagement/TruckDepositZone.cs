using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TruckDepositZone : NetworkBehaviour
{
    [Header("Effets Audio (Optionnel)")]
    public AudioClip depositSoundClip;
    public AudioClip removeSoundClip; 

    private List<CarriableItem> itemsInZone = new List<CarriableItem>();

    private void Update()
    {
        if (!IsServer) return;

        for (int i = itemsInZone.Count - 1; i >= 0; i--)
        {
            CarriableItem item = itemsInZone[i];

            if (item == null || item.IsHeld() || !item.enabled)
            {
                RemoveItemFromZone(item);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        CarriableItem item = other.GetComponentInParent<CarriableItem>();

        if (item != null && item.enabled && !item.IsHeld() && !item.isScored.Value)
        {
            if (item.itemType == ItemType.Pipe) return; 

            AddItemToZone(item);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        CarriableItem item = other.GetComponentInParent<CarriableItem>();

        if (item != null && itemsInZone.Contains(item))
        {
            RemoveItemFromZone(item);
        }
    }

    private void AddItemToZone(CarriableItem item)
    {
        if (item == null) return;

        item.isScored.Value = true;
        if (!itemsInZone.Contains(item)) itemsInZone.Add(item);

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(item.dollarValue);
        }

        if (AudioManager.Instance != null && depositSoundClip != null)
        {
            AudioManager.Instance.PlaySound3D(depositSoundClip, item.transform.position, volume: 0.7f);
        }
    }

    private void RemoveItemFromZone(CarriableItem item)
    {
        if (item != null)
        {
            item.isScored.Value = false;
            itemsInZone.Remove(item);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.RemoveMoney(item.dollarValue);
            }

            if (AudioManager.Instance != null && removeSoundClip != null)
            {
                AudioManager.Instance.PlaySound3D(removeSoundClip, item.transform.position, volume: 0.5f);
            }
        }
    }
}