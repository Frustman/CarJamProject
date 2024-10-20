using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public class Quest
{
    public string questKey;
    public int rewardIndex;
    public int rewardAmount;
    public string text;
    public string criteriaKey;
    public float criteriaValue;


    public bool CheckIsTrue()
    {
        return SaveManager.Instance.LoadFloat(string.Format("CarJam_{0}", criteriaKey)) >= criteriaValue;
    }
}
