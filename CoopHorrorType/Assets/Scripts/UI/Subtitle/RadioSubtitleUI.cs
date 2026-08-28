using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // Nécessite DOTween
using TMPro;
using UnityEngine;

public class RadioSubtitleUI : MonoBehaviour
{
    public static RadioSubtitleUI Instance { get; private set; }

    [Header("Références UI")]
    public GameObject radioBox;          
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI speakerText;  

    public Color normalColor; 
    public Color flashColor;  

    public bool enableRadioVibration = true; 
    public bool enableTypewriter = true;     
    public float charSpeed = 0.018f;         

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
                subtitleText.text = "";
                foreach (char c in phrase.text)
                {
                    subtitleText.text += c;
                    yield return new WaitForSeconds(charSpeed);
                }

                float typedTime = phrase.text.Length * charSpeed;
                float remainingTime = Mathf.Max(0.1f, phrase.duration - typedTime);
                yield return new WaitForSeconds(remainingTime);
            }
            else
            {
                subtitleText.text = phrase.text;
                yield return new WaitForSeconds(phrase.duration);
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