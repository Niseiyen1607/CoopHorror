using DG.Tweening; // Nécessite DOTween
using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI moneyText;      
    public RectTransform moneyContainer;   

    public Color normalColor = new Color(1f, 0.85f, 0f);  
    public Color gainColor = new Color(0f, 1f, 0.4f);      

    private int displayedMoney = 0;

    private void Start()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.currentMoney.OnValueChanged += OnMoneyChanged;
            displayedMoney = EconomyManager.Instance.currentMoney.Value;
            UpdateTextInstant(displayedMoney);
        }
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.currentMoney.OnValueChanged -= OnMoneyChanged;
        }
    }

    private void OnMoneyChanged(int previousValue, int newValue)
    {
        if (moneyText == null) return;

        Transform targetTransform = moneyContainer != null ? moneyContainer.transform : moneyText.transform;

        targetTransform.DOKill(true);
        targetTransform.localScale = Vector3.one;
        targetTransform.DOPunchScale(Vector3.one * 0.45f, 0.35f, 10, 0.5f);

        moneyText.DOKill();
        moneyText.color = gainColor;
        moneyText.DOColor(normalColor, 0.5f).SetDelay(0.15f);

        DOTween.To(() => displayedMoney, x => {
            displayedMoney = x;
            UpdateTextInstant(displayedMoney);
        }, newValue, 0.45f).SetEase(Ease.OutQuad);
    }

    private void UpdateTextInstant(int amount)
    {
        if (moneyText == null || EconomyManager.Instance == null) return;

        int goal = EconomyManager.Instance.moneyGoal;
        
        moneyText.text = $"$ {amount} / {goal}";

        if (amount >= goal)
        {
            moneyText.text = $"$ {amount} / {goal} <color=#00FF66>[ OK ]</color>";
        }
    }
}