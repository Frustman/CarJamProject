using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarUI : MonoBehaviour
{
    [Header("Prefab settings")]
    [SerializeField] private List<Transform> stars = new List<Transform>();
    [SerializeField] private List<Image> starImages = new();

    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite blankStarSprite;


    public void StartAnimation(int starCount)
    {
        for(int i = 0; i < stars.Count; i++)
        {
            stars[i].gameObject.SetActive(false);
            stars[i].localScale = new Vector3(0.4f, 0.4f, 0.4f);
            if (i < starCount)
                starImages[i].sprite = starSprite;
            else
                starImages[i].sprite = blankStarSprite;
        }

        stars[0].gameObject.SetActive(true);
        DOVirtual.DelayedCall(0.3f, () =>
        {
            stars[1].gameObject.SetActive(true);
            stars[1].DOScale(Vector3.one, 0.5f).SetEase(Ease.OutElastic);
        });
        DOVirtual.DelayedCall(0.8f, () =>
        {
            stars[2].gameObject.SetActive(true);
            stars[2].DOScale(Vector3.one, 0.5f).SetEase(Ease.OutElastic);
        });
        stars[0].DOScale(Vector3.one, 0.5f).SetEase(Ease.OutElastic);
    }
}
