using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class WorldRotator : MonoBehaviour
{
    [Header("Prefab Settings")]
    public bool isRotating = true;

    private GameObject currentTheme;
    private int currentThemeIndex = -1;
    [SerializeField] private Transform themeParent;
    [SerializeField] private List<GameObject> themes = new List<GameObject>();

    private CancellationTokenSource rotateToken = new();

    public void Start()
    {
        SetTheme(1);
    }

    public void SetRendererActive(bool state)
    {
        themeParent.gameObject.SetActive(state);
    }

    public void OnApplicationQuit()
    {
        rotateToken.Cancel();
    }

    private UniTask rotateTask;
    public void StartRotate()
    {
        isRotating = true;
        if (rotateTask.Status != UniTaskStatus.Pending)
        {
            rotateTask = Rotate(rotateToken.Token);
        }
    }

    public void StopRotate()
    {
        isRotating = false;
    }

    private async UniTask Rotate(CancellationToken token)
    {
        while (isRotating)
        {
            if (token.IsCancellationRequested) return;
            transform.Rotate(Vector3.up, Time.deltaTime * 4f);
            await UniTask.Yield();
        }
    }


    public void SetTheme(int index)
    {
        if (currentThemeIndex != index && index < themes.Count)
        {
            Destroy(currentTheme);
            currentTheme = Instantiate(themes[index], themeParent);
            currentThemeIndex = index;
        }
    }
}
