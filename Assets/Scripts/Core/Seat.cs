using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seat : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private Vehicle vehicle;

    public void SelectCustomer(Customer customer)
    {
        vehicle.Ride(customer);
    }
}
