using Unity.Netcode;
using UnityEngine;

public class TableHidingZone : NetworkBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            if (player.isCrouching.Value)
            {
                if (!player.isHiding.Value)
                {
                    player.isHiding.Value = true;
                    Debug.Log($"<color=green>[TABLE] Le joueur {player.OwnerClientId} est caché sous la table !</color>");
                }
            }
            else
            {
                if (player.isHiding.Value)
                {
                    player.isHiding.Value = false;
                    Debug.Log($"<color=orange>[TABLE] Le joueur {player.OwnerClientId} s'est relevé : Il n'est plus caché !</color>");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            if (player.isHiding.Value)
            {
                player.isHiding.Value = false;
                Debug.Log($"[TABLE] Le joueur {player.OwnerClientId} est sorti de dessous la table.");
            }
        }
    }
}