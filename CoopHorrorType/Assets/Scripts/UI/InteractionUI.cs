using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("Références UI")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private RectTransform containerRect;

    [Header("Réglages Animations (DOTween)")]
    [SerializeField] private float showDuration = 0.22f;
    [SerializeField] private float hideDuration = 0.15f;
    [SerializeField] private Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);

    [Header("Couleurs")]
    [SerializeField] private string keyHighlightColor = "#FFD000"; 

    private CanvasGroup canvasGroup;
    private string lastPrompt = "";
    private bool isVisible = false;

    private Sequence currentSequence;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        canvasGroup = GetComponent<CanvasGroup>();
        if (containerRect == null) containerRect = GetComponent<RectTransform>();

        canvasGroup.alpha = 0f;
        containerRect.localScale = startScale;
        gameObject.SetActive(false);
    }

    public void ShowPrompt(string fullPrompt)
    {
        if (string.IsNullOrEmpty(fullPrompt))
        {
            HidePrompt();
            return;
        }

        if (isVisible && lastPrompt == fullPrompt) return;

        lastPrompt = fullPrompt;

        promptText.text = FormatPromptWithColor(fullPrompt);

        if (!isVisible)
        {
            isVisible = true;
            gameObject.SetActive(true);

            currentSequence?.Kill();
            currentSequence = DOTween.Sequence();

            containerRect.localScale = startScale;
            canvasGroup.alpha = 0f;

            currentSequence.Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad))
                           .Join(containerRect.DOScale(1f, showDuration).SetEase(Ease.OutBack))
                           .SetUpdate(true); // Fonctionne même si Time.timeScale = 0
        }
        else
        {
            currentSequence?.Kill();
            containerRect.localScale = Vector3.one;
            containerRect.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.15f, 5, 0.5f).SetUpdate(true);
        }
    }

    public void ShowPrompt(string key, string action)
    {
        if (string.IsNullOrEmpty(key)) ShowPrompt(action);
        else ShowPrompt($"{key} {action}");
    }

    public void HidePrompt()
    {
        if (!isVisible) return;

        isVisible = false;
        lastPrompt = "";

        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        currentSequence.Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad))
                       .Join(containerRect.DOScale(startScale, hideDuration).SetEase(Ease.InQuad))
                       .OnComplete(() => gameObject.SetActive(false))
                       .SetUpdate(true);
    }

    private string FormatPromptWithColor(string text)
    {
        if (text.Contains("[") && text.Contains("]"))
        {
            int start = text.IndexOf('[');
            int end = text.IndexOf(']');

            if (start != -1 && end != -1 && end > start)
            {
                string key = text.Substring(start, end - start + 1);
                string coloredKey = $"<b><color={keyHighlightColor}>{key}</color></b>";
                return text.Replace(key, coloredKey);
            }
        }
        return text;
    }
}