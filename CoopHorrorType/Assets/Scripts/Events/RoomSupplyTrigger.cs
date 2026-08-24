using Unity.Netcode;
using UnityEngine;

public class RoomSupplyTrigger : NetworkBehaviour
{
    [Header("Points d'Apparition")]
    public Transform ceilingDropPoint; 
    public GameObject[] itemsToDrop;  

    [Header("Audio (Optionnel)")]
    public AudioSource dropSound;      

    private bool hasTriggered = false; 

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || hasTriggered) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            hasTriggered = true; 
            SpawnSuppliesFromCeiling();
        }
    }

    private void SpawnSuppliesFromCeiling()
    {
        if (dropSound != null)
        {
            PlayDropSoundClientRpc();
        }

        Vector3 basePos = ceilingDropPoint != null ? ceilingDropPoint.position : transform.position + Vector3.up * 3f;

        for (int i = 0; i < itemsToDrop.Length; i++)
        {
            GameObject prefab = itemsToDrop[i];
            if (prefab == null) continue;

            Vector3 randomOffset = new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(0f, 0.5f),
                Random.Range(-0.4f, 0.4f)
            );

            GameObject spawnedItem = Instantiate(prefab, basePos + randomOffset, Quaternion.identity);

            spawnedItem.GetComponent<NetworkObject>().Spawn();

            if (spawnedItem.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false; 
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse); 
            }
        }
    }

    [ClientRpc]
    private void PlayDropSoundClientRpc()
    {
        if (dropSound != null)
        {
            dropSound.Play();
        }
    }
}