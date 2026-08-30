using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // OBLIGATOIRE pour DOTween .WaitForCompletion()
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance { get; private set; }

    [Header("UI Spectateur")]
    public GameObject spectatorHUD;      
    public TextMeshProUGUI spectatingText; 

    private List<PlayerController> alivePlayers = new List<PlayerController>();
    private int currentTargetIndex = 0;
    private bool isSpectating = false;

    private void Awake()
    {
        Instance = this;
        if (spectatorHUD != null) spectatorHUD.SetActive(false);
    }

    public void StartSpectating()
    {
        StartCoroutine(DeathSpectateTransitionRoutine());
    }

    private IEnumerator DeathSpectateTransitionRoutine()
    {
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeToBlack(0.5f).WaitForCompletion();
        }

        isSpectating = true;
        if (spectatorHUD != null) spectatorHUD.SetActive(true);

        FindAlivePlayers();
        SpectateNextPlayer();

        yield return new WaitForSeconds(0.2f);

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToClear(0.5f);
        }
    }

    private void Update()
    {
        if (!isSpectating) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            SpectateNextPlayer();
        }

        if (alivePlayers.Count > 0 && currentTargetIndex < alivePlayers.Count)
        {
            PlayerController target = alivePlayers[currentTargetIndex];
            if (target != null && target.cameraHolder != null)
            {
                Camera myCam = Camera.main;
                if (myCam != null)
                {
                    myCam.transform.position = target.cameraHolder.position;
                    myCam.transform.rotation = target.cameraHolder.rotation;
                }
            }
        }
    }

    public void SpectateNextPlayer()
    {
        FindAlivePlayers();

        if (alivePlayers.Count == 0)
        {
            HideSpectatorHUD();
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (GameOverManager.Instance != null)
                {
                    GameOverManager.Instance.CheckGameOverState();
                }
            }
            return;
        }

        currentTargetIndex = (currentTargetIndex + 1) % alivePlayers.Count;
        PlayerController target = alivePlayers[currentTargetIndex];

        if (spectatingText != null && target != null)
        {
            spectatingText.text = $"[SPECTATEUR] Observation de : {target.playerName.Value}\n<size=18>[Clic Gauche / Espace] Joueur suivant</size>";
        }
    }

    public void HideSpectatorHUD()
    {
        isSpectating = false;
        if (spectatorHUD != null)
        {
            spectatorHUD.SetActive(false);
        }

        if (Camera.main != null)
        {
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }
    }

    private void FindAlivePlayers()
    {
        alivePlayers.Clear();

        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();

        foreach (PlayerController pc in allPlayers)
        {
            if (pc != null && !pc.isDead.Value)
            {
                alivePlayers.Add(pc);
            }
        }

        if (alivePlayers.Count == 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.CheckGameOverState();
            }
        }
    }

    private void ReturnToMenu()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}