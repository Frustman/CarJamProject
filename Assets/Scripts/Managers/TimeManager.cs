using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    public float playTime;

    const string RequestURL = "https://worldtimeapi.org/api/ip";
    DateTime currentDateTime;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator GetDateTime(Action<DateTime> callback)
    {
        yield return StartCoroutine(GetRealDateTimeFromAPI());
        callback(currentDateTime);
    }

    public IEnumerator GetRealDateTimeFromAPI()
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(RequestURL);
        yield return webRequest.SendWebRequest();

        if(webRequest.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string timeData = webRequest.downloadHandler.text;
                currentDateTime = ParseDateTime(timeData);
            }
            catch (Exception)
            {
            }
        } else
        {
            currentDateTime = DateTime.Now;
        }
    }

    DateTime ParseDateTime(string dateTime)
    {
        string date = Regex.Match(dateTime, @"^\d{4}-\d{2}-\d{2}").Value;
        string time = Regex.Match(dateTime, @"\d{2}:\d{2}:\d{2}").Value;
        return DateTime.Parse(string.Format("{0} {1}", date, time));
    }



    private int tick = 0;
    public void Update()
    {
        playTime += Time.deltaTime;
        tick++;
        if (tick > 10)
        {
            tick = 0;
            SaveManager.Instance.Save("CarJam_PlayTime", playTime / 60f);
        }
    }
}
