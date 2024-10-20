using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingLot : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Transform popPosition;

    [Header("Runtime Settings")]
    [SerializeField] private int vehicleTotalCount;
    [SerializeField] private VehicleDirection direction;
    [SerializeField] private Vector2Int posInGrid;
    private int[] vehicleColors;
    private int[] vehicleIndexs;

    private int currentVehicleIndex = 0;

    public void Initialize(int[] vehicleColors, int[] vehicleIndexs, VehicleDirection direction, Vector2Int position)
    {
        vehicleTotalCount = vehicleColors.Length;
        this.vehicleColors = vehicleColors;
        this.vehicleIndexs = vehicleIndexs;
        this.direction = direction;
        posInGrid = position;
        currentVehicleIndex = 0;
    }

    

    public void CheckCanPopVehicle()
    {
        if(currentVehicleIndex < vehicleTotalCount &&
            RoundManager.Instance.IsValidVehicle(posInGrid + Vehicle.GetVehicleDirection(direction), direction, Vehicle.GetVehicleSizeByIndex(vehicleIndexs[currentVehicleIndex])))
        {
            PopVehicle();
        }
    }

    public void PopVehicle()
    {
        Vehicle vehicle = RoundManager.Instance.SpawnVehicle(posInGrid + Vehicle.GetVehicleDirection(direction), direction, vehicleColors[currentVehicleIndex], vehicleIndexs[currentVehicleIndex]);

    }
}
