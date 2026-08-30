using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class LobbyNetworkManager : NetworkBehaviour
{
    public static LobbyNetworkManager Instance { get; private set; }

    public string gameSceneName = "TutorialScene";

    private static Dictionary<ulong, string> playerNamesDict = new Dictionary<ulong, string>();

    public System.Action OnPlayerListUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }
    }

    public void SetupConnectionApproval(string myName)
    {
        if (NetworkManager.Singleton != null)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(myName);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = nameBytes;

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) =>
            {
                string clientName = Encoding.UTF8.GetString(request.Payload);
                if (string.IsNullOrEmpty(clientName)) clientName = "Joueur_" + request.ClientNetworkId;

                playerNamesDict[request.ClientNetworkId] = clientName;

                response.CreatePlayerObject = false; 
                response.Approved = true;
            };
        }
    }

    public void RegisterCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            SyncPlayerNamesClientRpc(GetSerializedPlayerNames());
        }
        OnPlayerListUpdated?.Invoke();
    }

    private void OnPlayerLeft(ulong clientId)
    {
        if (playerNamesDict.ContainsKey(clientId))
        {
            playerNamesDict.Remove(clientId);
        }

        if (NetworkManager.Singleton.IsServer)
        {
            SyncPlayerNamesClientRpc(GetSerializedPlayerNames());
        }
        OnPlayerListUpdated?.Invoke();
    }

    private string GetSerializedPlayerNames()
    {
        List<string> entries = new List<string>();
        foreach (var kvp in playerNamesDict)
        {
            entries.Add($"{kvp.Key}:{kvp.Value}");
        }
        return string.Join(";", entries);
    }

    [ClientRpc]
    private void SyncPlayerNamesClientRpc(string serializedNames)
    {
        playerNamesDict.Clear();
        string[] entries = serializedNames.Split(';');
        foreach (string entry in entries)
        {
            if (string.IsNullOrEmpty(entry)) continue;
            string[] parts = entry.Split(':');
            if (parts.Length == 2 && ulong.TryParse(parts[0], out ulong id))
            {
                playerNamesDict[id] = parts[1];
            }
        }
        OnPlayerListUpdated?.Invoke();
    }

    public string FormatPlayerListText()
    {
        if (NetworkManager.Singleton == null) return "";

        string listStr = $"JOUEURS DANS LE SALON ({NetworkManager.Singleton.ConnectedClientsIds.Count}/4) :\n\n";

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            string name = playerNamesDict.ContainsKey(clientId) ? playerNamesDict[clientId] : "Joueur_" + clientId;

            if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost)
            {
                listStr += $"• {name} (Hôte - Vous)\n";
            }
            else if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                listStr += $"• {name} (Vous)\n";
            }
            else if (clientId == NetworkManager.ServerClientId)
            {
                listStr += $"• {name} (Hôte)\n";
            }
            else
            {
                listStr += $"• {name}\n";
            }
        }

        return listStr;
    }

    public void RegisterHostName(string name)
    {
        if (NetworkManager.Singleton != null)
        {
            playerNamesDict[NetworkManager.Singleton.LocalClientId] = name;
            OnPlayerListUpdated?.Invoke();
        }
    }

    public void StartGameSequence()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[RESEAU] Changement de scène ordonné vers : {gameSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                ulong clientId = sceneEvent.ClientId;
                Debug.Log($"[RESEAU] Le joueur {clientId} a fini de charger la scène. Spawn du personnage...");
                SpawnPlayerForClient(clientId);
            }
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject == null)
            {
                Vector3 spawnPos = new Vector3(0f, 1f, 0f);
                Quaternion spawnRot = Quaternion.identity;

                if (TutorialProgress.hasReachedCheckpoint && TutorialProgress.checkpointPosition != Vector3.zero)
                {
                    float angle = clientId * (360f / 4f); 
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 1.5f; 
                    spawnPos = TutorialProgress.checkpointPosition + offset;
                    Debug.Log($"<color=cyan>[CHECKPOINT SPAWN] Joueur {clientId} réapparu au CHECKPOINT ({spawnPos}) !</color>");
                }
                else
                {
                    GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

                    if (spawnPoints != null && spawnPoints.Length > 0)
                    {
                        int spawnIndex = (int)clientId % spawnPoints.Length;
                        spawnPos = spawnPoints[spawnIndex].transform.position;
                        spawnRot = spawnPoints[spawnIndex].transform.rotation;
                    }
                    else
                    {
                        float angle = clientId * (360f / 4f); 
                        Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 2.5f; 
                        spawnPos = new Vector3(0f, 1f, 0f) + offset;
                    }
                }

                GameObject playerObj = Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab, spawnPos, spawnRot);
                
                NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
                netObj.SpawnAsPlayerObject(clientId, true);

                PlayerController pc = playerObj.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.TeleportClientRpc(spawnPos, spawnRot);

                    string nameToSet = playerNamesDict.ContainsKey(clientId) ? playerNamesDict[clientId] : "Joueur_" + clientId;
                    pc.playerName.Value = nameToSet;
                }
            }
        }
    }
}