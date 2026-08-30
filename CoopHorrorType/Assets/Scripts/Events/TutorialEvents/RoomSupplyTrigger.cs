using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoomSupplyTrigger : NetworkBehaviour
{
    public Transform[] dropPoints;      
    public GameObject[] itemsToDrop;    

    public float delayBetweenDrops = 0.4f; 

    public AudioSource dropSound;       

    private bool hasTriggered = false;  

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || hasTriggered) return; 

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            hasTriggered = true; 
            StartCoroutine(SpawnSuppliesRoutine());
        }
    }

    private IEnumerator SpawnSuppliesRoutine()
    {
        int currentDropPointIndex = 0;

        for (int i = 0; i < itemsToDrop.Length; i++)
        {
            GameObject prefab = itemsToDrop[i];
            if (prefab != null)
            {
                Transform targetDropPoint = transform;
                if (dropPoints != null && dropPoints.Length > 0)
                {
                    targetDropPoint = dropPoints[currentDropPointIndex % dropPoints.Length];
                    currentDropPointIndex++; 
                }

                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    0f,
                    Random.Range(-0.2f, 0.2f)
                );

                Vector3 spawnPos = targetDropPoint.position + randomOffset;

                GameObject spawnedItem = Instantiate(prefab, spawnPos, Quaternion.identity);
                spawnedItem.GetComponent<NetworkObject>().Spawn();

                if (spawnedItem.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);
                }

                if (dropSound != null)
                {
                    PlayDropSoundClientRpc();
                }

                yield return new WaitForSeconds(delayBetweenDrops);
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