using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private float tweeenDuration = 1f;


    public void SetActiveWithTween()
    {
        transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        gameObject.SetActive(true);
        transform.DOScale(Vector3.one, tweeenDuration).SetEase(Ease.OutElastic);
    }
}
