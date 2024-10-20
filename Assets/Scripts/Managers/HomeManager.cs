using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeManager : MonoBehaviour
{
    public static HomeManager Instance;

    [Header("Dependency Settings")]
    [SerializeField] private WorldRotator worldRotator;


    [Header("Home Panel Settings")]
    [SerializeField] private GameObject vibrateToggleImage;
    [SerializeField] private TextMeshProUGUI heartText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private Image loadingBar;
    [SerializeField] private RawImage loadingBackground;
    [SerializeField] private DialogueUI heartAdvertisPanel;
    [SerializeField] private GameObject homeCamera;
    [SerializeField] private ThemeSelectorUI themeSelectorUI;

    [Header("Item Get Panel Settings")]
    [SerializeField] private List<Sprite> itemIcons = new List<Sprite>();
    [SerializeField] private CanvasGroup itemGetPanel;
    [SerializeField] private ScaleVariator itemScaleVariator;
    [SerializeField] private Image getItemImage;
    [SerializeField] private TextMeshProUGUI itemText;


    [Space(10f)]
    [SerializeField] private CanvasGroup loadingPanel;
    [SerializeField] private CanvasGroup homePanel;

    [Space(10f)]
    [SerializeField] private DailyBonusUI dailyBonusUI;
    [SerializeField] private QuestUI questUI;
    [SerializeField] private GameObject questAlertIcon;
    [SerializeField] private GameObject dailyBonusAlertIcon;
    [SerializeField] private GameObject newsAlertIcon;

    [Space(10f)]
    public bool isInfiniteGas = false;
    [SerializeField] private Image heart;
    private Vector3 initialHeartPosition;
    private List<string> loadedScene;

    private float remainingInfiniteGasTime;

    private void Awake()
    {
        Instance = this;
        loadedScene = new List<string>();
        initialHeartPosition = heart.transform.position;
    }

    public void ResetGame()
    {
        GameManager.Instance.ResetLevel();
    }
    public void SetTheme(int index)
    {
        worldRotator.SetTheme(index);
        themeSelectorUI.ChangeThemeName(index);
    }
    public void Initialize()
    {
        vibrateToggleImage.SetActive(InputManager.Instance.isVibrateOn);
        worldRotator.SetRendererActive(true);
        goldText.text = GameManager.Instance.GetGoldAmount().ToString();
        startButtonText.text = "Level " + GameManager.Instance.GetStageLevel().ToString();
        if(!isInfiniteGas) heartText.text = GameManager.Instance.GetHeart().ToString();
        gemText.text = GameManager.Instance.GetGemAmount().ToString();


        isInfiniteGas = SaveManager.Instance.LoadInt("CarJam_IsInfiniteGas") == 1 ? true : false;
        remainingInfiniteGasTime = SaveManager.Instance.LoadFloat("CarJam_RemainingInfiniteGasTime");

        /*
        dailyBonusUI.HasNeedToCheck(value =>
        {
            dailyBonusAlertIcon.SetActive(value);
        });

        questAlertIcon.gameObject.SetActive(questUI.HasNeedToCheck());

        if (remainingInfiniteGasTime > 0)
        {
            if(checkInfiniteGasTimeCoroutine == null)
            {
                checkInfiniteGasTimeCoroutine = StartCoroutine(CheckInfiniteGasTime());
            }
        }*/

        worldRotator.StartRotate();
    }

    public void SetVibrateToggle()
    {
        if (InputManager.Instance.SetHomeVibrateToggle())
        {
            vibrateToggleImage.SetActive(true);
        } else
        {
            vibrateToggleImage.SetActive(false);
        }
    }

    private void Start()
    {
        Initialize();
    }

    public void GoHome()
    {
        loadingPanel.gameObject.SetActive(true);
        loadingBar.fillAmount = 0.01f;
        moveBackgroundCoroutine = StartCoroutine(MoveLoadingPanelBackground());
        DOVirtual.Float(0f, 1f, 1f, value =>
        {
            loadingPanel.alpha = value;
        }).OnComplete(() =>
        {
            UIManager.Instance.SetGameUIActive(false);
            homePanel.gameObject.SetActive(false);
            StartCoroutine(LoadHome());
        });
    }

    public void ShowItemGetPanel(int index, int amount)
    {
        itemGetPanel.gameObject.SetActive(true);
        getItemImage.sprite = itemIcons[index];

        switch (index)
        {
            case 0:
                itemText.text = string.Format("You've got Infinite Gas for {0} minutes!", amount);
                isInfiniteGas = true;
                remainingInfiniteGasTime = 60f * amount;

                SaveManager.Instance.Save("CarJam_IsInfiniteGas", isInfiniteGas ? 1 : 0);
                SaveManager.Instance.Save("CarJam_RemainingInfiniteGasTime", remainingInfiniteGasTime);
                break;
            case 1:
                itemText.text = string.Format("You've got {0} coins!", amount);
                GameManager.Instance.IncreaseGold(amount);
                SaveManager.Instance.Save("CarJam_Gold", GameManager.Instance.GetGoldAmount());
                break;
            case 2:
                itemText.text = string.Format("You've got {0} gems!", amount);
                GameManager.Instance.IncreaseGem(amount);
                SaveManager.Instance.Save("CarJam_Gem", GameManager.Instance.GetGemAmount());
                break;
        }

        Initialize();

        DOVirtual.Float(0, 1f, 0.1f, value =>
        {
            itemGetPanel.alpha = value;
        }).OnComplete(() =>
        {
            DOVirtual.Vector3(Vector3.zero, Vector3.one, 1f, value =>
            {
                getItemImage.transform.localScale = value;
            }).SetEase(Ease.OutElastic).OnComplete(itemScaleVariator.StartGrow);
        });
    }

    Coroutine checkInfiniteGasTimeCoroutine = null;
    public IEnumerator CheckInfiniteGasTime()
    {
        int tick = 0;
        while(remainingInfiniteGasTime > 0)
        {
            yield return null;
            remainingInfiniteGasTime -= Time.deltaTime;

            heartText.text = string.Format("{0}:{1}", (int)(remainingInfiniteGasTime / 60), (int)(remainingInfiniteGasTime % 60));

            tick++;
            if(tick > 15)
            {
                SaveManager.Instance.Save("CarJam_RemainingInfiniteGasTime", remainingInfiniteGasTime);
                tick = 0;
            }
        }
        isInfiniteGas = false;
        remainingInfiniteGasTime = 0;

        SaveManager.Instance.Save("CarJam_IsInfiniteGas", isInfiniteGas ? 1 : 0);
        SaveManager.Instance.Save("CarJam_RemainingInfiniteGasTime", remainingInfiniteGasTime);

        heartText.text = GameManager.Instance.GetHeart().ToString();
        checkInfiniteGasTimeCoroutine = null;
    }


    public void IncreaseHeart()
    {
        GameManager.Instance.IncreaseHeart(1);
    }

    Coroutine moveBackgroundCoroutine;
    private bool canStartGame = true;
    public void StartLevel()
    {
        if (canStartGame && isInfiniteGas)
        {
            canStartGame = false;
            loadingPanel.gameObject.SetActive(true);
            loadingBar.fillAmount = 0.01f;
            moveBackgroundCoroutine = StartCoroutine(MoveLoadingPanelBackground());
            DOVirtual.Float(0f, 1f, 1f, value =>
            {
                loadingPanel.alpha = value;
            }).OnComplete(() =>
            {
                worldRotator.StopRotate();
                worldRotator.SetRendererActive(false);
                UIManager.Instance.SetGameUIActive(true);
                homeCamera.SetActive(false);
                homePanel.gameObject.SetActive(false);
                StartCoroutine(LoadScene(GameManager.Instance.GetCurrentLevelThemeName()));
                canStartGame = true;
            });
            return;
        }
        if (canStartGame)
        {
            if (GameManager.Instance.GetHeart() > 0)
            {
                canStartGame = false;
                heart.gameObject.SetActive(true);
                GameManager.Instance.DecreaseHeart();
                UIManager.Instance.SetHintButtonInteractable(true);
                UIManager.Instance.SetRetryButtonInteractable(true);
                UIManager.Instance.SetGoldButtonInteractable(true);
                UIManager.Instance.SetUndoButtonInteractable(true);

                heartText.text = GameManager.Instance.GetHeart().ToString();
                DOVirtual.Vector3(initialHeartPosition, new Vector3(Screen.width / 2f, Screen.height / 2f, 0), 0.3f, value =>
                {
                    heart.transform.position = value;
                }).SetEase(Ease.InOutBounce).OnComplete(() =>
                {
                    DOVirtual.Vector3(Vector3.one, 3 * Vector3.one, 0.5f, value =>
                    {
                        heart.transform.localScale = value;
                    }).SetEase(Ease.OutCubic).OnComplete(() =>
                    {
                        heart.gameObject.SetActive(false);
                        heart.transform.localScale = Vector3.one;


                        DOVirtual.DelayedCall(1f, () =>
                        {
                            loadingPanel.gameObject.SetActive(true);
                            loadingBar.fillAmount = 0;
                            moveBackgroundCoroutine = StartCoroutine(MoveLoadingPanelBackground());
                            DOVirtual.Float(0f, 1f, 1f, value =>
                            {
                                loadingPanel.alpha = value;
                            }).OnComplete(() =>
                            {
                                canStartGame = true;
                                worldRotator.StopRotate();
                                worldRotator.SetRendererActive(false);
                                UIManager.Instance.SetGameUIActive(true);
                                homeCamera.SetActive(false);
                                homePanel.gameObject.SetActive(false);
                                StartCoroutine(LoadScene(GameManager.Instance.GetCurrentLevelThemeName()));
                            });
                        });
                    });
                });
            }
            else
            {
                heartAdvertisPanel.SetActiveWithTween();
            }
        }
    }

    public IEnumerator LoadHome()
    {
        homeCamera.SetActive(true);
        homePanel.gameObject.SetActive(true);

        List<AsyncOperation> operations = new ();
        for(int i = 0; i < loadedScene.Count; i++)
        {
            operations.Add(SceneManager.UnloadSceneAsync(loadedScene[i]));
        }

        float value;
        while(operations.Count > 0)
        {
            yield return null;
            
            value = 0;
            for(int i = 0; i < operations.Count;)
            {
                if (operations[i].isDone)
                {
                    operations.RemoveAt(i);
                    continue;
                }
                value += operations[i].progress;
                i++;
            }
            value /= operations.Count;
            loadingBar.fillAmount = value;
        }

        loadedScene.Clear();

        DOVirtual.Float(1f, 0f, 1f, value =>
        {
            loadingPanel.alpha = value;
        }).OnComplete(() =>
        {
            Initialize();
            loadingPanel.gameObject.SetActive(false);
            StopCoroutine(moveBackgroundCoroutine);
        });
    }

    public IEnumerator LoadScene(string sceneName)
    {
        if (!loadedScene.Contains(sceneName))
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!operation.isDone)
            {
                yield return null;
                loadingBar.fillAmount = operation.progress;
            }
            loadedScene.Add(sceneName);
        } else
        {
            loadingBar.fillAmount = 1f;
        }
        GameManager.Instance.StartGame();
        DOVirtual.Float(1f, 0f, 1f, value =>
        {
            loadingPanel.alpha = value;
        }).OnComplete(() =>
        {
            loadingPanel.gameObject.SetActive(false);
            StopCoroutine(moveBackgroundCoroutine);
        });
    }
    Rect currentRect;
    public IEnumerator MoveLoadingPanelBackground()
    {

        while (true)
        {
            currentRect = loadingBackground.uvRect;
            currentRect.x += Time.deltaTime / 3f;
            currentRect.y += Time.deltaTime / 3f;
            loadingBackground.uvRect = currentRect;
            yield return null;
        }
    }


    public void ShowHeartAdvertise()
    {
        AdManager.Instance.SetRewardType(3); 
        AdManager.Instance.ShowRewardAdvertise();
    }
}
