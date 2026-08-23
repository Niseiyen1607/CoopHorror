using UnityEngine;
using Unity.Netcode;

public class StealableLoot : NetworkInteractable
{
    public int itemValue = 100; 

    protected override void OnServerInteract(PlayerController player)
    {
        Debug.Log($"Le joueur {player.OwnerClientId} a volé un objet d'une valeur de {itemValue}$ !");

        GetComponent<NetworkObject>().Despawn();
    }
}