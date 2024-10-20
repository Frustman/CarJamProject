using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HardLevelAnimator : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Transform devilTarget;
    [SerializeField] private Transform levelLabelTarget;
    [SerializeField] private Image devilImage;
    [SerializeField] private Image backGround;
    [SerializeField] private Color devilColor;

    Tween animationTween;
    Tweener colorTweener;
    private Vector3 initialPos;

    private void Start()
    {
        initialPos = levelText.transform.position;
    }

    TweenerCore<Vector3, Vector3, VectorOptions> moveTweener, scaleTweener, textMoveTweener;
    public void EnterDevilLevel()
    {
        backGround.gameObject.SetActive(true);
        Color initColor = backGround.color;

        DOVirtual.Float(0, 0.6f, 0.1f, value =>
        {
            initColor.a = value;
            backGround.color = initColor;
        });


        devilImage.rectTransform.anchoredPosition = Vector2.zero;
        devilImage.transform.localScale = Vector3.zero;
        devilImage.gameObject.SetActive(true);
        devilImage.transform.DOScale(new Vector3(2f, 2f, 2f), 1f).SetEase(Ease.OutBounce);
        levelText.color = Color.white;
        if (animationTween != null) animationTween.Kill(false);
        animationTween = DOVirtual.DelayedCall(1.5f, () =>
        {
            animationTween = null;

            DOVirtual.Float(0.6f, 0, 1f, value =>
            {
                initColor.a = value;
                backGround.color = initColor;
            }).OnComplete(() =>
            {
                backGround.gameObject.SetActive(false);
            });
            if (moveTweener != null) moveTweener.Kill(false);
            if (scaleTweener != null) scaleTweener.Kill(false);
            if (colorTweener != null) colorTweener.Kill(false);
            moveTweener = devilImage.transform.DOMove(devilTarget.position, 1.5f).SetEase(Ease.InOutCubic).OnComplete(() =>
            {
                moveTweener = null;
            });
            scaleTweener = devilImage.transform.DOScale(new Vector3(0.3f, 0.3f, 0.3f), 1.5f).SetEase(Ease.InOutCubic).OnComplete(() =>
            {
                scaleTweener = null;
            });
            colorTweener = DOVirtual.Color(Color.white, devilColor, 1.5f, value =>
            {
                colorTweener = null;
                levelText.color = value;
            }).SetEase(Ease.InOutCubic);
            textMoveTweener = levelText.transform.DOMove(levelLabelTarget.position, 1.5f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                textMoveTweener = null;
            });
        });
    }

    public void ResetDevilLevel()
    {
        if (animationTween != null) animationTween.Kill(false);
        if (moveTweener != null) moveTweener.Kill(false);
        if (scaleTweener != null) scaleTweener.Kill(false);
        if (colorTweener != null) colorTweener.Kill(false);
        devilImage.rectTransform.anchoredPosition = Vector2.zero;
        devilImage.transform.localScale = Vector3.zero;
        devilImage.gameObject.SetActive(false);
        levelText.color = Color.white;
        levelText.transform.position = initialPos;
    }
}
