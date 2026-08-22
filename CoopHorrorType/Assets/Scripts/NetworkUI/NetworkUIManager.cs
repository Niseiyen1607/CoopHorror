using Unity.Netcode;
using UnityEngine;

public class NetworkUIManager : MonoBehaviour
{
    private string codeToJoin = "";
    private string currentJoinCode = "";
    private bool isConnecting = false;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (isConnecting)
            {
                GUILayout.Label("Connexion au serveur en cours...");
            }
            else
            {
                if (GUILayout.Button("Créer une partie (Host)", GUILayout.Height(40)))
                {
                    StartHostGame();
                }

                GUILayout.Space(10);

                GUILayout.Label("Entrer le Code de la partie :");
                codeToJoin = GUILayout.TextField(codeToJoin);

                if (GUILayout.Button("Rejoindre (Client)", GUILayout.Height(30)))
                {
                    StartClientGame();
                }
            }
        }
        else
        {
            if (NetworkManager.Singleton.IsHost)
            {
                GUILayout.Label("PARTIE CRÉÉE !");
                GUILayout.Label("CODE POUR TES POTES :");
                GUILayout.TextField(currentJoinCode, GUILayout.Height(30));
            }
            else
            {
                GUILayout.Label("CONNECTÉ À LA PARTIE !");
            }
        }

        GUILayout.EndArea();
    }

    private async void StartHostGame()
    {
        isConnecting = true;
        RelayManager relay = FindObjectOfType<RelayManager>();
        if (relay != null)
        {
            currentJoinCode = await relay.CreateRelay();
        }
        isConnecting = false;
    }

    private async void StartClientGame()
    {
        if (string.IsNullOrEmpty(codeToJoin)) return;

        isConnecting = true;
        RelayManager relay = FindObjectOfType<RelayManager>();
        if (relay != null)
        {
            await relay.JoinRelay(codeToJoin);
        }
        isConnecting = false;
    }
}