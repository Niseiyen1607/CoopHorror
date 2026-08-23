using Unity.Netcode;
using UnityEngine;

public class PipeSocket : NetworkInteractable
{
    [Header("Réglages Casse-Tête")]
    public int targetRotationStep = 2; 
    
    private NetworkVariable<bool> isInstalled = new NetworkVariable<bool>(false);
    private NetworkVariable<int> currentRotationStep = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isFixedCorrectly = new NetworkVariable<bool>(false);

    private GameObject installedPipeObject;

    public override string GetInteractPrompt()
    {
        if (!isInstalled.Value)
        {
            return "Approcher un Tuyau Neuf pour l'encastrer !";
        }

        if (!isFixedCorrectly.Value)
        {
            return "[E] Tourner le tuyau à 90°";
        }

        return "Tuyau connecté et réparé !";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; 
        if (isInstalled.Value) return; 

        CarriableItem pipeItem = other.GetComponentInParent<CarriableItem>();

        if (pipeItem != null && pipeItem.itemType == ItemType.Pipe)
        {
            PlayerController holdingPlayer = GetPlayerHoldingItem(pipeItem);

            if (holdingPlayer != null)
            {
                Debug.Log("<color=cyan>★ SNAP AUTOMATIQUE ! Le tuyau s'encastre tout seul dans le mur ! ★</color>");

                pipeItem.DropRequestedByPlayer(holdingPlayer);

                pipeItem.transform.position = transform.position;
                pipeItem.transform.rotation = transform.rotation;
                pipeItem.transform.SetParent(transform);
                pipeItem.GetComponent<Rigidbody>().isKinematic = true;

                installedPipeObject = pipeItem.gameObject;
                isInstalled.Value = true;

                CheckRotation();
            }
        }
    }

    protected override void OnServerInteract(PlayerController player)
    {
        if (isInstalled.Value && !isFixedCorrectly.Value)
        {
            currentRotationStep.Value = (currentRotationStep.Value + 1) % 4; 
            transform.Rotate(0, 90, 0); 
            Debug.Log($"[SOCKET] Tuyau tourné à 90° ! Étape actuelle : {currentRotationStep.Value} / Cible : {targetRotationStep}");
            CheckRotation();
        }
    }

    private void CheckRotation()
    {
        if (currentRotationStep.Value == targetRotationStep)
        {
            isFixedCorrectly.Value = true;
            Debug.Log("<color=yellow>★ TUYAU PARFAITEMENT CONNECTÉ ET RÉPARÉ ! ★</color>");
        }
        else
        {
            isFixedCorrectly.Value = false;
        }
    }

    private PlayerController GetPlayerHoldingItem(CarriableItem item)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                PlayerController player = client.PlayerObject.GetComponent<PlayerController>();
                if (player != null && player.currentlyHeldItem == item)
                {
                    return player;
                }
            }
        }
        return null;
    }
}