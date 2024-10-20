using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private List<Quest> quests;
    [SerializeField] private QuestItemUI questItemPrefab;
    [SerializeField] private Transform scrollViewParent;
    [SerializeField] private List<Sprite> sprites;
    
    
    private List<QuestItemUI> questItems;

    public void Start()
    {
        questItems = new();
        for (int i = 0; i < quests.Count; i++)
        {
            QuestItemUI questItemUI =Instantiate(questItemPrefab, scrollViewParent);
            questItemUI.Initialize(quests[i], sprites[quests[i].rewardIndex]);
            questItems.Add(questItemUI);
        }
    }


    public bool HasNeedToCheck()
    {
        for(int i = 0; i < quests.Count; i++)
        {
            if (quests[i].CheckIsTrue() && SaveManager.Instance.LoadInt(string.Format("CarJam_{0}", quests[i].questKey)) == 0)
                return true;
        }
        return false;
    }

    public void CheckUpdateProperty()
    {
        for(int i = 0; i < questItems.Count; i++)
        {
            questItems[i].UpdateProperty();
        }
    }
}
