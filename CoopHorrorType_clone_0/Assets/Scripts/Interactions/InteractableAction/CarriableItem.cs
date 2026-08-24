using Unity.Netcode;
using UnityEngine;

public enum ItemType 
{ 
    Generic,     
    Wrench,      
    Pipe,        
    StolenLoot   
}

public class CarriableItem : NetworkInteractable
{
    [Header("Configuration Objet")]
    public bool isHeavy = false;          
    public float floatSpeed = 10f;        
    public float heavySpeedPenalty = 0.5f;

    [Header("Sécurité & Limites (2 Joueurs)")]
    public float maxTwoPlayerDistance = 3.5f; 

    private NetworkVariable<ulong> holder1Id = new NetworkVariable<ulong>(ulong.MaxValue);
    private NetworkVariable<ulong> holder2Id = new NetworkVariable<ulong>(ulong.MaxValue);

    private Rigidbody rb;

    [Header("Type d'Objet")]
    public ItemType itemType = ItemType.Generic;

    [Header("Économie")]
    public int dollarValue = 50; 
    [HideInInspector] 
    public NetworkVariable<bool> isScored = new NetworkVariable<bool>(false);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override string GetInteractPrompt()
    {
        bool isHeldByMe = (holder1Id.Value == NetworkManager.Singleton.LocalClientId || 
                           holder2Id.Value == NetworkManager.Singleton.LocalClientId);

        if (isHeldByMe) return "[G] ou [Clic Droit] Lâcher l'objet";

        PlayerController localPlayer = GetLocalPlayerController();
        if (localPlayer != null && localPlayer.currentlyHeldItem != null)
        {
            return "Mains occupées !";
        }

        if (!isHeavy)
        {
            return holder1Id.Value == ulong.MaxValue ? "[E] Porter l'objet" : "";
        }
        else
        {
            if (holder1Id.Value == ulong.MaxValue) return "[E] Porter la chaudière (1/2 Joueurs)";
            if (holder2Id.Value == ulong.MaxValue) return "[E] Aider à porter (2/2 Joueurs)";
            return "Complet !";
        }
    }

    public bool IsHeld()
    {
        return holder1Id.Value != ulong.MaxValue || holder2Id.Value != ulong.MaxValue;
    }

    protected override void OnServerInteract(PlayerController player)
    {
        ulong playerId = player.OwnerClientId;

        if (holder1Id.Value == playerId || holder2Id.Value == playerId)
        {
            return;
        }

        if (player.currentlyHeldItem != null && player.currentlyHeldItem != this)
        {
            Debug.LogWarning($"[OBJET] Refus : Le joueur {playerId} a déjà les mains occupées !");
            return;
        }

        if (holder1Id.Value == ulong.MaxValue)
        {
            holder1Id.Value = playerId;
            player.currentlyHeldItem = this; 
            if (isHeavy) player.speedMultiplier.Value = heavySpeedPenalty; 
            rb.isKinematic = true;

            SetPlayerCollisionClientRpc(playerId, true);
            return;
        }

        if (isHeavy && holder2Id.Value == ulong.MaxValue)
        {
            holder2Id.Value = playerId;
            player.currentlyHeldItem = this; 
            player.speedMultiplier.Value = heavySpeedPenalty;

            SetPlayerCollisionClientRpc(playerId, true);
            return;
        }
    }

    public void DropRequestedByPlayer(PlayerController player)
    {
        if (player == null) return;

        if (holder1Id.Value == player.OwnerClientId)
        {
            DropPlayer(1, player);
        }
        else if (holder2Id.Value == player.OwnerClientId)
        {
            DropPlayer(2, player);
        }
    }

    private void DropPlayer(int slot, PlayerController player)
    {
        if (player != null)
        {
            SetPlayerCollisionClientRpc(player.OwnerClientId, false);

            player.speedMultiplier.Value = 1f; 
            player.currentlyHeldItem = null;
        }

        if (slot == 1) holder1Id.Value = ulong.MaxValue;
        if (slot == 2) holder2Id.Value = ulong.MaxValue;

        if (holder1Id.Value == ulong.MaxValue && holder2Id.Value == ulong.MaxValue)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
        }
    }

    [ClientRpc]
    private void SetPlayerCollisionClientRpc(ulong playerId, bool ignore)
    {
        PlayerController player = GetPlayerController(playerId);
        if (player != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            Collider[] itemColliders = GetComponentsInChildren<Collider>();

            if (playerCollider != null)
            {
                foreach (Collider col in itemColliders)
                {
                    Physics.IgnoreCollision(col, playerCollider, ignore);
                }
            }
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        Transform point1 = GetHoldPoint(holder1Id.Value);
        Transform point2 = GetHoldPoint(holder2Id.Value);

        if (holder1Id.Value != ulong.MaxValue && point1 == null)
        {
            DropPlayer(1, GetPlayerController(holder1Id.Value));
            return;
        }
        if (holder2Id.Value != ulong.MaxValue && point2 == null)
        {
            DropPlayer(2, GetPlayerController(holder2Id.Value));
            return;
        }

        if (point1 != null && point2 != null)
        {
            float currentDistance = Vector3.Distance(point1.position, point2.position);
            if (currentDistance > maxTwoPlayerDistance)
            {
                DropPlayer(1, GetPlayerController(holder1Id.Value));
                DropPlayer(2, GetPlayerController(holder2Id.Value));
                return;
            }
        }

        Vector3 targetPosition = transform.position;

        if (point1 != null && point2 == null)
        {
            targetPosition = point1.position;
            transform.rotation = Quaternion.Slerp(transform.rotation, point1.rotation, Time.deltaTime * floatSpeed);
        }
        else if (point1 != null && point2 != null)
        {
            targetPosition = (point1.position + point2.position) / 2f;
            Vector3 directionBetweenPlayers = point2.position - point1.position;
            if (directionBetweenPlayers != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionBetweenPlayers);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * floatSpeed);
            }
        }

        if (point1 != null)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * floatSpeed);
        }
    }

    private Transform GetHoldPoint(ulong clientId)
    {
        PlayerController player = GetPlayerController(clientId);
        return player != null ? player.holdPoint : null;
    }

    private PlayerController GetPlayerController(ulong clientId)
    {
        if (clientId == ulong.MaxValue) return null;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                return client.PlayerObject.GetComponent<PlayerController>();
            }
        }
        return null;
    }

    private PlayerController GetLocalPlayerController()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>();
        }
        return null;
    }
}