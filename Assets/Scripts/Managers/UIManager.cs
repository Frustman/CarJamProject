using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Dependency Settings")]
    [SerializeField] private PickVehicle pickVehicle;


    [Header("Component Settings")]
    [SerializeField] private GameObject uiCanvas;
    [Space(5f)]
    [SerializeField] private DialogueUI defeatPanel;
    [SerializeField] private DialogueUI winPanel;
    [SerializeField] private DialogueUI advertisePanel;
    [SerializeField] private DialogueUI hintGoBackPanel;
    [SerializeField] private DialogueUI goBackAdvertisePanel;
    [SerializeField] private DialogueUI restartAdvertisePanel;
    [SerializeField] private DialogueUI hintAdvertisePanel;
    [SerializeField] private GameObject ItemPanel;
    [SerializeField] private StarUI starPanel;
    [Space(5f)]
    [SerializeField] private PopUpDialogueUI dialogueMessage;
    [Space(5f)]
    [SerializeField] private GameObject checkImage;
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI goldLabel;
    [SerializeField] private GameObject goldFrame;
    [Space(5f)]
    [SerializeField] private TextMeshProUGUI defeatGoldLabel;
    [SerializeField] private TextMeshProUGUI defeatGasLabel;

    [Space(10f)]
    [SerializeField] private Button retryAdvertiseButton;
    [SerializeField] private Button hintAdvertiseButton;
    [SerializeField] private Button goldAdvertiseButton;
    [Space(10f)]
    [SerializeField] private Image hintButton;



    public void Awake()
    {
        Instance = this;
    }


    public void ShowHintAdvertisePanel()
    {
        pickVehicle.SetCanPick(false);
        GameManager.Instance.SetGameState(false);
        hintAdvertisePanel.SetActiveWithTween();
    }
    public void ShowAdvertisePanel()
    {
        pickVehicle.SetCanPick(false);
        GameManager.Instance.SetGameState(false);
        advertisePanel.SetActiveWithTween();
    }
    public void ShowGoBackAdvertisePanel()
    {
        pickVehicle.SetCanPick(false);
        GameManager.Instance.SetGameState(false);
        goBackAdvertisePanel.SetActiveWithTween();
    }
    public void ShowRestartAdvertisePanel()
    {
        pickVehicle.SetCanPick(false);
        GameManager.Instance.SetGameState(false);
        restartAdvertisePanel.SetActiveWithTween();
    }
    public void ShowHintGoBackPanel()
    {
        pickVehicle.SetCanPick(false);
        GameManager.Instance.SetGameState(false);
        hintGoBackPanel.SetActiveWithTween();
    }

    public void ShowAdvertise()
    {
        AdManager.Instance.ShowRewardAdvertise();
    }

    public void CheckAnimation()
    {
        checkImage.SetActive(true);
        DOVirtual.Vector3(new Vector3(0.3f, 0.3f, 0.3f), Vector3.one, 1f, value =>
        {
            checkImage.transform.localScale = value;
        }).SetEase(Ease.OutElastic);

        DOVirtual.DelayedCall(1.5f, () =>
        {
            DOVirtual.Vector3(Vector3.one, Vector3.zero, 1f, value =>
            {
                checkImage.transform.localScale = value;
            }).SetEase(Ease.OutCubic).OnComplete(GameManager.Instance.GoToNextDifficulty);
        });
    }

    public void SetCheckImageActive(bool state)
    {
        checkImage.SetActive(state);
    }


    public void SetRetryButtonInteractable(bool state)
    {
        retryAdvertiseButton.interactable = state;
    }
    public void SetHintButtonInteractable(bool state)
    {
        hintAdvertiseButton.interactable = state;
    }
    public void SetGoldButtonInteractable(bool state)
    {
        goldAdvertiseButton.interactable = state;
    }
    public void ShowDialogueMessage(string message)
    {
        dialogueMessage.Show(message);
    }

    public void ShowDefeatPanel()
    {
        defeatPanel.SetActiveWithTween();
        ItemPanel.SetActive(false);
    }

    public void ShowWinPanel()
    {
        winPanel.SetActiveWithTween();
        ItemPanel.SetActive(false);
        starPanel.StartAnimation(3);
    }

    public void RefreshUI()
    {
        goldLabel.text = GameManager.Instance.GetGoldAmount().ToString();
        levelLabel.text = string.Format("Level {0}", GameManager.Instance.GetStageLevel());
        defeatGoldLabel.text = GameManager.Instance.GetGoldAmount().ToString();
        if (HomeManager.Instance && !HomeManager.Instance.isInfiniteGas) defeatGasLabel.text = GameManager.Instance.GetHeart().ToString();
    }

    public void SetGameUIActive(bool active)
    {
        uiCanvas.SetActive(active);
    }


    private DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> scaleTween;
    public void GoldGetAnimation()
    {
        goldFrame.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        DOVirtual.DelayedCall(0.05f, () =>
        {
            if (scaleTween != null) scaleTween.Kill(false);
            scaleTween = goldFrame.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutElastic).OnComplete(() =>
            {
                scaleTween = null;
            });
        });
    }

    public void RefreshHintUI()
    {
        int poppedVehicleCount = RoundManager.Instance.GetPoppedVehicleCount();
        if (poppedVehicleCount <= 10)
        {
            hintButton.fillAmount = poppedVehicleCount / 10f;
        } else
        {
            hintButton.fillAmount = 1;
        }
    }
}
