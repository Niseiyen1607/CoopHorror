using Unity.Netcode;
using UnityEngine;

public class DoorButton : NetworkInteractable
{
    [Header("Porte Cible")]
    public MechanicalDoor targetDoor;

    [Header("Changement de Texture / Matériau")]
    public MeshRenderer buttonMeshRenderer;
    public Material closedMaterial;          
    public Material openMaterial;            

    public override void OnNetworkSpawn()
    {
        if (targetDoor != null)
        {
            targetDoor.isOpen.OnValueChanged += OnDoorStateChanged;
            
            UpdateSquareMaterial(targetDoor.isOpen.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (targetDoor != null)
        {
            targetDoor.isOpen.OnValueChanged -= OnDoorStateChanged;
        }
    }

    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        UpdateSquareMaterial(newValue);
    }

    private void UpdateSquareMaterial(bool isOpen)
    {
        if (buttonMeshRenderer == null) return;

        if (isOpen && openMaterial != null)
        {
            buttonMeshRenderer.material = openMaterial; 
        }
        else if (!isOpen && closedMaterial != null)
        {
            buttonMeshRenderer.material = closedMaterial; 
        }
    }

    public override string GetInteractPrompt()
    {
        if (targetDoor != null && targetDoor.isOpen.Value)
        {
            return "[E] Fermer la porte";
        }
        return "[E] Appuyer sur le bouton (Ouvrir)";
    }

    protected override void OnServerInteract(PlayerController player)
    {
        if (targetDoor != null)
        {
            targetDoor.ToggleDoor();
        }
    }
}