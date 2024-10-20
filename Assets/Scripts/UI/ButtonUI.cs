using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 scaleDiff;
    [SerializeField] private float tweenDuration;
    private Vector3 initialScale;

    private void Start()
    {
        initialScale = Vector3.one;
    }

    public void OnPress()
    {
        InputManager.Instance.eventSystem.SetSelectedGameObject(null);
        transform.DOScale(initialScale - scaleDiff, tweenDuration / 2f).SetEase(Ease.OutElastic);

        DOVirtual.DelayedCall(tweenDuration / 2f - 0.1f, OnRelease);
    }

    public void OnRelease()
    {
        transform.DOScale(initialScale, tweenDuration / 5f).SetEase(Ease.OutCubic);

    }
}
