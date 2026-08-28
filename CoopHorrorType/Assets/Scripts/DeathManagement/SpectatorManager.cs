using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
            if (target != null && target.cameraHolder != null && Camera.main != null)
            {
                Camera.main.transform.position = target.cameraHolder.position;
                Camera.main.transform.rotation = target.cameraHolder.rotation;
            }
        }
    }

    public void SpectateNextPlayer()
    {
        FindAlivePlayers();

        if (alivePlayers.Count == 0)
        {
            if (spectatingText != null) 
                spectatingText.text = "<color=red>TOUTE L'ÉQUIPE EST MORTE !</color>";
            
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
}