using Unity.Netcode;
using UnityEngine;

public class GameOverManager : NetworkBehaviour
{
    public static GameOverManager Instance { get; private set; }

    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isVictory = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void CheckGameOverState()
    {
        if (!IsServer || isGameOver.Value) return;

        int aliveCount = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject != null)
            {
                PlayerController pc = client.PlayerObject.GetComponent<PlayerController>();
                if (pc != null && !pc.isDead.Value)
                {
                    aliveCount++;
                }
            }
        }

        if (aliveCount == 0)
        {
            TriggerEndGame(false);
        }
    }

    public void TriggerEndGame(bool victory)
    {
        if (!IsServer || isGameOver.Value) return;

        isGameOver.Value = true;
        isVictory.Value = victory;

        StopAllMonstersAndSpawners();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAmbience();
        }

        int totalEarned = EconomyManager.Instance != null ? EconomyManager.Instance.currentMoney.Value : 0;

        ShowEndScreenClientRpc(victory, totalEarned);
    }

    [ClientRpc]
    private void ShowEndScreenClientRpc(bool victory, int totalEarned)
    {
        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.DisplayEndScreen(victory, totalEarned);
        }
    }

    private void StopAllMonstersAndSpawners()
    {
        StalkerSpawner[] spawners = FindObjectsOfType<StalkerSpawner>();
        foreach (var spawner in spawners)
        {
            spawner.StopSpawningLoop();
        }

        StalkerAI[] monsters = FindObjectsOfType<StalkerAI>();
        foreach (var monster in monsters)
        {
            if (monster.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
    }

    public void RestartMission()
    {
        if (IsServer)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            NetworkManager.Singleton.SceneManager.LoadScene(currentScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    public void ReturnToMenu()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}