using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class StalkerSpawner : NetworkBehaviour
{
    public GameObject stalkerPrefab;

    public float minSpawnInterval = 15f; 
    public float maxSpawnInterval = 30f;
    public float minDistanceFromPlayers = 10f;

    public Transform[] customSpawnPoints;

    private GameObject currentSpawnedStalker;
    private bool isSpawningActive = false;

    public void StartSpawningLoop()
    {
        if (!IsServer || isSpawningActive) return;
        isSpawningActive = true;
        StartCoroutine(SpawnLoopRoutine());
    }

    public void StopSpawningLoop()
    {
        isSpawningActive = false;
        StopAllCoroutines();

        if (currentSpawnedStalker != null)
        {
            if (currentSpawnedStalker.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            currentSpawnedStalker = null;
        }
    }

    public void RegisterCurrentStalker(GameObject stalkerObj)
    {
        currentSpawnedStalker = stalkerObj;
    }

    private IEnumerator SpawnLoopRoutine()
    {
        while (isSpawningActive)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            if (isSpawningActive && currentSpawnedStalker == null && FindObjectOfType<StalkerAI>() == null)
            {
                TrySpawnStalkerLogically();
            }
        }
    }

    private void TrySpawnStalkerLogically()
    {
        if (FindObjectOfType<StalkerAI>() != null) return;

        List<Vector3> potentialPositions = new List<Vector3>();

        if (customSpawnPoints != null && customSpawnPoints.Length > 0)
        {
            foreach (var t in customSpawnPoints) if (t != null) potentialPositions.Add(t.position);
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
                Vector3 randomOffset = Random.insideUnitSphere * 20f;
                randomOffset.y = 0;
                Vector3 testPos = players[Random.Range(0, players.Length)].transform.position + randomOffset;

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
            if (distance < minDistanceFromPlayers) return false;

            Transform camTransform = player.GetComponentInChildren<Camera>()?.transform;
            if (camTransform == null) continue;

            Vector3 dir = (position + Vector3.up * 1.0f) - camTransform.position;
            float angle = Vector3.Angle(camTransform.forward, dir.normalized);

            if (angle < 70f) 
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
        if (FindObjectOfType<StalkerAI>() != null) return; 

        currentSpawnedStalker = Instantiate(stalkerPrefab, position, Quaternion.identity);
        currentSpawnedStalker.GetComponent<NetworkObject>().Spawn();
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