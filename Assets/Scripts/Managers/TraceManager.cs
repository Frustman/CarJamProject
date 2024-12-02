using DG.Tweening;
using Solver;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Pick
{
    public VehicleLog pickedVehicle;
    public VehicleLog[] parkedVehicles;
    public int[] ridingCustomers;
    public int customerIndex;
    public List<int>[] availableSeatCount;
    public List<VehicleLog>[] parkedVehicleIndex;
}

public class VehicleLog
{
    public Vector2Int posInGrid;
    public VehicleDirection direction;
    public int colorIndex;
    public int vehicleIndex;

    public VehicleLog() { }

    public VehicleLog(VehicleLog vehicleLog)
    {
        if (vehicleLog == null) return;
        this.posInGrid = vehicleLog.posInGrid;
        this.direction = vehicleLog.direction;
        this.colorIndex = vehicleLog.colorIndex;
        this.vehicleIndex = vehicleLog.vehicleIndex;
    }
}

public class TraceManager : MonoBehaviour
{
    public static TraceManager Instance;
    [Header("Prefab Settings")]
    [SerializeField] private PickVehicle pickVehicle;
    [SerializeField] private CustomerSpawner spawner;
    [SerializeField] private VehicleSpace vehicleSpace;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private LevelSolverMultiThread levelSolverMulti;
    [SerializeField] private LevelSolver levelSolver;
    [SerializeField] private HintPanelUI hintPanelUI;


    [Header("Runtime")]

    [SerializeField] private int[] customerLine;

    [SerializeField] private VehicleLog[] parkedVehicles;
    [SerializeField] private GameObject failPanel;

    public bool canUndo;
    public int currentCustomerIndex;
    private Vector2Int gridCenter;

    private Stack<Pick> picks;
    private int availableSpaceCount;
    [SerializeField] private List<int>[] availableSeatCount;
    [SerializeField] private List<VehicleLog>[] parkedVehicleIndex;
    [SerializeField] private int[] ridingCustomers;

    private int colorCount;
    private float canUndoTimer = 0f;

    public bool useMult = false;


    public int solveIndex;

    public void Awake()
    {
        Instance = this;
    }

    public void GetCurrentSolverData(CurrentLevelData data)
    {
        data.parkedVehicleStates = new (int, int)[availableSpaceCount];
        int count = 0;
        for(int i = 0; i < availableSeatCount.Length; i++)
        {
            if (availableSeatCount[i].Count > 0)
            {
                for(int j = 0; j < availableSeatCount[i].Count; j++)
                {
                    data.parkedVehicleStates[count] = new(i, availableSeatCount[i][j]);
                    count++;
                }
            }
        }
        for(int i = count; i < availableSpaceCount; i++)
        {
            data.parkedVehicleStates[i].Item1 = -1;
        }

        data.currentCustomerIndex = currentCustomerIndex;
    }


    public void RemoveAnswer()
    {
        if (useMult) levelSolverMulti.RemoveAnswer();
        else levelSolver.RemoveAnswer();
    }


    public void Initialize(int[] customerLine, int availableSpaceCount, int colorCount, Vector2Int gridCenter, int[] answer)
    {
        canUndo = true;
        solveIndex = 0;
        failPanel.SetActive(false);
        picks = new Stack<Pick>();
        this.customerLine = customerLine;
        this.availableSpaceCount = availableSpaceCount;
        parkedVehicles = new VehicleLog[availableSpaceCount];
        this.gridCenter = gridCenter;
        this.colorCount = colorCount;

        availableSeatCount = new List<int>[colorCount];
        parkedVehicleIndex = new List<VehicleLog>[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            availableSeatCount[i] = new List<int>();
            parkedVehicleIndex[i] = new List<VehicleLog>();
        }

        ridingCustomers = new int[availableSpaceCount];

        for (int i = 0; i < ridingCustomers.Length; i++)
        {
            parkedVehicles[i] = null;
            ridingCustomers[i] = 0;
        }
        currentCustomerIndex = 0;
        if (useMult)
        {
            levelSolverMulti.InitializeCysharp(customerLine, availableSpaceCount, answer, colorCount);
        } else
        {
            levelSolver.InitializeCysharp(customerLine, availableSpaceCount, answer, colorCount);
        }
        //levelSolver.Initialize(customerLine, availableSpaceCount, answer, colorCount);
    }

    CancellationTokenSource solveToken = new();
    public void PickVehicle(Vehicle vehicle, int parkIdx)
    {
        PushPick(vehicle, parkIdx);
    }

    public async void CheckCanSolve(Vehicle vehicle)
    {
        if (useMult)
        {
            if (await levelSolverMulti.StartSolve(solveIndex, vehicle))
            {
                //failPanel.SetActive(false);
            }
            else
            {
                //failPanel.SetActive(true);
            }
        } else
        {
            if (await levelSolver.StartSolve(solveIndex, vehicle))
            {
                //failPanel.SetActive(false);
            }
            else
            {
                //failPanel.SetActive(true);
            }
        }
    }
    
    public async void CheckCanSolve()
    {
        pickVehicle.SetCanPick(false);
        hintPanelUI.gameObject.SetActive(true);
        if (useMult)
        {
            if (await levelSolverMulti.StartSolve(solveIndex))
            {
                hintPanelUI.SetYes();
            }
            else
            {
                hintPanelUI.SetNo();
            }
        }
        else
        {
            if (await levelSolver.StartSolve(solveIndex))
            {
                hintPanelUI.SetYes();
            }
            else
            {
                hintPanelUI.SetNo();
            }
        }
        pickVehicle.SetCanPick(true);
    }


    public async void UndoUntilCanWin()
    {
        pickVehicle.SetCanPick(false);
        hintPanelUI.gameObject.SetActive(true);
        if (useMult)
        {
            while (true)
            {
                if (await levelSolverMulti.StartSolve(solveIndex))
                {
                    hintPanelUI.SetYes();
                    break;
                }
                else
                {
                    Undo();
                    continue;
                }
            }
        }
        else
        {
            while (true)
            {
                if (await levelSolver.StartSolve(solveIndex))
                {
                    hintPanelUI.SetYes();
                    break;
                }
                else
                {
                    Undo();
                    continue;
                }
            }
        }
        pickVehicle.SetCanPick(true);
    }



    public void PushPick(Vehicle vehicle, int parkIdx)
    {
        int colorIndex = vehicle.GetVehicleColorIndex();

        Pick pick = new Pick();

        VehicleLog vehicleData = new VehicleLog();
        vehicleData.colorIndex = colorIndex;
        vehicleData.vehicleIndex = vehicle.GetVehicleIndex();
        vehicleData.posInGrid = vehicle.gridPos;
        vehicleData.direction = vehicle.vehicleDirection;

        pick.parkedVehicles = new VehicleLog[availableSpaceCount];
        pick.ridingCustomers = new int[availableSpaceCount];
        pick.pickedVehicle = vehicleData;
        pick.customerIndex = currentCustomerIndex;
        pick.availableSeatCount = new List<int>[colorCount];
        pick.parkedVehicleIndex = new List<VehicleLog>[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            pick.availableSeatCount[i] = new List<int>(availableSeatCount[i]);
            pick.parkedVehicleIndex[i] = new List<VehicleLog>(parkedVehicleIndex[i]);
        }

        for (int i = 0; i < availableSpaceCount; i++)
        {
            pick.parkedVehicles[i] = parkedVehicles[i];
            pick.ridingCustomers[i] = ridingCustomers[i];
        }

        parkedVehicles[parkIdx] = vehicleData;

        parkedVehicleIndex[colorIndex].Add(vehicleData);
        availableSeatCount[colorIndex].Add(Vehicle.GetCustomerCountByIndex(vehicleData.vehicleIndex));


        while (currentCustomerIndex < customerLine.Length && availableSeatCount[customerLine[currentCustomerIndex]].Count > 0 && availableSeatCount[customerLine[currentCustomerIndex]][0] > 0)
        {
            availableSeatCount[customerLine[currentCustomerIndex]][0]--;

            for(int i = 0; i < availableSpaceCount; i++)
            {
                if (parkedVehicles[i] == null) continue;
                if (parkedVehicleIndex[customerLine[currentCustomerIndex]].Count > 0 && parkedVehicles[i].Equals(parkedVehicleIndex[customerLine[currentCustomerIndex]][0])){
                    ridingCustomers[i]++;

                    if (ridingCustomers[i] == Vehicle.GetCustomerCountByIndex(parkedVehicleIndex[customerLine[currentCustomerIndex]][0].vehicleIndex))
                    {
                        ridingCustomers[i] = 0;
                        parkedVehicles[i] = null;
                    }
                    break;
                }
            }

            if (availableSeatCount[customerLine[currentCustomerIndex]][0] == 0)
            {
                availableSeatCount[customerLine[currentCustomerIndex]].RemoveAt(0);
                parkedVehicleIndex[customerLine[currentCustomerIndex]].RemoveAt(0);
            }
            currentCustomerIndex++;
        }

        picks.Push(pick);
        solveIndex++;
        CheckCanSolve(vehicle);
    }

    Pick lastPick;

    public void UndoPick()
    {
        if(GameManager.Instance.GetGoldAmount() >= 30)
        {
            Undo(() =>
            {
                GameManager.Instance.DecreaseGold(30);
            });
        } else
        {
            UIManager.Instance.ShowAdvertisePanel();
        }
    }

    public void Undo(TweenCallback callback = null)
    {
        if (canUndo)
        {
            if (picks.TryPop(out lastPick))
            {
                if (callback != null) callback();
                UIManager.Instance.RefreshHintUI();
                roundManager.UndoVehiclePick(lastPick.pickedVehicle);
                vehicleSpace.SetParkedVehicles(lastPick.ridingCustomers, lastPick.parkedVehicles);
                spawner.ChangeCustomerIndex(lastPick.customerIndex);

                parkedVehicleIndex = lastPick.parkedVehicleIndex;
                availableSeatCount = lastPick.availableSeatCount;
                ridingCustomers = lastPick.ridingCustomers;
                parkedVehicles = lastPick.parkedVehicles;
            }
            else
            {
                UIManager.Instance.ShowDialogueMessage("There's no selection to undo!");
            }
        }
    }

    private int Min(int a, int b)
    {
        if (a < b) return a;
        return b;
    }
    public void SuperUndo()
    {
        int undoCount = Min(picks.Count, 10);
        for (int i = 0; i < undoCount; i++)
        {
            if (picks.TryPop(out lastPick))
            {
                roundManager.UndoVehiclePick(lastPick.pickedVehicle);
                vehicleSpace.SetParkedVehicles(lastPick.ridingCustomers, lastPick.parkedVehicles);
                spawner.ChangeCustomerIndex(lastPick.customerIndex);

                parkedVehicleIndex = lastPick.parkedVehicleIndex;
                availableSeatCount = lastPick.availableSeatCount;
                ridingCustomers = lastPick.ridingCustomers;
                parkedVehicles = lastPick.parkedVehicles;
            }
            else
            {
                UIManager.Instance.ShowDialogueMessage("There's no selection to undo!");
            }
        }
    }

    public void SetUndoImpossible()
    {
        canUndo = false;
        canUndoTimer = 0;
    }

    public void Update()
    {
        canUndoTimer += Time.deltaTime;

        if (canUndoTimer > 1f)
        {
            canUndoTimer = 0;
            canUndo = true;
        }
    }
}
