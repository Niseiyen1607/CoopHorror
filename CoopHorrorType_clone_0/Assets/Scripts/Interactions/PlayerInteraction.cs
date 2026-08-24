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
    private IInteractable lastInteractable;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!IsOwner) return;

        CheckForInteractable();
        HandleInput();
        UpdateInteractionUI();
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

        if (currentInteractable != lastInteractable)
        {
            SetOutlineEnabled(lastInteractable, false);

            SetOutlineEnabled(currentInteractable, true);

            lastInteractable = currentInteractable;
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

    private void UpdateInteractionUI()
    {
        if (InteractionUI.Instance == null) return;

        if (currentInteractable != null)
        {
            string rawPrompt = currentInteractable.GetInteractPrompt();
            
            if (rawPrompt.StartsWith("[E]"))
            {
                InteractionUI.Instance.ShowPrompt("[E]", rawPrompt.Replace("[E]", "").Trim());
            }
            else if (rawPrompt.StartsWith("[G]"))
            {
                InteractionUI.Instance.ShowPrompt("[G]", rawPrompt.Replace("[G]", "").Trim());
            }
            else
            {
                InteractionUI.Instance.ShowPrompt("", rawPrompt);
            }
        }
        else if (playerController.currentlyHeldItem != null)
        {
            InteractionUI.Instance.ShowPrompt("[G]", "Lâcher l'objet");
        }
        else
        {
            InteractionUI.Instance.HidePrompt();
        }
    }

    private void SetOutlineEnabled(IInteractable interactable, bool state)
    {
        if (interactable == null) return;

        MonoBehaviour mb = interactable as MonoBehaviour;
        if (mb != null)
        {
            var outline = mb.GetComponentInChildren<Outline>();
            if (outline != null)
            {
                outline.enabled = state;
            }
        }
    }

    private void OnDisable()
    {
        if (IsOwner && InteractionUI.Instance != null)
        {
            InteractionUI.Instance.HidePrompt();
        }
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