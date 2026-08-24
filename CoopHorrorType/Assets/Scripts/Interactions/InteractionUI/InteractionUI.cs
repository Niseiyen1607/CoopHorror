using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [Header("Références UI")]
    public GameObject promptPanel;
    public TextMeshProUGUI keyText;
    public TextMeshProUGUI actionText;

    private void Awake()
    {
        Instance = this;
        HidePrompt(); 
    }

    public void ShowPrompt(string key, string description)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (keyText != null) keyText.text = key;
        if (actionText != null) actionText.text = description;
    }

    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }
}