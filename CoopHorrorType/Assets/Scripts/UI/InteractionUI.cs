using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("Référence Texte")]
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        HidePrompt();
    }

    public void ShowPrompt(string key, string action)
    {
        if (promptText == null) return;

        if (!string.IsNullOrEmpty(key))
        {
            promptText.text = $"<color=#FFFFFF>{key}</color> {action}";
        }
        else
        {
            promptText.text = action;
        }

        promptText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.text = "";
            promptText.gameObject.SetActive(false);
        }
    }
}