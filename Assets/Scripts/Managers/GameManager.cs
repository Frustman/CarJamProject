using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance = null;

    [Header("Dependency Settings")]
    [SerializeField] private AdManager adManager;
    [SerializeField] private CustomerSpawner spawner;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private PickVehicle pickVehicle;
    [SerializeField] private HardLevelAnimator hardLevelAnimator;
    [SerializeField] private EmojiCollector emojiCollector;
    [SerializeField] private RandomLevelGenerator randomLevelGenerator;

    [Header("Level Settings")]
    [SerializeField] private List<LevelData> levelDatas = new List<LevelData>();

    [Header("Runtime")]
    [SerializeField] private int difficulty = 0;
    [SerializeField] private int stageLevel = 1;
    [SerializeField] private int gold = 0;
    [SerializeField] private int heart = 10;
    [SerializeField] private int gem = 0;

    // Public

    [HideInInspector] public bool isGameRunning = false;
    [HideInInspector] public bool isInPause = false;
    [HideInInspector] public bool isRunningWinAnimation;
    [HideInInspector] public int stageGettingGoldAmount = 0;
    [HideInInspector] public int lastStageGettingGoldAmount = 0;
    [HideInInspector] public bool canCheckWin = true;

    // Private

    private LevelData currentLevel;

    private int starCount = 3;



    private void Awake()
    {
        if (null == Instance)
        {
            Instance = this;
        } else
        {
            Destroy(this);
        }
        DOTween.SetTweensCapacity(200, 312);
    }

    public void Start()
    {
        if (SaveManager.Instance.HasKey("CarJam_Level")){
            stageLevel = SaveManager.Instance.LoadInt("CarJam_Level");
            heart = SaveManager.Instance.LoadInt("CarJam_Heart");
            gold = SaveManager.Instance.LoadInt("CarJam_Gold");
            gem = SaveManager.Instance.LoadInt("CarJam_Gem");
            TimeManager.Instance.playTime = SaveManager.Instance.LoadFloat("CarJam_PlayTime") * 60f;
        } else
        {
            stageLevel = 1;
            heart = 10;
            gold = 0;
            gem = 0;
            SaveManager.Instance.Save("CarJam_Level", 1);
            SaveManager.Instance.Save("CarJam_Heart", 10);
            SaveManager.Instance.Save("CarJam_Gold", 0);
            SaveManager.Instance.Save("CarJam_Gem", 0);
            SaveManager.Instance.Save("CarJam_PlayTime", 0);
        }

        isRunningWinAnimation = false;
        UIManager.Instance.RefreshUI();
        SceneManager.LoadScene("HomeScene", LoadSceneMode.Additive);
        adManager.InitializeRewardedAds();
        canCheckWin = true;
        StartCoroutine(TimeManager.Instance.GetDateTime(currentTime =>
        {
            if (SaveManager.Instance.HasKey("CarJam_LastHeartGetTime"))
            {
                DateTime lastGetTime = DateTime.Parse(SaveManager.Instance.LoadString("CarJam_LastHeartGetTime"));
                TimeSpan passTime = currentTime.Subtract(lastGetTime);
                if(passTime.TotalMinutes > 30f)
                {
                    IncreaseHeart((int)(passTime.TotalMinutes / 30f));
                    Debug.LogFormat("Get a New Heart! : {0}", (int)(passTime.TotalMinutes / 30f));
                }

                currentTime.AddMinutes(-(passTime.TotalMinutes % 30f));
                SaveManager.Instance.Save("CarJam_LastHeartGetTime", currentTime.ToString());
            } else
            {
                SaveManager.Instance.Save("CarJam_LastHeartGetTime", currentTime.ToString());
                Debug.Log("Current Time Data Saved");
            }
        }));

    }

    public int GetGoldAmount()
    {
        return gold;
    }

    public int GetGemAmount()
    {
        return gem;
    }

    public void IncreaseGem(int amount)
    {
        gem += amount;
        SaveManager.Instance.Save("CarJam_Gem", gem);
    }

    public void DecreaseGem(int amount)
    {
        gem -= amount;
        SaveManager.Instance.Save("CarJam_Gem", gem);
    }

    public void DecreaseHeart()
    {
        heart--;
        SaveManager.Instance.Save("CarJam_Heart", heart);
    }

    private DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> scaleTween;
    public void DecreaseGold(int decreaseAmount)
    {
        gold -= decreaseAmount;
        UIManager.Instance.RefreshUI();
        UIManager.Instance.GoldGetAnimation();
    }

    public void IncreaseGold(int increaseAmount)
    {
        gold += increaseAmount;
        UIManager.Instance.RefreshUI();
        UIManager.Instance.GoldGetAnimation();
    }

    public void IncreaseHeart(int amount)
    {
        if (heart + amount < 10)
            heart += amount;
        else
            heart = 10;
        SaveManager.Instance.Save("CarJam_Heart", heart);
        if(HomeManager.Instance != null) HomeManager.Instance.Initialize();
    }
    public int GetStageLevel()
    {
        return stageLevel;
    }
    public int GetDifficulty()
    {
        return difficulty;
    }
    public int GetHeart()
    {
        return heart;
    }
    public string GetCurrentLevelThemeName()
    {
        return "Thema_City";
    }

    public void GetRidingGold(int count, float positionVariation, Vector3 position)
    {
        if (stageGettingGoldAmount + count > lastStageGettingGoldAmount)
        {
            if (lastStageGettingGoldAmount > stageGettingGoldAmount)
            {
                emojiCollector.InvokeEmojiCollect(lastStageGettingGoldAmount - stageGettingGoldAmount, positionVariation, position);
            }
            else
            {
                emojiCollector.InvokeEmojiCollect(count, positionVariation, position);
            }
        }
        stageGettingGoldAmount += count;
    }

    public void InvokeEmojiController(int count, float positionVariation, Vector3 position)
    {
        emojiCollector.InvokeEmojiCollect(count, positionVariation, position);
    }

    public void SetPauseState(bool state)
    {
        isInPause = state;
    }

    public void ResetLevel()
    {
        PlayerPrefs.DeleteAll();
        gold = 0;
        heart = 10;
        stageLevel = 1;
        gem = 0;
        SaveManager.Instance.Save("CarJam_Level", stageLevel);
        SaveManager.Instance.Save("CarJam_Heart", heart);
        SaveManager.Instance.Save("CarJam_Gold", gold);
        SaveManager.Instance.Save("CarJam_Gem", gem);
        HomeManager.Instance.Initialize();
        UIManager.Instance.RefreshUI();

        Application.Quit();
    }

    public void GoToNextDifficulty()
    {
        isRunningWinAnimation = false;
        PoolManager.Instance.ResetObjects();
        UIManager.Instance.SetCheckImageActive(false);
        SaveManager.Instance.Save("CarJam_Gold", gold);
        SaveManager.Instance.Save("CarJam_Gem", gem);
        difficulty = 1;
        StartGame(stageLevel);
        starCount = 3;
    }
    public void GoToNextLevel()
    {
        SaveManager.Instance.Save(string.Format("CarJam_{0}_{1}", currentLevel, difficulty), stageGettingGoldAmount);
        if (difficulty == 0) difficulty = 1;
        else
        {
            stageLevel++;
            difficulty = 0;
        }
        StartGame(stageLevel);
        starCount = 3;
    }

    public void LoadHome()
    {
        hardLevelAnimator.ResetDevilLevel();
        SaveManager.Instance.Save(string.Format("CarJam_{0}_{1}", currentLevel, difficulty), stageGettingGoldAmount);
        difficulty = 0;
        HomeManager.Instance.Initialize();
        HomeManager.Instance.GoHome();
        SaveManager.Instance.Save("CarJam_Gold", gold);
        SaveManager.Instance.Save("CarJam_Heart", heart);
    }

    public void Win()
    {
        if (!isRunningWinAnimation)
        {
            isRunningWinAnimation = true;
            InputManager.Instance.InvokeVibrate(2);
            SaveManager.Instance.Save(string.Format("CarJam_{0}_{1}", currentLevel, difficulty), stageGettingGoldAmount);
            spawner.needToSpawnCustomer = false;
            if (difficulty == 0)
            {
                UIManager.Instance.CheckAnimation();
            }
            else
            {
                InvokeEmojiController(50, 300f, transform.position);
                DOVirtual.DelayedCall(3f, WinPanelShow);
            }
        }
    }

    public void GetHunderedEmoji()
    {
        InvokeEmojiController(100, 300f, transform.position);
    }

    public void WinPanelShow()
    {
        PoolManager.Instance.ResetObjects();
        UIManager.Instance.ShowWinPanel();

        hardLevelAnimator.ResetDevilLevel();

        if (SaveManager.Instance.LoadInt("CarJam_Level") < stageLevel + 1)
        {
            SaveManager.Instance.Save("CarJam_Level", stageLevel + 1);
        }
        SaveManager.Instance.Save("CarJam_Gold", gold);
        SaveManager.Instance.Save("CarJam_Gem", gem);


        isRunningWinAnimation = false;
    }
    public void DecreaseStar()
    {
        starCount--;
        if(starCount < 0)
        {
            starCount = 0;
        }
    }

    public void StartGame()
    {
        StartGame(stageLevel);
        starCount = 3;
    }


    private void StartGame(int level)
    {
        spawner.CancelTasks();

        if (level * 2 + difficulty <= levelDatas.Count)
        {
            currentLevel = levelDatas[(level - 1 ) * 2 + difficulty];
        }
        else currentLevel = randomLevelGenerator.GenerateLevelByDifficulty(level, difficulty);



        if (level == 1 && difficulty == 0) TutorialManager.Instance.StartTutorial();
        if (currentLevel.difficulty == 1) hardLevelAnimator.EnterDevilLevel();
        UIManager.Instance.RefreshUI();
        isGameRunning = true;
        spawner.StartLevel(currentLevel);
        roundManager.StartLevel(currentLevel);
        stageGettingGoldAmount = 0;
        lastStageGettingGoldAmount = SaveManager.Instance.LoadInt(string.Format("CarJam_{0}_{1}", currentLevel, difficulty));

    }

    public void Defeat()
    {
        SaveManager.Instance.Save(string.Format("CarJam_{0}_{1}", currentLevel, difficulty), stageGettingGoldAmount);
        isGameRunning = false;
        UIManager.Instance.ShowDefeatPanel();
    }


    public void CheckCanWin()
    {
        if (canCheckWin)
        {
            if (gold >= 30)
            {
                canCheckWin = false;
                DecreaseGold(30);
                SaveManager.Instance.Save("CarJam_Gold", gold);
                TraceManager.Instance.CheckCanSolve();
            }
            else
            {
                UIManager.Instance.ShowHintAdvertisePanel();
            }
        }
    }

    public void SetGameState(bool state)
    {
        isGameRunning = state;
    }

    public void SetHeartFull()
    {
        heart = 10;
        SaveManager.Instance.Save("CarJam_Heart", 10);
        HomeManager.Instance.Initialize();
    }

    public void RestartUsingGold()
    {
        if (gold >= 200)
        {
            DecreaseGold(200);
            SaveManager.Instance.Save("CarJam_Gold", gold);
            RestartGame();
        }
        else
        {
            UIManager.Instance.ShowRetryAdvertisePanel();
        }
    }

    public void RestartUsingGoldByItem()
    {
        if (gold >= 200)
        {
            DecreaseGold(200);
            SaveManager.Instance.Save("CarJam_Gold", gold);
            RestartGame();
        }
        else
        {
            UIManager.Instance.ShowRestartAdvertisePanel();
        }
    }

    public void Undo10TimesUsingGold()
    {
        if(gold >= 10)
        {
            DecreaseGold(10);
            TraceManager.Instance.UndoPick();
        } else
        {
            UIManager.Instance.ShowRetryAdvertisePanel();
        }
    }

    public void RestartGame()
    {
        SaveManager.Instance.Save(string.Format("CarJam_{0}_{1}", currentLevel, difficulty), stageGettingGoldAmount);
        PoolManager.Instance.ResetObjects();
        StartGame(stageLevel);
        UIManager.Instance.RefreshUI();
    }
}
