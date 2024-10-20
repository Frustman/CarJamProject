using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class ThemeSelectorUI : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private ThemeItemUI customizingItemPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private List<Sprite> themeImage = new List<Sprite>();
    [SerializeField] private TextMeshProUGUI themeNameText;

    private List<ThemeItemUI> itemList = new List<ThemeItemUI>();

    public void UnlockAllTheme()
    {
        for(int i = 0; i < themeImage.Count; i++)
        {
            SaveManager.Instance.Save(string.Format("CarJam_Theme{0}", i), 1);
        }
        Refresh();
    }

    public void Start()
    {
        for(int i = 0; i < themeImage.Count; i++)
        {
            Theme theme = new Theme();
            theme.name = themeImage[i].name;
            theme.key = string.Format("Theme{0}", i);
            theme.imageIndex = i;
            ThemeItemUI item = Instantiate(customizingItemPrefab, gridParent);
            item.Initialize(theme, themeImage[i]);

            itemList.Add(item);
        }
    }

    public void Refresh()
    {
        for(int i = 0; i < itemList.Count; i++)
        {
            itemList[i].Refresh();
        }
    }

    public void ChangeThemeName(int themeIndex)
    {
        themeNameText.text = themeImage[themeIndex].name;
    }
}
