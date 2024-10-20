using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderingArea : MonoBehaviour
{
    private float canUndoTimer;
    private bool canUndoTogger = false;

    public void Update()
    {
        if(canUndoTimer > 2f)
        {
            if (canUndoTogger)
            {
                canUndoTogger = false;
                TraceManager.Instance.canUndo = true;
            }
        } else
        {
            canUndoTimer += Time.deltaTime;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        other.TryGetComponent(out Customer customer);
        other.TryGetComponent(out Vehicle vehicle);

        if (customer)
        {
            canUndoTimer = 0;
            canUndoTogger = true;
            PoolManager.Instance.Put(customer.poolIndex, customer.gameObject);
        }
        if (vehicle)
        {
            canUndoTimer = 0;
            canUndoTogger = true;
            PoolManager.Instance.Put(vehicle.poolIndex, vehicle.gameObject);
        }
    }
}
