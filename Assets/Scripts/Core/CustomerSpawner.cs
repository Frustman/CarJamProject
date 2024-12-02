using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();
    [SerializeField] private VehicleSpace vehicleSpace;
    [SerializeField] private TraceManager traceManager;
    [SerializeField] private TextMeshProUGUI customerCounterTMP;
    [SerializeField] private Transform spawnPosition;

    [Header("Parameter Settings")]
    [SerializeField] private LevelData levelData;
    [SerializeField] private int seed;

    [Header("Seat Settings")]
    [SerializeField] private List<Transform> waitingSeats = new List<Transform>();

    [SerializeField] private List<Customer> waitingCustomers;

    public bool needToSpawnCustomer;
    private System.Random random;
    [SerializeField] private int customerCount = 0;
    [SerializeField] private int currentCustomerIndex;
    [SerializeField] private int[] customerLine;

    private Coroutine popCoroutine;

    public int poppedCustomerCount = 0;
    [HideInInspector] public bool canPopCustomer = true;

    public void Start()
    {
        random = new System.Random(seed);

        waitingSeats.Reverse();

    }

    private UniTask spawnCustomerTask;
    private UniTask popCustomerTask;

    private CancellationTokenSource spawnCustomerToken = new();
    private CancellationTokenSource popCustomerToken = new();

    public void StartLevel(LevelData level)
    {
        levelData = level;
        seed = level.seed;
        random = new System.Random(seed);

        customerCounterTMP.text = level.totalCustomerCount.ToString();

        currentCustomerIndex = 0;
        customerCount = 0;
        poppedCustomerCount = 0;
        needToSpawnCustomer = true;

        spawnCustomerToken = new();
        popCustomerToken = new();

        spawnCustomerTask = SpawnCustomer(spawnCustomerToken.Token);
        spawnCustomerTask.Forget();
        traceManager.Initialize(customerLine, level.availableVehicleSpace, level.colorCount, level.gridSize / 2, level.answer);
    }
    private void OnApplicationQuit()
    {
        CancelTasks();
    }

    public void CancelTasks()
    {
        spawnCustomerToken?.Cancel();
        popCustomerToken?.Cancel();

        spawnCustomerToken?.Dispose();
        popCustomerToken?.Dispose();
    }
    

    public async UniTask SpawnCustomer(CancellationToken token)
    {
        waitingCustomers = new List<Customer>();
        customerLine = levelData.customerColorLine;
        bool start = true;
        currentCustomerIndex = 0;

        while (needToSpawnCustomer)
        {
            if (token.IsCancellationRequested) return;
            if (currentCustomerIndex >= customerLine.Length)
            {
                if (needToSpawnCustomer == false) return;
                await UniTask.Yield();
                continue;
            }

            if (customerCount < waitingSeats.Count)
            {
                customerCount++;

                Customer customer = PoolManager.Instance.Get(customerPrefabs[customerLine[currentCustomerIndex]]).GetComponent<Customer>();
                if (waitingCustomers.Count >= waitingSeats.Count)
                {

                    customer.transform.position = spawnPosition.position;
                    customer.transform.rotation = spawnPosition.rotation;
                }
                else
                {
                    customer.transform.position = waitingSeats[waitingCustomers.Count].position;
                    customer.transform.rotation = waitingSeats[waitingCustomers.Count].rotation;
                }

                customer.Initialize(this, customerLine[currentCustomerIndex], PoolManager.Instance.GetPoolIndex(customerPrefabs[customerLine[currentCustomerIndex]]));
                customer.SetTarget(waitingSeats[waitingCustomers.Count]);

                currentCustomerIndex++;
                waitingCustomers.Add(customer);
            }

            while (customerCount >= waitingSeats.Count)
            {
                if (token.IsCancellationRequested) return;
                start = false;
                while (!GameManager.Instance.isGameRunning)
                {
                    if (token.IsCancellationRequested) return;
                    if (needToSpawnCustomer == false) return;
                    await UniTask.Yield();
                }
                if (needToSpawnCustomer == false) return;
                await UniTask.Yield();
            }
            if (start) continue;
            await UniTask.Delay(TimeSpan.FromSeconds(0.005f));
        }

        Debug.Log("Finish Spawn");

    }

    int[] newCustomerLine;
    public void ChangeCustomerIndex(int customerIndex)
    {
        newCustomerLine = new int[poppedCustomerCount - customerIndex];
        for (int i = customerIndex; i < poppedCustomerCount; i++)
        {
            newCustomerLine[i - customerIndex] = customerLine[i];
        }

        int initialCount = waitingCustomers.Count;
        if (waitingSeats.Count - waitingCustomers.Count < poppedCustomerCount - customerIndex)
        {
            int customerDiff = poppedCustomerCount - customerIndex - (waitingSeats.Count - waitingCustomers.Count);
            for (int i = initialCount - 1; i >= initialCount - customerDiff; i--)
            {
                PoolManager.Instance.Put(waitingCustomers[i].poolIndex, waitingCustomers[i].gameObject);
                waitingCustomers.RemoveAt(i);
            }


            for (int i = newCustomerLine.Length - 1; i >= 0; i--)
            {
                Customer customer = PoolManager.Instance.Get(customerPrefabs[newCustomerLine[i]]).GetComponent<Customer>();
                customer.Initialize(this, newCustomerLine[i], PoolManager.Instance.GetPoolIndex(customerPrefabs[newCustomerLine[i]]));
                waitingCustomers.Insert(0, customer);
                customer.transform.rotation = waitingSeats[0].rotation;
            }

            customerCount = waitingCustomers.Count;
            currentCustomerIndex -= customerDiff;
            poppedCustomerCount = customerIndex;
        }
        else
        {
            for (int i = newCustomerLine.Length - 1; i >= 0; i--)
            {
                Customer customer = PoolManager.Instance.Get(customerPrefabs[newCustomerLine[i]]).GetComponent<Customer>();
                customer.Initialize(this, newCustomerLine[i], PoolManager.Instance.GetPoolIndex(customerPrefabs[newCustomerLine[i]]));
                customer.isSeating = false;
                customer.transform.rotation = waitingSeats[0].rotation;
                waitingCustomers.Insert(0, customer);
            }

            customerCount = waitingCustomers.Count;
            poppedCustomerCount = customerIndex;
        }

        for(int i = 0; i < waitingCustomers.Count; i++)
        {
            waitingCustomers[i].SetTarget(waitingSeats[i]);
        }


        TraceManager.Instance.currentCustomerIndex = poppedCustomerCount;
    }

    public int Min(int a, int b)
    {
        if (a < b) return a;
        return b;
    }

    public void PopCustomer(Customer customer)
    {
        if (waitingCustomers.Contains(customer))
        {
            waitingCustomers.Remove(customer);
        }
        ReOrderCustomer();
    }

    public void ReOrderCustomer()
    {
        for(int i = 0; i < waitingCustomers.Count; i++)
        {
            waitingCustomers[i].SetTarget(waitingSeats[i]);
        }
    }

    public async UniTask StartPopCustomer(int[] availableSeats, bool canDefeat)
    {
        bool flag = false;
        while (popCustomerTask.Status == UniTaskStatus.Pending)
        {
            if (!GameManager.Instance.isGameRunning)
            {
                flag = true;
                break;
            }
            await UniTask.Yield();
        }
        if (flag)
        {
        } else
            popCustomerTask = CheckIfCanPopCustomer(availableSeats, canDefeat, popCustomerToken.Token);
    }

    public async UniTask CheckIfCanPopCustomer(int[] availableSeats, bool canDefeat, CancellationToken token)
    {
        FrameRateManager.Instance.SetRenderingFullFps(true);
        int[] available = new int[availableSeats.Length];
        int[] seatDiff = new int[availableSeats.Length];
        bool flag = false;

        for (int i = 0; i < seatDiff.Length; i++)
        {
            available[i] = availableSeats[i];
            seatDiff[i] = 0;
        }

        while (waitingCustomers.Count > 0 && available[(int)waitingCustomers[0].GetPersonColor()] > 0 || available[levelData.colorCount] > 0)
        {
            if (token.IsCancellationRequested) return;
            TraceManager.Instance.SetUndoImpossible();
            while (GameManager.Instance.isInPause)
            {
                TraceManager.Instance.SetUndoImpossible();
                if (token.IsCancellationRequested) return;
                await UniTask.Yield();
            }
            if (!GameManager.Instance.isGameRunning)
            {
                flag = true;
                break;
            }
            if (available[levelData.colorCount] > 0)
            {
                seatDiff[(int)levelData.colorCount]--;
                available[(int)levelData.colorCount]--;


                waitingCustomers[0].GoToSeat(vehicleSpace.GetNextSeat(levelData.colorCount));
            }
            else
            {
                seatDiff[(int)waitingCustomers[0].GetPersonColor()]--;
                available[(int)waitingCustomers[0].GetPersonColor()]--;

                waitingCustomers[0].GoToSeat(vehicleSpace.GetNextSeat(waitingCustomers[0].GetPersonColor()));
            }

            customerCount--;
            poppedCustomerCount++;
            customerCounterTMP.text = (levelData.totalCustomerCount - poppedCustomerCount).ToString();

            PopCustomer(waitingCustomers[0]);
            canPopCustomer = false;

            while (!canPopCustomer)
            {
                if (token.IsCancellationRequested) return;
                await UniTask.Yield();
            }
        }
        if (!flag)
        {
            vehicleSpace.NotifyChange(seatDiff);
        }

        if (waitingCustomers.Count == 0)
        {
            GameManager.Instance.Win();
        }
        if (canDefeat) vehicleSpace.CheckIfDefeat();
    }
}
