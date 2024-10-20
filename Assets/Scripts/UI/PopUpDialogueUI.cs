using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopUpDialogueUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float popUpDuration = 1f;
    [SerializeField] private float easeDuration = 0.1f;


    private Tween delayedCall;
    private Tweener showTween, hideTween;
    public void Show(string text)
    {
        gameObject.SetActive(true);
        this.text.text = text;

        showTween?.Kill(false);
        hideTween?.Kill(false);

        showTween = DOVirtual.Vector3(Vector3.zero, Vector3.one, easeDuration, value =>
        {
            transform.localScale = value;
        }).OnComplete(() =>
        {
            showTween = null;
        }).SetEase(Ease.OutCubic);

        if(delayedCall != null) delayedCall.Kill(false);
        delayedCall = DOVirtual.DelayedCall(popUpDuration - easeDuration, Hide);
    }

    public void Hide()
    {
        hideTween = DOVirtual.Vector3(Vector3.one, Vector3.zero, easeDuration, value =>
        {
            transform.localScale = value;
        }).OnComplete(() =>
        {
            hideTween = null;
            gameObject.SetActive(false);
        }).SetEase(Ease.OutCubic);
        delayedCall = null;
    }
}
