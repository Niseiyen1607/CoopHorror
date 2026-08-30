using Unity.Netcode;
using UnityEngine;

public class StealableLoot : NetworkInteractable
{
    public int itemValue = 100; 

    public GameObject particlePickupPrefab; 
    public GameObject floatingTextPrefab;   

    public override string GetInteractPrompt()
    {
        return $"[E] Voler l'objet ({itemValue}$)";
    }

    protected override void OnServerInteract(PlayerController player)
    {
        TriggerPickupJuiceClientRpc(transform.position, itemValue);

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(itemValue);
        }

        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void TriggerPickupJuiceClientRpc(Vector3 spawnPos, int amount)
    {
        if (particlePickupPrefab != null)
        {
            GameObject particles = Instantiate(particlePickupPrefab, spawnPos, Quaternion.identity);
            Destroy(particles, 2f);
        }

        if (floatingTextPrefab != null)
        {
            GameObject floatObj = Instantiate(floatingTextPrefab, spawnPos + Vector3.up * 0.5f, Quaternion.identity);
            FloatingText ft = floatObj.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.Setup($"+{amount}$", Color.yellow);
            }
        }
    }
}