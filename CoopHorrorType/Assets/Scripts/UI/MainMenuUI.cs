using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panneaux UI")]
    public GameObject mainPanel;        
    public GameObject lobbyPanel;       

    [Header("Champs de Texte")]
    public TMP_InputField joinCodeInput; 
    public TextMeshProUGUI displayCodeText;
    public TextMeshProUGUI statusText;    

    [Header("Boutons")]
    public Button createHostButton;
    public Button joinGameButton;
    public Button startGameButton;     

    [Header("Nom de la Scène de Jeu")]
    public string gameSceneName = "TestScenes"; 

    private void Start()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        if (createHostButton != null) createHostButton.onClick.AddListener(OnCreateHostClicked);
        if (joinGameButton != null) joinGameButton.onClick.AddListener(OnJoinGameClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    private async void OnCreateHostClicked()
    {
        if (statusText != null) statusText.text = "Création du serveur Relay...";
        
        string code = await RelayManager.Instance.CreateRelay();

        if (!string.IsNullOrEmpty(code))
        {
            if (statusText != null) statusText.text = "Partie créée !";
            if (displayCodeText != null) displayCodeText.text = code;

            if (mainPanel != null) mainPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            
            if (startGameButton != null) startGameButton.gameObject.SetActive(true);
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

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[RESEAU] Lancement de la scène de jeu : {gameSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}