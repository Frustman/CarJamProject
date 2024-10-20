using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmojiCollector : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float scaleVariation = 0.1f;
    [SerializeField] private float timeVariation = 0.2f;
    [SerializeField] private RectTransform targetPosition;


    public void Start()
    {
        PoolManager.Instance.AssignPoolingObject(iconPrefab);
    }

    public void InvokeEmojiCollect(int emojiCounts, float positionVariation, Vector3 worldPosition)
    {
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        float scaleDiff;
        for(int i = 0; i < emojiCounts; i++)
        {
            Image icon = PoolManager.Instance.Get(iconPrefab).GetComponent<Image>();
            icon.transform.SetParent(transform);
            icon.transform.rotation = Quaternion.identity;
            icon.gameObject.SetActive(true);
            icon.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            icon.rectTransform.position = screenPos;

            icon.transform.DOMove(screenPos + new Vector2(Random.Range(-positionVariation, positionVariation), Random.Range(-positionVariation, positionVariation)), 0.7f + Random.Range(-timeVariation, timeVariation)).SetEase(Ease.OutElastic);
            scaleDiff = 0.4f + Random.Range(-scaleVariation, scaleVariation);
            icon.transform.DOScale(new Vector3(scaleDiff, scaleDiff, scaleDiff), 0.1f).SetEase(Ease.OutElastic);
            DOVirtual.DelayedCall(1f + Random.Range(-timeVariation, timeVariation), () =>
            {
                float duration = 1f + Random.Range(-timeVariation, timeVariation);
                icon.transform.DOMove(targetPosition.position + new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f)), duration).SetEase(Ease.OutExpo).OnComplete(() =>
                {
                    PoolManager.Instance.Put(iconPrefab, icon.gameObject);
                    GameManager.Instance.IncreaseGold(1);
                    InputManager.Instance.InvokeVibrate(0);
                });
                icon.transform.DORotate(new Vector3(0, 0, Random.Range(-1040f, 1040f)), duration).SetEase(Ease.OutExpo);
            });
        }
    }
}
