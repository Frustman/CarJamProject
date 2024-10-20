using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class VehicleSpace : MonoBehaviour
{
    [Header("Prefab Settings")]

    [SerializeField] private List<GameObject> customerPrefabs = new();
    [SerializeField] private List<GameObject> sedans = new();
    [SerializeField] private List<GameObject> miniBuses = new();
    [SerializeField] private List<GameObject> Buses = new();
    [Space(10f)]

    [SerializeField] private CustomerSpawner spawner;
    [SerializeField] private Transform outPosition;

    [SerializeField] private List<Transform> parkingSpace = new List<Transform>();
    [SerializeField] private List<Transform> inSpace = new List<Transform>();
    [SerializeField] private List<GameObject> lockCanvas = new List<GameObject> ();
    [SerializeField] private List<GameObject> blinkers = new List<GameObject> ();
    [SerializeField] private LevelData levelData;

    private int[] availableSeats;
    [SerializeField] private Vehicle[] movingVehicles;

    private Queue<Seat>[] seatTransforms;
    [SerializeField] private Vehicle[] parkedVehicles;

    [SerializeField] private int availableSpaceCount;

    public int movingCarCount = 0;
    public int parkedCarCount = 0;
    private int colorCount;

    Tweener[] blinkerTweener = new Tweener[7];

    public void Start()
    {
        for (int i = 0; i < customerPrefabs.Count; i++)
        {
            PoolManager.Instance.AssignPoolingObject(customerPrefabs[i]);
        }
        for (int i = 0; i < sedans.Count; i++)
        {
            PoolManager.Instance.AssignPoolingObject(sedans[i]);
        }
        for (int i = 0; i < miniBuses.Count; i++)
        {
            PoolManager.Instance.AssignPoolingObject(miniBuses[i]);
        }
        for (int i = 0; i < Buses.Count; i++)
        {
            PoolManager.Instance.AssignPoolingObject(Buses[i]);
        }
    }

    public void Initialize(LevelData level)
    {
        parkedVehicles = new Vehicle[parkingSpace.Count];
        levelData = level;
        availableSeats = new int[level.colorCount + 1];                                                            // Counts of color
        seatTransforms = new Queue<Seat>[level.colorCount + 1];                                                    // Counts of Color
        movingVehicles = new Vehicle[parkingSpace.Count];
        availableSpaceCount = level.availableVehicleSpace;
        movingCarCount = 0;
        parkedCarCount = 0;
        colorCount = level.colorCount;

        for (int i = 0; i < availableSeats.Length; i++)
        {
            availableSeats[i] = 0;
            seatTransforms[i] = new Queue<Seat>();
        }
        
        for(int i = 0; i < parkingSpace.Count; i++)
        {
            if(i < availableSpaceCount)
            {
                int currentIdx = i;

                if (blinkerTweener[currentIdx] != null) blinkerTweener[currentIdx].Kill(false);
                blinkerTweener[currentIdx] = DOVirtual.Vector3(new Vector3(-300f, 0, 0), new Vector3(300f, 0, 0), 1f, value =>
                {
                    blinkers[currentIdx].transform.localPosition = value;
                }).OnComplete(() =>
                {
                    blinkerTweener[currentIdx] = null;
                });
            }
            lockCanvas[i].gameObject.SetActive(i >= availableSpaceCount);
        }
    }
    private int Max(int a, int b)
    {
        if (a > b) return a;
        else return b;
    }

    public int[] GetAvailableSeats()
    {
        int[] newAvailable = new int[availableSeats.Length];
        for(int i = 0; i < availableSeats.Length; i++)
        {
            newAvailable[i] = availableSeats[i];
        }
        return newAvailable;
    }


    public bool CanParkVehicle()
    {
        if (parkedCarCount >= availableSpaceCount)
            return false;
        return true;
    }

    public void Park(Vehicle vehicle)
    {
        for(int i = 0; i < availableSpaceCount; i++)
        {
            if (parkedVehicles[i] != null) continue;
            parkedVehicles[i] = vehicle;
            parkedCarCount++;
            TraceManager.Instance.PickVehicle(vehicle, i);
            return;
        }
    }

    public void CheckCustomerState(bool canDefeat)
    { 
        spawner.StartPopCustomer(availableSeats, canDefeat).Forget();
    }

    public void NotifyChange(int[] seatDiff)
    {
        for(int i = 0; i < seatDiff.Length; i++)
        {
            availableSeats[i] += seatDiff[i];
        }

    }

    public void OnApplicationQuit()
    {
        source.Cancel();
    }

    private CancellationTokenSource source = new();
    private UniTask checkDefeatTask;
    public void CheckIfDefeat()
    {
        if(checkDefeatTask.Status == UniTaskStatus.Pending) source.Cancel();
        checkDefeatTask = CheckDefeat();
    }


    private async UniTask CheckDefeat()
    {
        bool flag = false;
        for(int i = 0; i < 3; i++)
        {
            if (availableSpaceCount == parkedCarCount && movingCarCount == 0)
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: source.Token);
            else
            {
                flag = true;
                break;
            }
        }
        if(!flag)
            GameManager.Instance.Defeat();
    }

    public Seat GetNextSeat(int color)
    {
        return seatTransforms[color].Dequeue();
    }

    public void PopVehicle(Vehicle vehicle)
    {
        for(int i = 0; i < availableSpaceCount; i++)
        {
            if (parkedVehicles[i] && parkedVehicles[i].Equals(vehicle))
            {
                parkedVehicles[i].target = null;
                parkedVehicles[i].GoForward(1f);
                parkedVehicles[i] = null;
                parkedCarCount--;
                break;
            }
        }
    }

    private bool isVehicleSpaceUnlocked;
    private int unlockedPickCount;
    public void UnlockVehicleSpace()
    {
        if (isVehicleSpaceUnlocked)
        {
            UIManager.Instance.ShowDialogueMessage("Parking space is already unlocked!");
            return;
        }
        if(GameManager.Instance.GetGoldAmount() < 200)
        {
            UIManager.Instance.ShowAdvertisePanel();
            return;
        }

        isVehicleSpaceUnlocked = true;
        unlockedPickCount = RoundManager.Instance.pickCount;
        availableSpaceCount++;
        GameManager.Instance.DecreaseGold(200);
        for (int i = 0; i < parkingSpace.Count; i++)
        {
            lockCanvas[i].gameObject.SetActive(i >= availableSpaceCount);
        }

        UIManager.Instance.ShowDialogueMessage("One more parking space has opened up!");
    }


    private void LockVehicleSpace()
    {
        isVehicleSpaceUnlocked = true;
        unlockedPickCount = RoundManager.Instance.pickCount;
        availableSpaceCount--;
        for (int i = 0; i < parkingSpace.Count; i++)
        {
            lockCanvas[i].gameObject.SetActive(i >= availableSpaceCount);
        }

        UIManager.Instance.ShowDialogueMessage("Temporal parking space has gone!");
    }

    public void CheckVehicleSpaceToLock()
    {
        if(RoundManager.Instance.pickCount > unlockedPickCount + 5)
        {
            LockVehicleSpace();
        }
    }

    GameObject currentPrefab;

    public void SetParkedVehicles(int[] ridingCustomers, VehicleLog[] parkedVehicles)
    {
        for(int i = 0; i < this.parkedVehicles.Length; i++)
        {
            if (this.parkedVehicles[i] == null) continue;
            for(int j = 0; j < this.parkedVehicles[i].ridingCustomers.Count; j++)
            {
                PoolManager.Instance.Put(this.parkedVehicles[i].ridingCustomers[j].poolIndex, this.parkedVehicles[i].ridingCustomers[j].gameObject);
            }
            PoolManager.Instance.Put(this.parkedVehicles[i].poolIndex, this.parkedVehicles[i].gameObject);
            this.parkedVehicles[i] = null;
        }

        seatTransforms = new Queue<Seat>[colorCount + 1];
        availableSeats = new int[colorCount + 1];

        for(int i = 0; i < seatTransforms.Length; i++)
        {
            seatTransforms[i] = new Queue<Seat>();
            availableSeats[i] = 0;
        }

        int count = 0;
        for(int i = 0; i < parkedVehicles.Length; i++)
        {
            if (parkedVehicles[i] != null) count++;
        }
        parkedCarCount = count;
        movingCarCount = 0;

        for(int i = 0; i < parkedVehicles.Length; i++)
        {
            if (parkedVehicles[i] == null || ridingCustomers[i] == 0) continue;

            if (parkedVehicles[i].vehicleIndex == 0)
            {
                currentPrefab = sedans[parkedVehicles[i].colorIndex];
            }
            else if (parkedVehicles[i].vehicleIndex == 1)
            {
                currentPrefab = miniBuses[parkedVehicles[i].colorIndex];
            }
            else
            {
                currentPrefab = Buses[parkedVehicles[i].colorIndex];
            }
            Vehicle vehicle = PoolManager.Instance.Get(currentPrefab).GetComponent<Vehicle>();
            vehicle.gameObject.SetActive(true);
            vehicle.Initialize(this, parkedVehicles[i].direction, parkedVehicles[i].posInGrid, parkedVehicles[i].colorIndex, PoolManager.Instance.GetPoolIndex(currentPrefab));
            vehicle.transform.position = parkingSpace[i].transform.position;
            vehicle.transform.rotation = parkingSpace[i].transform.rotation;
            vehicle.SetWaitingState();
            vehicle.isPicked = true;
            vehicle.isParked = true;


            this.parkedVehicles[i] = vehicle;
            Seat[] seats = vehicle.GetSeatTransforms();
            for(int j = 0; j < seats.Length; j++)
            {
                if (j < ridingCustomers[i])
                {
                    Customer customer = PoolManager.Instance.Get(customerPrefabs[parkedVehicles[i].colorIndex]).GetComponent<Customer>();
                    customer.gameObject.SetActive(true);
                    customer.transform.position = seats[j].transform.position;
                    customer.transform.rotation = Quaternion.Euler(0, 45f, 0);
                    customer.GoToSeat(seats[j]);
                    vehicle.Ride(customer);
                    customer.isSeating = true;
                }
                else
                {
                    availableSeats[parkedVehicles[i].colorIndex]++;
                    seatTransforms[parkedVehicles[i].colorIndex].Enqueue(seats[j]);
                }
            }
        }

        for (int i = 0; i < parkedVehicles.Length; i++)
        {
            if (parkedVehicles[i] == null || ridingCustomers[i] > 0) continue;

            if (parkedVehicles[i].vehicleIndex == 0)
            {
                currentPrefab = sedans[parkedVehicles[i].colorIndex];
            }
            else if (parkedVehicles[i].vehicleIndex == 1)
            {
                currentPrefab = miniBuses[parkedVehicles[i].colorIndex];
            }
            else
            {
                currentPrefab = Buses[parkedVehicles[i].colorIndex];
            }
            Vehicle vehicle = PoolManager.Instance.Get(currentPrefab).GetComponent<Vehicle>();
            vehicle.Initialize(this, parkedVehicles[i].direction, parkedVehicles[i].posInGrid, parkedVehicles[i].colorIndex, PoolManager.Instance.GetPoolIndex(currentPrefab));
            vehicle.transform.position = parkingSpace[i].transform.position;
            vehicle.transform.rotation = parkingSpace[i].transform.rotation;
            vehicle.SetWaitingState();
            vehicle.isPicked = true;
            vehicle.isParked = true;

            this.parkedVehicles[i] = vehicle;
            foreach (Seat seat in vehicle.GetSeatTransforms())
            {
                seatTransforms[parkedVehicles[i].colorIndex].Enqueue(seat);
                availableSeats[parkedVehicles[i].colorIndex]++;
            }
        }
    }






    private VehicleCollider vehicle;
    private void OnTriggerEnter(Collider other)
    {
        vehicle = null;
        other.TryGetComponent(out vehicle);

        if (vehicle && vehicle.vehicle.isPicked && !vehicle.vehicle.isParked)
        {
            for(int i = 0; i < availableSpaceCount; i++)
            {
                if (vehicle.vehicle.Equals(parkedVehicles[i]))
                {
                    movingVehicles[i] = vehicle.vehicle;
                        
                    movingVehicles[i].SetTarget(inSpace[i], 100, () =>
                    {
                        movingVehicles[i].SetTarget(parkingSpace[i], 1000, () =>
                        {
                            availableSeats[movingVehicles[i].GetVehicleColorIndex()] += movingVehicles[i].GetSeatCount();
                            Seat[] seats = movingVehicles[i].GetSeatTransforms();
                            for (int j = 0; j < seats.Length; j++)
                            {
                                seatTransforms[(int)movingVehicles[i].GetVehicleColorIndex()].Enqueue(seats[j]);
                            }
                            movingVehicles[i].SetParticleState(false);
                            movingVehicles[i].isParked = true;
                            movingVehicles[i] = null;
                            movingCarCount--;
                            CheckCustomerState(movingCarCount == 0);
                        });
                    });
                    return;
                }
            }
        }
    }
}
