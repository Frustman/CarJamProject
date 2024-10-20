using System.Collections.Generic;
using TMPro;
using CandyCoded.HapticFeedback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    [Header("Prefab Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] public EventSystem eventSystem;
    [SerializeField] private PickVehicle pickVehicle;
    [SerializeField] private ParticleSystem ripple;

    private InputAction touchPositionAction;
    private InputAction touchPressAction;

    [SerializeField] private Image vibrateCheck;
    public bool isVibrateOn = true;
    private void Awake()
    {
        Instance = this;
        touchPressAction = playerInput.actions["TouchPress"];
        touchPositionAction = playerInput.actions["TouchPosition"];
        isVibrateOn = PlayerPrefs.GetInt("CarJam_VibrateOn") == 1;
        vibrateCheck.gameObject.SetActive(isVibrateOn);
    }
    public static bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.layer == 5 && results[i].gameObject.activeInHierarchy) //5 = UI layer
            {

                return true;
            }
        }

        return false;
    }
    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TutorialManager.Instance.LoadNextPage();
            if (!IsPointerOverUIObject())
            {
                pickVehicle.Pick(Input.mousePosition);
                ripple.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane + 1f));
                ripple.Play();
            }
            FrameRateManager.Instance.SetRenderingFullFps(true);
        }
    }

    public void SetVibrateToggle()
    {
        if (isVibrateOn)
        {
            isVibrateOn = false;
            PlayerPrefs.SetInt("CarJam_VibrateOn", 0);
        } else
        {
            isVibrateOn = true;
            PlayerPrefs.SetInt("CarJam_VibrateOn", 1);
        }
        vibrateCheck.gameObject.SetActive(isVibrateOn);
    }

    public bool SetHomeVibrateToggle()
    {
        if (isVibrateOn)
        {
            isVibrateOn = false;
            PlayerPrefs.SetInt("CarJam_VibrateOn", 0);
        }
        else
        {
            isVibrateOn = true;
            PlayerPrefs.SetInt("CarJam_VibrateOn", 1);
        }
        return isVibrateOn;
    }

    public void InvokeVibrate(int vibrateMode)
    {
        if (isVibrateOn)
        {
            if (vibrateMode == 0)
            {
                HapticFeedback.LightFeedback();
            }
            else if (vibrateMode == 1)
            {
                HapticFeedback.MediumFeedback();
            }
            else if (vibrateMode == 2)
            {
                HapticFeedback.HeavyFeedback();
            }
        }
    }
}
