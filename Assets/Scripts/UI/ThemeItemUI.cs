using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemeItemUI : MonoBehaviour
{
    [Header("Prefab Settings")]
    private CustomizingItem item;

    public bool isUnlocked;

    [SerializeField] private Image itemImage;
    [SerializeField] private GameObject maskObject;
    [SerializeField] private GameObject lockImage;

    public void Initialize(CustomizingItem item, Sprite image)
    {
        this.item = item;
        itemImage.sprite = image;

        isUnlocked = SaveManager.Instance.LoadInt(string.Format("CarJam_{0}", item.key)) == 1 ? true : false;
        maskObject.SetActive(!isUnlocked);
        lockImage.SetActive(!isUnlocked);
    }

    public void Refresh()
    {
        isUnlocked = SaveManager.Instance.LoadInt(string.Format("CarJam_{0}", item.key)) == 1 ? true : false;
        maskObject.SetActive(!isUnlocked);
        lockImage.SetActive(!isUnlocked);
    }

    public void Click()
    {
        isUnlocked = SaveManager.Instance.LoadInt(string.Format("CarJam_{0}", item.key)) == 1 ? true : false;
        if (isUnlocked)
        {
            HomeManager.Instance.SetTheme(item.imageIndex);
        }
    }
}
