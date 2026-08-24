using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : NetworkBehaviour
{
    [Header("Panneaux UI")]
    public GameObject mainPanel;        
    public GameObject lobbyPanel;       

    [Header("Champs de Texte")]
    public TMP_InputField playerNameInput; 
    public TMP_InputField joinCodeInput; 
    public TextMeshProUGUI displayCodeText;
    public TextMeshProUGUI statusText;    
    public TextMeshProUGUI playerListText; 

    [Header("Boutons")]
    public Button createHostButton;
    public Button joinGameButton;
    public Button startGameButton;     

    [Header("Nom de la Scène de Jeu")]
    public string gameSceneName = "TestScenes"; 

    private static Dictionary<ulong, string> playerNamesDict = new Dictionary<ulong, string>();

    private void Start()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        if (createHostButton != null) createHostButton.onClick.AddListener(OnCreateHostClicked);
        if (joinGameButton != null) joinGameButton.onClick.AddListener(OnJoinGameClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);

        if (playerNameInput != null)
        {
            playerNameInput.text = PlayerPrefs.GetString("PlayerName", "Joueur_" + Random.Range(100, 999));
        }
    }

    private string GetPlayerName()
    {
        string name = playerNameInput != null ? playerNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name))
        {
            name = "Joueur_" + Random.Range(100, 999);
        }
        PlayerPrefs.SetString("PlayerName", name); 
        return name;
    }

    private async void OnCreateHostClicked()
    {
        if (statusText != null) statusText.text = "Création du serveur Relay...";

        string localName = GetPlayerName();

        SetupConnectionApproval(localName);
        RegisterNetworkCallbacks();

        string code = await RelayManager.Instance.CreateRelay();

        if (!string.IsNullOrEmpty(code))
        {
            if (statusText != null) statusText.text = "Partie créée !";
            if (displayCodeText != null) displayCodeText.text = code;

            if (mainPanel != null) mainPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            
            if (startGameButton != null) startGameButton.gameObject.SetActive(true);

            playerNamesDict[NetworkManager.Singleton.LocalClientId] = localName;
            UpdatePlayerList();
        }
        else
        {
            if (statusText != null) statusText.text = "Erreur lors de la création du serveur.";
        }
    }

    private async void OnJoinGameClicked()
    {
        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";

        if (string.IsNullOrEmpty(code))
        {
            if (statusText != null) statusText.text = "Veuillez entrer un code valide !";
            return;
        }

        if (statusText != null) statusText.text = "Connexion à la partie...";

        string localName = GetPlayerName();

        SetupConnectionApproval(localName);
        RegisterNetworkCallbacks();

        bool success = await RelayManager.Instance.JoinRelay(code);

        if (success)
        {
            if (statusText != null) statusText.text = "Connecté ! En attente de l'hôte...";
            if (mainPanel != null) mainPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (displayCodeText != null) displayCodeText.text = code;

            if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        }
        else
        {
            if (statusText != null) statusText.text = "Erreur : Code invalide ou serveur plein.";
        }
    }

    private void SetupConnectionApproval(string myName)
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

    private void RegisterNetworkCallbacks()
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
        UpdatePlayerList();
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
        UpdatePlayerList();
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
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        if (playerListText == null || NetworkManager.Singleton == null) return;

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

        playerListText.text = listStr;
    }

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            SetPlayerSpawningInMenu(true);
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;

            Debug.Log($"[RESEAU] Lancement de la scène de jeu : {gameSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void SetPlayerSpawningInMenu(bool shouldSpawn)
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = (request, response) =>
            {
                response.CreatePlayerObject = shouldSpawn;
                response.Approved = true;
            };
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;

                Debug.Log("[RESEAU] Scène chargée ! Génération des joueurs sur des points séparés...");

                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

                int index = 0;
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject == null)
                    {
                        Vector3 spawnPos = new Vector3(0f, 1f, 0f);
                        Quaternion spawnRot = Quaternion.identity;

                        if (spawnPoints != null && spawnPoints.Length > 0)
                        {
                            int spawnIndex = index % spawnPoints.Length;
                            spawnPos = spawnPoints[spawnIndex].transform.position;
                            spawnRot = spawnPoints[spawnIndex].transform.rotation;
                        }
                        else
                        {
                            float angle = index * (360f / 4f); 
                            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 2f; 
                            spawnPos = new Vector3(0f, 1f, 0f) + offset;
                        }

                        GameObject playerObj = Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab, spawnPos, spawnRot);
                        
                        string nameToSet = playerNamesDict.ContainsKey(client.ClientId) ? playerNamesDict[client.ClientId] : "Joueur_" + client.ClientId;
                        PlayerController pc = playerObj.GetComponent<PlayerController>();
                        if (pc != null)
                        {
                            pc.playerName.Value = nameToSet;
                        }

                        playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId, true);

                        index++;
                    }
                }
            }
        }
    }
}