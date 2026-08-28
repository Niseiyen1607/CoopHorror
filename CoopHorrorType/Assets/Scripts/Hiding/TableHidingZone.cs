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
                }
            }
            else
            {
                if (player.isHiding.Value)
                {
                    player.isHiding.Value = false;
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
            }
        }
    }
}