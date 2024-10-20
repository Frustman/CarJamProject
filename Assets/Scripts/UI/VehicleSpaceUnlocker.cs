using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpaceUnlocker : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private VehicleSpace vehicleSpace;

    public void UnlockVehicleSpace()
    {
        if(GameManager.Instance.GetGoldAmount() < 200)
        {

        }
    }
}
