using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class RadioSubtitleUI : MonoBehaviour
{
    public static RadioSubtitleUI Instance { get; private set; }

    [Header("Références UI")]
    public GameObject radioBox;          
    public TextMeshProUGUI subtitleText; 
    public TextMeshProUGUI speakerText;  

    [Header("Couleurs Rétro CRT / Walkie-Talkie")]
    public Color normalColor = new Color(1f, 0.9f, 0.2f); 
    public Color flashColor = new Color(1f, 1f, 1f);      

    [Header("Réglages Juiciness Rétro (Sans Scale)")]
    public bool enableRadioVibration = true; 
    public bool enableTypewriter = true;     
    public float charSpeed = 0.03f;      

    private Coroutine playPhrasesRoutine;

    private void Awake()
    {
        Instance = this;
        if (radioBox != null) radioBox.SetActive(false);
    }

    public void ShowPhrases(string speakerName, List<SubtitlePhrase> phrases)
    {
        if (radioBox == null || subtitleText == null || phrases == null || phrases.Count == 0) return;

        if (playPhrasesRoutine != null) StopCoroutine(playPhrasesRoutine);
        playPhrasesRoutine = StartCoroutine(PlayPhrasesRoutine(speakerName, phrases));
    }

    private IEnumerator PlayPhrasesRoutine(string speakerName, List<SubtitlePhrase> phrases)
    {
        radioBox.SetActive(true);
        if (speakerText != null) speakerText.text = $"{speakerName.ToUpper()}";

        foreach (var phrase in phrases)
        {
            if (string.IsNullOrEmpty(phrase.text)) continue;

            if (enableRadioVibration && radioBox != null)
            {
                radioBox.transform.DOKill(true);
                radioBox.transform.DOPunchPosition(new Vector3(4f, 2f, 0f), 0.18f, 10, 0.5f);
            }

            subtitleText.DOKill();
            subtitleText.color = flashColor;
            subtitleText.DOColor(normalColor, 0.2f);

            if (enableTypewriter)
            {
                float timer = 0f;
                int totalChars = phrase.text.Length;
                
                float typingDuration = Mathf.Min(phrase.duration * 0.65f, totalChars * charSpeed);

                while (timer < phrase.duration)
                {
                    timer += Time.unscaledDeltaTime; 

                    if (typingDuration > 0f)
                    {
                        float progress = Mathf.Clamp01(timer / typingDuration);
                        int visibleChars = Mathf.FloorToInt(progress * totalChars);
                        subtitleText.text = phrase.text.Substring(0, visibleChars);
                    }
                    else
                    {
                        subtitleText.text = phrase.text;
                    }

                    yield return null; 
                }
            }
            else
            {
                subtitleText.text = phrase.text;
                yield return new WaitForSecondsRealtime(phrase.duration);
            }
        }

        radioBox.SetActive(false);
    }

    public void HideBox()
    {
        if (playPhrasesRoutine != null) StopCoroutine(playPhrasesRoutine);
        if (radioBox != null) radioBox.SetActive(false);
    }
}