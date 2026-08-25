using DG.Tweening; // Nécessite DOTween
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public TextMeshPro textMesh;

    public void Setup(string text, Color color)
    {
        if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();

        textMesh.text = text;
        textMesh.color = color;

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one * 1.3f, 0.25f).SetEase(Ease.OutBack);

        transform.DOMoveY(transform.position.y + 1.8f, 0.9f).SetEase(Ease.OutCubic);

        textMesh.DOFade(0f, 0.4f).SetDelay(0.5f).OnComplete(() => Destroy(gameObject));
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }
}