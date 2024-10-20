using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Runtime Settings")]
    [SerializeField] CustomerSpawner spawner;
    [SerializeField] private int colorIndex;

    [SerializeField] private Transform target;

    public int poolIndex;

    private Seat currentSeat;

    private bool isMoving;
    public bool isSeating;

    public void Initialize(CustomerSpawner spawner, int colorIndex, int poolIndex)
    {
        this.spawner = spawner;
        this.colorIndex = colorIndex;
        this.poolIndex = poolIndex;
        isMoving = false;
        isSeating = false;
        target = null;
    }

    public int GetPersonColor()
    {
        return colorIndex;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }


    public void GoToSeat(Seat seat)
    {
        currentSeat = seat;
        this.target = seat.transform;
    }

    private void Update()
    {
        if (isSeating)
        {
            transform.position = currentSeat.transform.position;
            return;
        }
        if(!isMoving && target && Vector3.Distance(target.position, transform.position) > 0.5f)
        {
            isMoving = true;
            transform.DOMove(target.position, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
            {
                isMoving = false;
                if (currentSeat && Vector3.Distance(currentSeat.transform.position, transform.position) < 1f)
                {
                    currentSeat.SelectCustomer(this);
                    spawner.canPopCustomer = true;
                    isSeating = true;
                }
            });
        }
    }

}
