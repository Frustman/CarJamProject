using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleVariator : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Vector3 targetScale;
    [SerializeField] private float tweenDuration = 0.5f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    public void OnEnable()
    {
        StartGrow();
    }

    Tweener growTweener, shrinkTweener;
    public void StartGrow()
    {
        shrinkTweener = null;
        if (growTweener != null) growTweener.Kill(false);

        growTweener = DOVirtual.Vector3(Vector3.one, targetScale, tweenDuration, value =>
        {
            transform.localScale = value;
        }).SetEase(ease).OnComplete(StartShrink);
    }


    public void StartShrink()
    {
        growTweener = null;
        if(shrinkTweener != null) shrinkTweener.Kill(false);

        shrinkTweener = DOVirtual.Vector3(targetScale, Vector3.one, tweenDuration, value =>
        {
            transform.localScale = value;
        }).SetEase(ease).OnComplete(StartGrow);
    }
}
