using Unity.Netcode;
using UnityEngine;

public class TruckDepositZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; 

        CarriableItem item = other.GetComponentInParent<CarriableItem>();

        if (item != null && item.enabled && !item.IsHeld() && !item.isScored.Value)
        {
            if (item.itemType == ItemType.Pipe) return;
            
            item.isScored.Value = true; 

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(item.dollarValue);
            }
        }
    }
}