using Unity.Netcode;
using UnityEngine;

public class HidingSpot : NetworkInteractable
{
    [Header("Positions Locker / Armoire")]
    public Transform cameraInsidePoint;
    public Transform exitPoint;         

    [Header("Audio SFX (Optionnel)")]
    public AudioClip enterSound; 
    public AudioClip exitSound;  

    private NetworkVariable<ulong> occupyingPlayerId = new NetworkVariable<ulong>(ulong.MaxValue);

    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (occupyingPlayerId.Value == clientId)
        {
            occupyingPlayerId.Value = ulong.MaxValue;
        }
    }

    public override string GetInteractPrompt()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (occupyingPlayerId.Value == localId)
        {
            return "[E] Sortir du locker";
        }

        if (occupyingPlayerId.Value == ulong.MaxValue)
        {
            return "[E] Se cacher dans le locker";
        }

        return "Locker occupé !";
    }

    protected override void OnServerInteract(PlayerController player)
    {
        ulong playerId = player.OwnerClientId;

        if (occupyingPlayerId.Value == ulong.MaxValue)
        {
            if (player.currentlyHeldItem != null)
            {
                player.currentlyHeldItem.DropRequestedByPlayer(player);
            }

            Debug.Log($"<color=cyan>[LOCKER] Le joueur {playerId} s'est caché dans le locker !</color>");

            occupyingPlayerId.Value = playerId;
            player.isHiding.Value = true;
            player.currentHidingSpot = this;

            Vector3 insidePos = cameraInsidePoint != null ? cameraInsidePoint.position : transform.position;
            Quaternion insideRot = cameraInsidePoint != null ? cameraInsidePoint.rotation : transform.rotation;
            
            player.TeleportClientRpc(insidePos, insideRot);
            SetPlayerVisibilityClientRpc(playerId, false);

            PlayHideSoundClientRpc(true);
        }
        else if (occupyingPlayerId.Value == playerId)
        {
            ExitHidingSpot(player);
        }
    }

    public void ExitHidingSpot(PlayerController player)
    {
        if (player == null) return;
        ulong playerId = player.OwnerClientId;

        if (occupyingPlayerId.Value == playerId || player.isHiding.Value)
        {
            Debug.Log($"[LOCKER] Le joueur {playerId} sort du locker.");

            occupyingPlayerId.Value = ulong.MaxValue;
            player.isHiding.Value = false;
            player.currentHidingSpot = null;

            Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position + transform.forward * 1.5f;
            
            player.TeleportClientRpc(exitPos, player.transform.rotation);
            SetPlayerVisibilityClientRpc(playerId, true);

            PlayHideSoundClientRpc(false);
        }
    }

    [ClientRpc]
    private void PlayHideSoundClientRpc(bool isEntering)
    {
        if (AudioManager.Instance != null)
        {
            AudioClip clipToPlay = isEntering ? enterSound : exitSound;
            if (clipToPlay != null) AudioManager.Instance.PlaySound3D(clipToPlay, transform.position, 0.8f);
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
                    foreach (MeshRenderer r in renderers) r.enabled = isVisible;

                    SkinnedMeshRenderer[] skinnedRenderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer r in skinnedRenderers) r.enabled = isVisible;
                    break;
                }
            }
        }
    }
}