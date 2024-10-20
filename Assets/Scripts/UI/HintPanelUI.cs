using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintPanelUI : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Animator thinkingAnimator;

    private float thinkingTime;

    private readonly int Thinking = Animator.StringToHash("Think");
    private readonly int Yes = Animator.StringToHash("Yes");
    private readonly int No = Animator.StringToHash("No");
    private void OnEnable()
    {
        thinkingAnimator.SetBool(Thinking, true);
        thinkingAnimator.SetBool(Yes, false);
        thinkingAnimator.SetBool(No, false);
        DOVirtual.Vector3(Vector3.zero, Vector3.one, 0.5f, value =>
        {
            thinkingAnimator.transform.localScale = value;
        }).SetEase(Ease.OutElastic);

        thinkingTime = 0;
    }

    public void Update()
    {
        if(gameObject.activeInHierarchy) thinkingTime += Time.deltaTime;
    }


    public void SetYes()
    {
        if(thinkingTime < 2f)
        {
            DOVirtual.DelayedCall(2f - thinkingTime, () =>
            {
                thinkingAnimator.SetBool(Thinking, false);
                thinkingAnimator.SetBool(Yes, true);
                thinkingAnimator.SetBool(No, false);

                DOVirtual.DelayedCall(2f, () =>
                {
                    DOVirtual.Vector3(Vector3.one, Vector3.one * 0.2f, 0.5f, value =>
                    {
                        thinkingAnimator.transform.localScale = value;
                    }).SetEase(Ease.OutElastic).OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        GameManager.Instance.canCheckWin = true;
                    });
                });
            });
        } else
        {
            thinkingAnimator.SetBool(Thinking, false);
            thinkingAnimator.SetBool(Yes, true);
            thinkingAnimator.SetBool(No, false);

            DOVirtual.DelayedCall(2f, () =>
            {
                DOVirtual.Vector3(Vector3.one, Vector3.one * 0.2f, 0.5f, value =>
                {
                    thinkingAnimator.transform.localScale = value;
                }).SetEase(Ease.OutElastic).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    GameManager.Instance.canCheckWin = true;
                });
            });
        }
    }

    public void SetNo()
    {
        if (thinkingTime < 2f)
        {
            DOVirtual.DelayedCall(2f - thinkingTime, () =>
            {
                thinkingAnimator.SetBool(Thinking, false);
                thinkingAnimator.SetBool(Yes, false);
                thinkingAnimator.SetBool(No, true);

                DOVirtual.DelayedCall(2f, () =>
                {
                    DOVirtual.Vector3(Vector3.one, Vector3.one / 8f, 0.5f, value =>
                    {
                        thinkingAnimator.transform.localScale = value;
                    }).SetEase(Ease.OutElastic).OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        GameManager.Instance.canCheckWin = true;
                    });
                });
            });
        } else
        {
            thinkingAnimator.SetBool(Thinking, false);
            thinkingAnimator.SetBool(Yes, false);
            thinkingAnimator.SetBool(No, true);

            DOVirtual.DelayedCall(2f, () =>
            {
                DOVirtual.Vector3(Vector3.one, Vector3.one / 8f, 0.5f, value =>
                {
                    thinkingAnimator.transform.localScale = value;
                }).SetEase(Ease.OutElastic).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    GameManager.Instance.canCheckWin = true;
                });
            });
        }
    }


    private void OnDisable()
    {
        thinkingAnimator.SetBool(Thinking, false);
        thinkingAnimator.SetBool(Yes, false);
        thinkingAnimator.SetBool(No, false);
    }
}
