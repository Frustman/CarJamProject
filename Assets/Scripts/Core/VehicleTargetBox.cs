using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleTargetBox : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private int targetPriority;



    private VehicleCollider vehicle;
    private void OnTriggerEnter(Collider other)
    {
        vehicle = null;
        other.TryGetComponent(out vehicle);

        if (vehicle && vehicle.vehicle.isPicked)
        {
            vehicle.vehicle.SetTarget(target, targetPriority);
        }
    }
}
