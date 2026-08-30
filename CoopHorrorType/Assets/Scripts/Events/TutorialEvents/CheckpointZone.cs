using Unity.Netcode;
using UnityEngine;

public class CheckpointZone : NetworkBehaviour
{
    [Header("Position de Spawn de Réapparition")]
    public Transform checkpointSpawnPoint; 

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) player = other.GetComponent<PlayerController>();

        if (player != null && !TutorialProgress.hasReachedCheckpoint)
        {
            TutorialProgress.hasReachedCheckpoint = true;
            TutorialProgress.hasSeenIntro = true; 

            Vector3 spawnPos = checkpointSpawnPoint != null ? checkpointSpawnPoint.position : transform.position;
            TutorialProgress.checkpointPosition = spawnPos;

            Debug.Log($"<color=cyan>[CHECKPOINT] Checkpoint activé à la position {spawnPos} !</color>");
        }
    }
}