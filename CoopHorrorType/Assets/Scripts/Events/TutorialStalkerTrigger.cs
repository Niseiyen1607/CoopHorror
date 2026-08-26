using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TutorialStalkerTrigger : NetworkBehaviour
{
    [Header("Monstre & Emplacement")]
    public GameObject stalkerPrefab;         
    public Transform sideCorridorSpawnPoint;  

    [Header("Ambiance Horreur (Optionnel)")]
    public Light[] corridorLights;            

    private bool hasTriggered = false;        

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || hasTriggered) return; 

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            hasTriggered = true;
            StartCoroutine(StalkerAmbushSequence());
        }
    }

    private IEnumerator StalkerAmbushSequence()
    {
        Debug.Log("<color=red>[TUTO STALKER] Déclenchement de l'embuscade dans le couloir !</color>");

        if (corridorLights != null && corridorLights.Length > 0)
        {
            for (int i = 0; i < 6; i++)
            {
                bool lightState = (i % 2 == 0);
                foreach (Light l in corridorLights)
                {
                    if (l != null) l.enabled = lightState;
                }
                yield return new WaitForSeconds(0.12f); 
            }

            foreach (Light l in corridorLights)
            {
                if (l != null) l.enabled = true;
            }
        }

        if (stalkerPrefab != null && sideCorridorSpawnPoint != null)
        {
            GameObject stalkerObj = Instantiate(stalkerPrefab, sideCorridorSpawnPoint.position, sideCorridorSpawnPoint.rotation);
            
            stalkerObj.GetComponent<NetworkObject>().Spawn();

            Debug.Log($"<color=red>👹 Stalker apparu dans le petit couloir latéral ! ({sideCorridorSpawnPoint.position})</color>");
        }
    }
}