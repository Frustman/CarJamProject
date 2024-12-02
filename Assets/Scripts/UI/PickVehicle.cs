using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PickVehicle : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private LayerMask layerMask;

    Ray ray;
    RaycastHit hit;
    [SerializeField] private bool canPick = true;


    public void SetCanPick(bool state)
    {
        canPick = state;
    }


    public void Pick(Vector2 position)
    {
        if (canPick)
        {

            ray = mainCamera.ScreenPointToRay(position);

            if (Physics.Raycast(ray, out hit, 1000f, layerMask))
            {
                hit.transform.TryGetComponent(out Vehicle currentVehicle);
                //Debug.Log(hit.transform.name);

                if (currentVehicle)
                {
                    currentVehicle.Pick();
                    TraceManager.Instance.RemoveAnswer();
                    //InputManager.Instance.InvokeVibrate(1);
                    UIManager.Instance.RefreshHintUI();
                }
            }
        }
    }
}
