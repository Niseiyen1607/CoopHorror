using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public abstract class NetworkInteractable : NetworkBehaviour, IInteractable
{
    [SerializeField] protected string interactPrompt = "[E] Interagir";

    public virtual string GetInteractPrompt()
    {
        return interactPrompt;
    }

    public void Interact(PlayerController player)
    {
        InteractServerRpc(player.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    protected void InteractServerRpc(ulong playerNetworkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out var playerObject))
        {
            PlayerController player = playerObject.GetComponent<PlayerController>();
            if (player != null)
            {
                OnServerInteract(player);
            }
        }
    }

    protected abstract void OnServerInteract(PlayerController player);
}