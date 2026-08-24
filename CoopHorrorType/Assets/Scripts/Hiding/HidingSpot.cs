using Unity.Netcode;
using UnityEngine;

public class HidingSpot : NetworkInteractable
{
    [Header("Positions Armoire")]
    public Transform cameraInsidePoint;
    public Transform exitPoint;         

    private NetworkVariable<ulong> occupyingPlayerId = new NetworkVariable<ulong>(ulong.MaxValue);

    public override string GetInteractPrompt()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (occupyingPlayerId.Value == localId)
        {
            return "[E] Sortir de l'armoire";
        }

        if (occupyingPlayerId.Value == ulong.MaxValue)
        {
            return "[E] Se cacher dans l'armoire";
        }

        return "Armoire occupée !";
    }

    protected override void OnServerInteract(PlayerController player)
    {
        ulong playerId = player.OwnerClientId;

        if (occupyingPlayerId.Value == playerId)
        {
            ExitHidingSpot(player);
            return;
        }

        if (occupyingPlayerId.Value == ulong.MaxValue)
        {
            if (player.currentlyHeldItem != null)
            {
                player.currentlyHeldItem.DropRequestedByPlayer(player);
            }

            Debug.Log($"<color=cyan>[CACHE-CACHE] Le joueur {playerId} s'est caché dans l'armoire !</color>");

            occupyingPlayerId.Value = playerId;
            player.isHiding.Value = true;
            player.currentHidingSpot = this;

            Vector3 insidePos = cameraInsidePoint != null ? cameraInsidePoint.position : transform.position;
            Quaternion insideRot = cameraInsidePoint != null ? cameraInsidePoint.rotation : transform.rotation;
            
            player.TeleportClientRpc(insidePos, insideRot);

            SetPlayerVisibilityClientRpc(playerId, false);
        }
    }

    public void ExitHidingSpot(PlayerController player)
    {
        ulong playerId = player.OwnerClientId;

        if (occupyingPlayerId.Value == playerId)
        {
            Debug.Log($"[CACHE-CACHE] Le joueur {playerId} sort de l'armoire.");

            occupyingPlayerId.Value = ulong.MaxValue;
            player.isHiding.Value = false;
            player.currentHidingSpot = null;

            Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position + transform.forward * 1.5f;
            
            player.TeleportClientRpc(exitPos, player.transform.rotation);

            SetPlayerVisibilityClientRpc(playerId, true);
        }
    }

    [ClientRpc]
    private void SetPlayerVisibilityClientRpc(ulong playerId, bool isVisible)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                if (obj.OwnerClientId == playerId && obj.TryGetComponent<PlayerController>(out var player))
                {
                    MeshRenderer[] renderers = player.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer r in renderers)
                    {
                        r.enabled = isVisible;
                    }

                    SkinnedMeshRenderer[] skinnedRenderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer r in skinnedRenderers)
                    {
                        r.enabled = isVisible;
                    }
                    break;
                }
            }
        }
    }
}