using DG.Tweening;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    public CanvasGroup fadeCanvasGroup; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f; 
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(true);
        }
    }

    public Tween FadeToBlack(float duration = 0.5f)
    {
        if (fadeCanvasGroup == null) return null;
        fadeCanvasGroup.gameObject.SetActive(true);
        return fadeCanvasGroup.DOFade(1f, duration);
    }

    public Tween FadeToClear(float duration = 0.5f)
    {
        if (fadeCanvasGroup == null) return null;
        return fadeCanvasGroup.DOFade(0f, duration).OnComplete(() => {
            fadeCanvasGroup.blocksRaycasts = false;
        });
    }

    public Tween FadeToAlpha(float targetAlpha, float duration = 0.5f)
    {
        if (fadeCanvasGroup == null) return null;
        fadeCanvasGroup.gameObject.SetActive(true);
        return fadeCanvasGroup.DOFade(targetAlpha, duration);
    }
}