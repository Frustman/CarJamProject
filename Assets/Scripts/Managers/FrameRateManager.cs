using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class FrameRateManager : MonoBehaviour
{
    public static FrameRateManager Instance;

    public string input;
    private float elapsedSlowDownTime;
    private bool hasSlowDownOccured;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
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

    [ContextMenu("Check")]
    public void Check()
    {

    }



    /*
    private void Update()
    {
        elapsedSlowDownTime += Time.deltaTime;

        if (!hasSlowDownOccured && elapsedSlowDownTime > 10f)
        {
            hasSlowDownOccured = true;
            SetRenderingFullFps(false);0
        }
    }*/



}
