using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FrameRateManager : MonoBehaviour
{
    public static FrameRateManager Instance;

    private float elapsedSlowDownTime;
    private bool hasSlowDownOccured;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        OnDemandRendering.renderFrameInterval = 1;

        elapsedSlowDownTime = 0;
        hasSlowDownOccured = false;
    }

    public void SetRenderingFullFps(bool state)
    {
        if (state)
        {
            hasSlowDownOccured = false;
            elapsedSlowDownTime = 0;
            OnDemandRendering.renderFrameInterval = 1;
        } else
        {
            OnDemandRendering.renderFrameInterval = 1;
        }
    }
    private void Update()
    {
        elapsedSlowDownTime += Time.deltaTime;

        if (!hasSlowDownOccured && elapsedSlowDownTime > 10f)
        {
            hasSlowDownOccured = true;
            SetRenderingFullFps(false);
        }
    }
}
