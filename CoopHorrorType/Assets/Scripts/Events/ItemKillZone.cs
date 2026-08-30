using Unity.Netcode;
using UnityEngine;

public class ItemKillZone : NetworkBehaviour
{
    public Transform safeRespawnPoint; 

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; 

        CarriableItem item = other.GetComponentInParent<CarriableItem>();

        if (item != null)
        {
            if (item.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Vector3 targetPos = safeRespawnPoint != null ? safeRespawnPoint.position : new Vector3(0f, 1f, 0f);
            
            targetPos += new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, Random.Range(-0.3f, 0.3f));

            item.transform.position = targetPos;
        }
    }
}