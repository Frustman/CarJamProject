using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CustomBuildPlayer : MonoBehaviour
{
    [MenuItem("Build/Build Android")]
    public static void BuildAndroid()
    {

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MainScene.unity", "Assets/Scenes/HomeScene.unity", "Assets/Scenes/Thema_City.unity" };
        buildPlayerOptions.locationPathName = "BUILD/KYH_CarJam.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;


        if(summary.result == BuildResult.Succeeded)
        {
            Debug.LogFormat("Build Succeeded : {0} bytes", summary.totalSize);
        } else if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build Failed");
        }
    }


    [MenuItem("Build/Build IOS")]
    public static void BuildIOS()
    {

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MainScene.unity", "Assets/Scenes/HomeScene.unity", "Assets/Scenes/Thema_City.unity" };
        buildPlayerOptions.locationPathName = "BUILD/ZDEV_KYH_iOS/";
        buildPlayerOptions.target = BuildTarget.iOS;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;


        if (summary.result == BuildResult.Succeeded)
        {
            Debug.LogFormat("Build Succeeded : {0} bytes", summary.totalSize);
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build Failed");
        }
    }
}
