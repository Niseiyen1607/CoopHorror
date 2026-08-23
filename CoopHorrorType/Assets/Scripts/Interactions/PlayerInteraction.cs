using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Réglages")]
    public float interactionDistance = 3f;
    public LayerMask interactableLayer = ~0; 

    [Header("Références")]
    public Transform cameraTransform;
    private PlayerController playerController;

    private IInteractable currentInteractable;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!IsOwner) return;

        CheckForInteractable();
        HandleInput();
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactableLayer, QueryTriggerInteraction.Collide);
        
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        currentInteractable = null;

        foreach (var hit in hits)
        {
            if (playerController.currentlyHeldItem != null && 
                hit.collider.gameObject == playerController.currentlyHeldItem.gameObject)
            {
                continue;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            
            if (interactable != null && ((MonoBehaviour)interactable).enabled)
            {
                currentInteractable = interactable;
                break;
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact(playerController);
            }
        }
    }

    public string GetCurrentPrompt()
    {
        if (currentInteractable != null)
        {
            return currentInteractable.GetInteractPrompt();
        }
        if (playerController.currentlyHeldItem != null)
        {
            return "[G] ou [Clic Droit] Lâcher l'objet";
        }
        return "";
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * interactionDistance);
        }
    }
}