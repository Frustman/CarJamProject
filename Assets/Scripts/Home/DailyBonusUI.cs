using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyBonusUI : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Button claimButton;
    [SerializeField] private List<GameObject> rewardLocker = new();

    public DateTime lastGetDate;
    public int alreadyGetBonusCount;

    private Queue<(int, int)> itemGetQueue;


    public void HasNeedToCheck(Action<bool> callback)
    {
        string lastBonusGetDateText = SaveManager.Instance.LoadString("CarJam_LastDailyBonusGetDate");
        if (lastBonusGetDateText != "")
        {
            lastGetDate = DateTime.Parse(lastBonusGetDateText);
        }
        else
        {
            lastGetDate = DateTime.Today.AddDays(-1f);
        }
        StartCoroutine(TimeManager.Instance.GetDateTime(currentTime =>
        {
            callback(currentTime.DayOfYear != lastGetDate.DayOfYear);
        }));
    }


    public void CheckDailyInfomation()
    {
        alreadyGetBonusCount = SaveManager.Instance.LoadInt("CarJam_AlreadyGetBonusCount");
        string lastBonusGetDateText = SaveManager.Instance.LoadString("CarJam_LastDailyBonusGetDate");
        itemGetQueue = new();
        if (lastBonusGetDateText != "")
        {
            lastGetDate = DateTime.Parse(lastBonusGetDateText);
        } else
        {
            lastGetDate = DateTime.Today.AddDays(-1f);
        }

        for(int i = 0; i < rewardLocker.Count; i++)
        {
            rewardLocker[i].SetActive(i < alreadyGetBonusCount);
        }

        claimButton.interactable = false;
        StartCoroutine(TimeManager.Instance.GetDateTime(CheckIfCanGetDailyBonus));
    }

    public void CheckIfCanGetDailyBonus(DateTime currentTime)
    {
        if(currentTime.DayOfYear == lastGetDate.DayOfYear)
        {
            return;
        }

        claimButton.interactable = true;
    }


    public void Claim()
    {
        claimButton.interactable = false;
        StartCoroutine(TimeManager.Instance.GetDateTime(SaveCurrentTime));        
    }

    public void SaveCurrentTime(DateTime currentTime)
    {
        Debug.Log("SaveCurrent Time!");

        SaveManager.Instance.Save("CarJam_LastDailyBonusGetDate", currentTime.ToString());
        alreadyGetBonusCount++;
        SaveManager.Instance.Save("CarJam_AlreadyGetBonusCount", alreadyGetBonusCount);


        if (alreadyGetBonusCount == 1)
        {
            HomeManager.Instance.ShowItemGetPanel(0, 30);
        }
        else if (alreadyGetBonusCount == 2)
        {
            HomeManager.Instance.ShowItemGetPanel(0, 30);
            itemGetQueue.Enqueue((1, 100));
        } else if (alreadyGetBonusCount == 3)
        {
            HomeManager.Instance.ShowItemGetPanel(2, 10);
        }

        for (int i = 0; i < rewardLocker.Count; i++)
        {
            rewardLocker[i].SetActive(i < alreadyGetBonusCount);
        }

    }

    public void CheckIfItemQueueRemain()
    {
        if(itemGetQueue.TryDequeue(out (int, int) result))
        {
            HomeManager.Instance.ShowItemGetPanel(result.Item1, result.Item2);
        }
    }

    public void GetReward(int bonusIndex)
    {
        Debug.Log("Bonus Get!");
    }
}
