using System.Collections;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("Panneaux UI")]
    public GameObject endPanel;          
    public GameObject hostButtonsGroup;  

    [Header("Textes UI")]
    public TextMeshProUGUI titleText;      
    public TextMeshProUGUI totalMoneyText;  
    public TextMeshProUGUI subDetailText;  

    [Header("Boutons")]
    public Button restartButton;
    public Button menuButton;

    [Header("Audio (Séquence Balatro)")]
    public AudioClip victorySound;      
    public AudioClip defeatSound;       
    public AudioClip countTickSound;    
    public AudioClip finalCashSound;    
    public AudioClip typewriterSound;   

    private void Awake() => Instance = this;

    private void Start()
    {
        if (endPanel != null) endPanel.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
    }

    public void DisplayEndScreen(bool isVictory, int totalMoneyEarned)
    {
        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.HideSpectatorHUD();
        }

        if (endPanel != null) endPanel.SetActive(true);

        if (hostButtonsGroup != null)
        {
            hostButtonsGroup.SetActive(NetworkManager.Singleton.IsHost);
        }

        StartCoroutine(AnimateBalatroRecapRoutine(isVictory, totalMoneyEarned));
    }

    private IEnumerator AnimateBalatroRecapRoutine(bool isVictory, int totalMoneyEarned)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeToBlack(0.8f).WaitForCompletion();
        }

        totalMoneyText.transform.localScale = Vector3.one;
        totalMoneyText.text = "GAINS TOTAUX : <color=#FFD700>$0</color>";
        if (subDetailText != null) subDetailText.text = "";

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToClear(0.8f);
        }

        if (isVictory)
        {
            titleText.text = "<color=#00FF66> VICTOIRE ! </color>\n<size=24>CONTRAT D'EXORCISME REMPLI</size>";
            if (AudioManager.Instance != null && victorySound != null)
                AudioManager.Instance.PlaySound2D(victorySound, 0.9f);
        }
        else
        {
            titleText.text = "<color=#FF2222> ÉQUIPE ÉLIMINÉE </color>\n<size=24>MISSION ÉCHOUÉE</size>";
            if (AudioManager.Instance != null && defeatSound != null)
                AudioManager.Instance.PlaySound2D(defeatSound, 0.9f);
        }

        titleText.transform.DOPunchScale(Vector3.one * 0.35f, 0.45f, 8, 0.5f);

        yield return new WaitForSeconds(0.6f);

        int rollingMoney = 0;
        int lastSoundMoney = 0;
        float duration = 1.4f;

        Tween countTween = DOTween.To(() => rollingMoney, x => {
            rollingMoney = x;
            totalMoneyText.text = $"GAINS TOTAUX : <color=#FFD700>${rollingMoney}</color>";

            if (rollingMoney - lastSoundMoney >= Mathf.Max(1, totalMoneyEarned / 20))
            {
                lastSoundMoney = rollingMoney;
                float progress = totalMoneyEarned > 0 ? (float)rollingMoney / totalMoneyEarned : 1f;
                float currentPitch = Mathf.Lerp(0.85f, 1.45f, progress);

                if (AudioManager.Instance != null && countTickSound != null)
                {
                    PlayPitchShiftedTick(countTickSound, 0.5f, currentPitch);
                }
            }
        }, totalMoneyEarned, duration).SetEase(Ease.OutQuad);

        yield return countTween.WaitForCompletion();

        if (totalMoneyEarned > 0)
        {
            if (AudioManager.Instance != null && finalCashSound != null)
            {
                AudioManager.Instance.PlaySound2D(finalCashSound, 0.95f, pitchRandomness: 0.05f);
            }

            totalMoneyText.transform.DOPunchScale(Vector3.one * 0.55f, 0.55f, 10, 0.5f);
        }

        yield return new WaitForSeconds(0.5f);

        if (AudioManager.Instance != null && typewriterSound != null)
        {
            AudioManager.Instance.PlaySound2D(typewriterSound, 0.6f);
        }

        if (subDetailText != null)
        {
            subDetailText.text = isVictory 
                ? "Rapport : Tuyaux réparés et recyclés au camion avec succès." 
                : "Rapport : L'équipe entière s'est faite dévorer par l'anomalie.";
            
            subDetailText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f);
        }
    }

    private void PlayPitchShiftedTick(AudioClip clip, float volume, float pitch)
    {
        GameObject tempGO = new GameObject("TempTickSound");
        AudioSource source = tempGO.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f; 
        source.Play();
        Destroy(tempGO, clip.length + 0.1f);
    }

    private void OnRestartClicked()
    {
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.RestartMission();
        }
    }

    private void OnMenuClicked()
    {
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ReturnToMenu();
        }
    }
}