using Unity.Netcode;
using UnityEngine;

public class DefectivePipe : NetworkInteractable
{
    [Header("Configuration")]
    public GameObject pipeSocket;        
    public GameObject brokenPipePrefab;  

    public override string GetInteractPrompt()
    {
        PlayerController localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        if (localPlayer != null && localPlayer.currentlyHeldItem != null && localPlayer.currentlyHeldItem.itemType == ItemType.Wrench)
        {
            return "[E] Dévisser le tuyau (Clé à molette)";
        }

        return "Nécessite une Clé à molette !";
    }

    protected override void OnServerInteract(PlayerController player)
    {
        if (player.currentlyHeldItem != null && player.currentlyHeldItem.itemType == ItemType.Wrench)
        {
            Debug.Log("<color=green>[TUYAU] Tuyau défectueux dévissé ! Il tombe au sol !</color>");

            if (brokenPipePrefab != null)
            {
                GameObject brokenPipe = Instantiate(brokenPipePrefab, transform.position, transform.rotation);

                brokenPipe.GetComponent<NetworkObject>().Spawn();

                if (brokenPipe.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    Vector3 dropImpulse = transform.forward * 1.5f + Vector3.down * 0.5f; 
                    rb.AddForce(dropImpulse, ForceMode.Impulse);
                }
            }

            if (pipeSocket != null)
            {
                pipeSocket.SetActive(true);
            }

            GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            Debug.LogWarning("[TUYAU] Impossible de dévisser : Vous devez tenir la Clé à molette dans vos mains !");
        }
    }
}