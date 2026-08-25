using Unity.Netcode;
using UnityEngine;

public enum PipeRotationAxis { X_Axis, Y_Axis, Z_Axis }

[RequireComponent(typeof(Rigidbody))]
public class PipeSocket : NetworkInteractable
{
    [Header("Réseau & Circuit")]
    public PipeNetworkManager circuitManager; 

    [Header("Indicateur Visuel (Ghost)")]
    public GameObject ghostPipeIndicator; 

    [Header("Réglages Casse-Tête")]
    public PipeRotationAxis rotationAxis = PipeRotationAxis.Z_Axis;
    public bool isCrossSocket = false; 
    public int targetRotationStep = 2; 
    
    private NetworkVariable<bool> isInstalled = new NetworkVariable<bool>(false);
    private NetworkVariable<int> currentRotationStep = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isFixedCorrectly = new NetworkVariable<bool>(false);

    private GameObject installedPipeObject;
    private float snapCooldownTimer = 0f; 

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public bool IsFixedCorrectly() => isFixedCorrectly.Value;

    public override string GetInteractPrompt()
    {
        PlayerController localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();

        if (!isInstalled.Value)
        {
            return "Lancer ou approcher un Tuyau Neuf pour l'encastrer !";
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

    public override void OnNetworkSpawn()
    {
        isInstalled.OnValueChanged += OnInstalledStateChanged;
        UpdateGhostVisual();
    }

    private void OnEnable()
    {
        UpdateGhostVisual();
    }

    private void Update()
    {
        if (snapCooldownTimer > 0f)
        {
            snapCooldownTimer -= Time.deltaTime;
        }
    }

    private void OnInstalledStateChanged(bool previousValue, bool newValue)
    {
        UpdateGhostVisual();
    }

    private void UpdateGhostVisual()
    {
        if (ghostPipeIndicator != null)
        {
            ghostPipeIndicator.SetActive(!isInstalled.Value);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isInstalled.Value) return;
        if (snapCooldownTimer > 0f) return;

        CarriableItem pipeItem = other.GetComponent<CarriableItem>();
        if (pipeItem == null)
        {
            pipeItem = other.GetComponentInParent<CarriableItem>();
        }

        if (pipeItem != null && pipeItem.itemType == ItemType.Pipe)
        {
            RequestSnapServerRpc(pipeItem.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSnapServerRpc(ulong pipeNetworkObjectId)
    {
        if (isInstalled.Value) return;
        if (snapCooldownTimer > 0f) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pipeNetworkObjectId, out var netObj))
        {
            CarriableItem pipeItem = netObj.GetComponent<CarriableItem>();

            if (pipeItem != null && pipeItem.itemType == ItemType.Pipe && !isInstalled.Value)
            {
                PlayerController holdingPlayer = GetPlayerHoldingItem(pipeItem);

                if (holdingPlayer != null)
                {
                    pipeItem.DropRequestedByPlayer(holdingPlayer);
                    holdingPlayer.currentlyHeldItemRef.Value = default; 
                }

                ClearHoldersReferences(pipeItem);

                pipeItem.ForceClearHolders();

                if (pipeItem.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }

                pipeItem.transform.SetParent(transform);
                pipeItem.transform.localPosition = Vector3.zero;
                pipeItem.transform.localRotation = Quaternion.Euler(pipeItem.customSnapRotation);

                pipeItem.enabled = false;

                installedPipeObject = pipeItem.gameObject;
                isInstalled.Value = true;

                CheckRotation();
            }
        }
    }

    private void ClearHoldersReferences(CarriableItem pipeItem)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                PlayerController pc = client.PlayerObject.GetComponent<PlayerController>();
                if (pc != null && pc.currentlyHeldItem == pipeItem)
                {
                    pc.currentlyHeldItemRef.Value = default;
                }
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
            RemoveInstalledPipe();
        }
        else
        {
            currentRotationStep.Value = (currentRotationStep.Value + 1) % 4; 

            Vector3 rotAxis = Vector3.forward; 
            if (rotationAxis == PipeRotationAxis.X_Axis) rotAxis = Vector3.right;
            if (rotationAxis == PipeRotationAxis.Y_Axis) rotAxis = Vector3.up;

            transform.Rotate(rotAxis * 90f, Space.Self);

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
                // CORRECTION : Effacer la mémoire du tuyau avant de le ré-activer
                pipeItem.ForceClearHolders();
                pipeItem.enabled = true;
            }

            if (installedPipeObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false;
                Vector3 popForce = transform.forward * 2.0f + Vector3.up * 0.5f; 
                rb.AddForce(popForce, ForceMode.Impulse);
            }

            installedPipeObject = null;
        }

        isInstalled.Value = false;
        isFixedCorrectly.Value = false;

        snapCooldownTimer = 0.5f;

        if (circuitManager != null)
        {
            circuitManager.CheckCircuitCompletion();
        }
    }

    private void CheckRotation()
    {
        if (isCrossSocket || currentRotationStep.Value == targetRotationStep)
        {
            isFixedCorrectly.Value = true;
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