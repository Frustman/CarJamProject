using DG.Tweening;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    [Header("Prefab Settings")]
    [SerializeField] private GameObject[] pages;
    [SerializeField] private GameObject[] circles;

    [SerializeField] private Transform[] circlePositions;

    public bool inTutorial = false;
    private int currentPage = 0;

    public void Awake()
    {
        Instance = this;
    }

    public void StartTutorial()
    {
        currentPage = 0;
        DOVirtual.DelayedCall(1f, () =>
        {
            inTutorial = true;
            pages[0].SetActive(true);
            circles[0].transform.position = Camera.main.WorldToScreenPoint(circlePositions[0].position);
            circles[0].SetActive(true);
        });
    }

    public void FinishTutorial()
    {
        inTutorial = false;
        currentPage = 0;
        for(int i = 0;i < pages.Length; i++)
        {
            pages[i].SetActive(false);
        }
    }

    public void LoadPage(int index)
    {
        switch (index)
        {
            case 1:
                pages[0].SetActive(false);
                pages[1].SetActive(true);
                circles[1].SetActive(true);
                circles[1].transform.position = Camera.main.WorldToScreenPoint(circlePositions[1].position);
                break;
            case 2:
                pages[1].SetActive(false);
                pages[2].SetActive(true);
                circles[2].SetActive(true);
                circles[2].transform.position = Camera.main.WorldToScreenPoint(circlePositions[2].position);
                break;
            case 3:
                FinishTutorial();
                break;
            default:
                break;
        }
    }

    public void LoadNextPage() {

        if (inTutorial)
        {
            if(currentPage < pages.Length - 1)
            {
                currentPage++;
                LoadPage(currentPage);
            } else
            {
                FinishTutorial();
            }
        }
    }
}
