using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class VoiceHUD : MonoBehaviour
{
    public static VoiceHUD Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image micIcon;
    [SerializeField] private Image voiceLevelFill; 

    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.4f, 1f); 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0.4f;
    }

    public void UpdateVoiceLevel(bool isTalking, float normalizedVolume)
    {
        if (micIcon == null) return;

        if (isTalking)
        {
            micIcon.color = activeColor;
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            float targetScale = Mathf.Clamp(1f + (normalizedVolume * 0.25f), 1f, 1.35f);
            micIcon.transform.localScale = Vector3.Lerp(micIcon.transform.localScale, Vector3.one * targetScale, Time.deltaTime * 20f);

            if (voiceLevelFill != null)
            {
                voiceLevelFill.fillAmount = Mathf.Clamp01(normalizedVolume);
            }
        }
        else
        {
            micIcon.color = idleColor;
            if (canvasGroup != null) canvasGroup.alpha = 0.4f;
            micIcon.transform.localScale = Vector3.Lerp(micIcon.transform.localScale, Vector3.one, Time.deltaTime * 10f);

            if (voiceLevelFill != null)
            {
                voiceLevelFill.fillAmount = 0f;
            }
        }
    }
}