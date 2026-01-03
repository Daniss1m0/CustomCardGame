using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GlowEffect : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private float minAlpha = 0.4f, maxAlpha = 1.0f, duration = 0.5f;

    private Tween glowTween;

    private void OnEnable()
    {
        if (targetImage == null) 
            targetImage = GetComponent<Image>();

        Color c = targetImage.color;
        c.a = minAlpha;
        targetImage.color = c;

        glowTween = targetImage.DOFade(maxAlpha, duration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void OnDisable()
    {
        glowTween?.Kill();
    }
}