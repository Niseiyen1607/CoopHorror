using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class StalkerSpawner : NetworkBehaviour
{
    [Header("Prefab du Stalker")]
    public GameObject stalkerPrefab;

    [Header("Réglages de Fréquence")]
    public float minSpawnInterval = 35f;
    public float maxSpawnInterval = 70f;
    public float minDistanceFromPlayers = 15f;

    [Header("Points de Spawns Spécifiques (Optionnel)")]
    [Tooltip("Laissez vide pour utiliser les objets avec le tag 'MonsterSpawn'")]
    public Transform[] customSpawnPoints;

    private GameObject currentSpawnedStalker;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(SpawnLoopRoutine());
    }

    private IEnumerator SpawnLoopRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            if (currentSpawnedStalker == null)
            {
                TrySpawnStalkerLogically();
            }
        }
    }

    private void TrySpawnStalkerLogically()
    {
        List<Vector3> potentialPositions = new List<Vector3>();

        if (customSpawnPoints != null && customSpawnPoints.Length > 0)
        {
            foreach (var t in customSpawnPoints) potentialPositions.Add(t.position);
        }
        else
        {
            GameObject[] taggedSpawns = GameObject.FindGameObjectsWithTag("MonsterSpawn");
            foreach (var go in taggedSpawns) potentialPositions.Add(go.transform.position);
        }

        ShuffleList(potentialPositions);

        foreach (Vector3 spawnPos in potentialPositions)
        {
            if (IsPositionSafeAndHidden(spawnPos))
            {
                SpawnMonsterAt(spawnPos);
                return;
            }
        }

        PlayerController[] players = FindObjectsOfType<PlayerController>();
        if (players.Length > 0)
        {
            for (int i = 0; i < 15; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * 30f;
                randomOffset.y = 0;
                Vector3 testPos = players[0].transform.position + randomOffset;

                if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                {
                    if (IsPositionSafeAndHidden(hit.position))
                    {
                        SpawnMonsterAt(hit.position);
                        return;
                    }
                }
            }
        }
    }

    private bool IsPositionSafeAndHidden(Vector3 position)
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();

        foreach (var player in players)
        {
            float distance = Vector3.Distance(player.transform.position, position);
            if (distance < minDistanceFromPlayers) return false; // Trop proche d'un joueur

            Transform camTransform = player.GetComponentInChildren<Camera>()?.transform;
            if (camTransform == null) continue;

            Vector3 dir = (position + Vector3.up * 1.0f) - camTransform.position;
            float angle = Vector3.Angle(camTransform.forward, dir.normalized);

            if (angle < 75f) 
            {
                if (Physics.Raycast(camTransform.position, dir.normalized, out RaycastHit hit, distance))
                {
                    if (hit.distance >= distance - 0.5f)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private void SpawnMonsterAt(Vector3 position)
    {
        currentSpawnedStalker = Instantiate(stalkerPrefab, position, Quaternion.identity);
        currentSpawnedStalker.GetComponent<NetworkObject>().Spawn();
        Debug.Log($"<color=green>[SPAWNER] Stalker infiltré discrètement à la position {position} !</color>");
    }

    private void ShuffleList(List<Vector3> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Vector3 temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}