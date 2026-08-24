using Unity.Netcode;
using UnityEngine;

public enum PipeRotationAxis { X_Axis, Y_Axis, Z_Axis }

public class PipeSocket : NetworkInteractable
{
    [Header("Réseau & Circuit")]
    public PipeNetworkManager circuitManager; 

    [Header("Réglages Casse-Tête")]
    public PipeRotationAxis rotationAxis = PipeRotationAxis.Z_Axis;
    public int targetRotationStep = 2; // (0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°)
    
    private NetworkVariable<bool> isInstalled = new NetworkVariable<bool>(false);
    private NetworkVariable<int> currentRotationStep = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isFixedCorrectly = new NetworkVariable<bool>(false);

    private GameObject installedPipeObject;

    public bool IsFixedCorrectly() => isFixedCorrectly.Value;

    public override string GetInteractPrompt()
    {
        PlayerController localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        if (!isInstalled.Value)
        {
            return "Approcher un Tuyau Neuf pour l'encastrer !";
        }

        bool hasWrench = localPlayer != null && 
                         localPlayer.currentlyHeldItem != null && 
                         localPlayer.currentlyHeldItem.itemType == ItemType.Wrench;

        if (hasWrench)
        {
            return "[E] Retirer le tuyau du mur (Clé à molette)";
        }
        else if (!isFixedCorrectly.Value)
        {
            return "[E] Tourner le tuyau à 90°";
        }

        return "Tuyau connecté et aligné !";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (isInstalled.Value) return;

        CarriableItem pipeItem = other.GetComponentInParent<CarriableItem>();

        if (pipeItem != null && pipeItem.itemType == ItemType.Pipe && pipeItem.enabled)
        {
            PlayerController holdingPlayer = GetPlayerHoldingItem(pipeItem);

            if (holdingPlayer != null)
            {
                Debug.Log("<color=cyan>★ SNAP AUTOMATIQUE ! Le tuyau s'encastre dans le mur ! ★</color>");

                pipeItem.DropRequestedByPlayer(holdingPlayer);

                pipeItem.transform.SetParent(transform);
                pipeItem.transform.localPosition = Vector3.zero;
                
                pipeItem.transform.localRotation = Quaternion.Euler(pipeItem.customSnapRotation);

                pipeItem.GetComponent<Rigidbody>().isKinematic = true;
                pipeItem.enabled = false;

                installedPipeObject = pipeItem.gameObject;
                isInstalled.Value = true;

                CheckRotation();
            }
        }
    }

    protected override void OnServerInteract(PlayerController player)
    {
        if (!isInstalled.Value) return;

        bool hasWrench = player.currentlyHeldItem != null && 
                         player.currentlyHeldItem.itemType == ItemType.Wrench;

        if (hasWrench)
        {
            Debug.Log("<color=orange>[SOCKET] Tuyau démonté du mur avec la Clé à molette !</color>");
            RemoveInstalledPipe();
        }
        else
        {
            currentRotationStep.Value = (currentRotationStep.Value + 1) % 4; 

            Vector3 rotAxis = Vector3.forward; 
            if (rotationAxis == PipeRotationAxis.X_Axis) rotAxis = Vector3.right;
            if (rotationAxis == PipeRotationAxis.Y_Axis) rotAxis = Vector3.up;

            transform.Rotate(rotAxis * 90f, Space.Self);

            Debug.Log($"[SOCKET] Tuyau tourné à 90° ! Étape actuelle : {currentRotationStep.Value} / Cible : {targetRotationStep}");
            CheckRotation();
        }
    }

    private void RemoveInstalledPipe()
    {
        if (installedPipeObject != null)
        {
            installedPipeObject.transform.SetParent(null);

            if (installedPipeObject.TryGetComponent<CarriableItem>(out var pipeItem))
            {
                pipeItem.enabled = true;
            }

            if (installedPipeObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false;
                Vector3 popForce = transform.forward * 1.5f + Vector3.up * 0.5f; 
                rb.AddForce(popForce, ForceMode.Impulse);
            }

            installedPipeObject = null;
        }

        isInstalled.Value = false;
        isFixedCorrectly.Value = false;

        if (circuitManager != null)
        {
            circuitManager.CheckCircuitCompletion();
        }
    }

    private void CheckRotation()
    {
        if (currentRotationStep.Value == targetRotationStep)
        {
            isFixedCorrectly.Value = true;
            Debug.Log("<color=yellow>✔ Tuyau aligné correctement !</color>");
        }
        else
        {
            isFixedCorrectly.Value = false;
        }

        if (circuitManager != null)
        {
            circuitManager.CheckCircuitCompletion();
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