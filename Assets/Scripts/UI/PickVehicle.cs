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
    private Vehicle currentVehicle;
    private bool canPick = true;


    public void SetCanPick(bool state)
    {
        canPick = state;
    }


    public void Pick(Vector2 position)
    {
        if (canPick)
        {
            ray = mainCamera.ScreenPointToRay(position);

            if (Physics.Raycast(ray, out hit, layerMask))
            {
                hit.transform.TryGetComponent(out currentVehicle);

                if (currentVehicle)
                {
                    currentVehicle.Pick();
                    TraceManager.Instance.RemoveAnswer();
                    InputManager.Instance.InvokeVibrate(1);
                }
            }
        }
    }
}
