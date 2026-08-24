using Unity.Netcode;
using UnityEngine;

public class TruckDepositZone : NetworkBehaviour
{
    [Header("Objectif de Mission")]
    public int moneyGoal = 500;
    
    // Variable réseau : L'argent récolté
    public NetworkVariable<int> currentMoney = new NetworkVariable<int>(0);

    [Header("Effets")]
    public AudioSource moneySound; 

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        CarriableItem item = other.GetComponentInParent<CarriableItem>();

        if (item != null && item.enabled && !item.IsHeld() && !item.isScored.Value)
        {
            if (item.itemType == ItemType.Pipe) return;

            item.isScored.Value = true;

            int valueEarned = item.dollarValue;
            currentMoney.Value += valueEarned;

            Debug.Log($"<color=green>💰 OBJET DÉPOSÉ DANS LE CAMION ! +{valueEarned}$ | Total : {currentMoney.Value}$ / {moneyGoal}$</color>");

            PlayMoneySoundClientRpc();

        }
    }

    [ClientRpc]
    private void PlayMoneySoundClientRpc()
    {
        if (moneySound != null)
        {
            moneySound.Play();
        }
    }
}