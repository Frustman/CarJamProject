using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemUI : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Quest quest;
    [SerializeField] private GameObject completeMask;
    [SerializeField] private TextMeshProUGUI desciptionText;
    [SerializeField] private TextMeshProUGUI processivityText;
    [SerializeField] private TextMeshProUGUI rewardAmountText;
    [SerializeField] private Image fillGage;
    [SerializeField] private int rewardIndex;
    [SerializeField] private int rewardAmount;
    [SerializeField] private Image rewardImage;

    public bool isAlreadyComplete = false;

    public void Initialize(Quest quest, Sprite sprite)
    {
        this.quest = quest;

        isAlreadyComplete = SaveManager.Instance.LoadInt(string.Format("CarJam_{0}", quest.questKey)) == 1 ? true : false;
        completeMask.SetActive(isAlreadyComplete);

        desciptionText.text = quest.text;
        rewardAmountText.text = quest.rewardAmount.ToString();
        processivityText.text = string.Format("{0} / {1}", (int)SaveManager.Instance.LoadFloat(string.Format("CarJam_{0}", quest.criteriaKey)), (int)quest.criteriaValue);
        rewardIndex = quest.rewardIndex;
        rewardAmount = quest.rewardAmount;

        fillGage.fillAmount = (int)SaveManager.Instance.LoadFloat(string.Format("CarJam_{0}", quest.criteriaKey)) / (int)quest.criteriaValue;
        rewardImage.sprite = sprite;
    }

    public void UpdateProperty()
    {
        isAlreadyComplete = SaveManager.Instance.LoadInt(string.Format("CarJam_{0}", quest.questKey)) == 1 ? true : false;
        completeMask.SetActive(isAlreadyComplete);
        fillGage.fillAmount = Min((int)SaveManager.Instance.LoadFloat(string.Format("CarJam_{0}", quest.criteriaKey)) / (int)quest.criteriaValue, 1.0f);
        processivityText.text = string.Format("{0} / {1}", (int)SaveManager.Instance.LoadFloat(string.Format("CarJam_{0}", quest.criteriaKey)), (int)quest.criteriaValue);
    }

    public float Min(float a, float b)
    {
        if (a < b) return a;
        return b;
    }

    public void Click()
    {
        if (quest.CheckIsTrue() && SaveManager.Instance.LoadInt(string.Format("CarJam_{0}", quest.questKey)) == 0)
        {
            SaveManager.Instance.Save(string.Format("CarJam_{0}", quest.questKey), 1);
            HomeManager.Instance.ShowItemGetPanel(quest.rewardIndex, quest.rewardAmount);
            isAlreadyComplete = true;
            completeMask.SetActive(isAlreadyComplete);
        }
    }
}
