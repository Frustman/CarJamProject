using System;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static MaxEvents;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;
    [Header("Prefab Settings")]
#if UNITY_ANDROID
    string adUnitId = "----";
#else
    string adUnitId = "----";
#endif

    int retryAttempt;

    public int rewardType = 0;

    public void InitializeRewardedAds()
    {
        Instance = this;
        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) => {

            LoadRewardedAd();
        };
        MaxSdk.InitializeSdk();
        // Attach callback
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

        // Load the first rewarded ad
    }


    public void ShowAdIfReady()
    {
        if (MaxSdk.IsInterstitialReady(adUnitId))
        {
            MaxSdk.ShowInterstitial(adUnitId);
        }

        if (MaxSdk.IsAppOpenAdReady(adUnitId))
        {
            MaxSdk.ShowAppOpenAd(adUnitId);
        }
        else
        {
            MaxSdk.LoadAppOpenAd(adUnitId);
        }
    }

    private void LoadRewardedAd()
    {
        MaxSdk.LoadRewardedAd(adUnitId);
    }

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is ready for you to show. MaxSdk.IsRewardedAdReady(adUnitId) now returns 'true'.

        // Reset retry attempt
        retryAttempt = 0;
    }

    private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Rewarded ad failed to load
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).

        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));

        Invoke("LoadRewardedAd", (float)retryDelay);
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad failed to display. AppLovin recommends that you load the next ad.
        LoadRewardedAd();
    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is hidden. Pre-load the next ad
        LoadRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {

        Debug.LogFormat("Rewarded user : {0}, {1}", reward.Amount, reward.Label);
        if (rewardType == 0)
        {
            UIManager.Instance.SetGoldButtonInteractable(false);
            GameManager.Instance.GetHunderedEmoji();
        }
        else if (rewardType == 1)
        {
            UIManager.Instance.SetRetryButtonInteractable(false);
            GameManager.Instance.RestartGame();
        } else if(rewardType == 2)
        {
            UIManager.Instance.SetUndoButtonInteractable(false);
            TraceManager.Instance.UndoPick();
        } else if(rewardType == 3)
        {
            GameManager.Instance.SetHeartFull();
        }
        else if (rewardType == 4)
        {
            UIManager.Instance.SetHintButtonInteractable(false);
            TraceManager.Instance.CheckCanSolve();
        }
        // The rewarded ad displayed and the user should receive the reward.
    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Ad revenue paid. Use this callback to track user revenue.
    }

    public void ShowRewardAdvertise()
    {
        if (rewardType == 0)
        {
            UIManager.Instance.SetGoldButtonInteractable(false);
            GameManager.Instance.GetHunderedEmoji();
        }
        else if (rewardType == 1)
        {
            UIManager.Instance.SetRetryButtonInteractable(false);
            GameManager.Instance.RestartGame();
        }
        else if (rewardType == 2)
        {
            UIManager.Instance.SetUndoButtonInteractable(false);
            TraceManager.Instance.UndoPick();
        }
        else if (rewardType == 3)
        {
            GameManager.Instance.SetHeartFull();
        }
        else if (rewardType == 4)
        {
            UIManager.Instance.SetHintButtonInteractable(false);
            TraceManager.Instance.CheckCanSolve();
        }
    }
    public void SetRewardType(int type)
    {
        rewardType = type;
    }
}
