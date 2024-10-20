using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Solver
{
    public class ParkedVehicle
    {
        public int index;
        public Vector2Int posInGrid;
        public VehicleDirection direction;
        public int colorIndex;
        public int vehicleIndex;
    }

    public class CurrentLevelData : IDisposable
    {
        public bool[][] vehicleExistGrid;

        public (int, int)[] parkedVehicleStates; // (colorIndex, remainSeatCount)
        public List<ParkedVehicle> remainVehicles;
        public int currentCustomerIndex;

        private bool disposed = false;

        public CurrentLevelData()
        {

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (remainVehicles != null)
                    {
                        remainVehicles.Clear();
                        remainVehicles = null;
                    }
                    if (vehicleExistGrid != null)
                    {
                        for (int i = 0; i < vehicleExistGrid.Length; i++)
                        {
                            vehicleExistGrid[i] = null;
                        }
                        vehicleExistGrid = null;
                    }

                    if (parkedVehicleStates != null)
                    {
                        parkedVehicleStates = null;
                    }
                }

                disposed = true;
            }
        }
        public CurrentLevelData(CurrentLevelData data)
        {
            vehicleExistGrid = new bool[data.vehicleExistGrid.Length][];
            for(int i = 0; i < vehicleExistGrid.Length; i++)
            {
                vehicleExistGrid[i] = new bool[data.vehicleExistGrid[i].Length];
                for(int j = 0; j < vehicleExistGrid[i].Length; j++)
                {
                    vehicleExistGrid[i][j] = data.vehicleExistGrid[i][j];
                }
            }

            remainVehicles = new List<ParkedVehicle>(data.remainVehicles);
            currentCustomerIndex = data.currentCustomerIndex;
            parkedVehicleStates = new (int, int)[data.parkedVehicleStates.Length];
            for(int i = 0; i < parkedVehicleStates.Length; i++)
            {
                parkedVehicleStates[i] = new (data.parkedVehicleStates[i].Item1, data.parkedVehicleStates[i].Item2);
            }
        }
    }

    public class LevelSolver : MonoBehaviour
    {
        [Header("Prefab Settings")]
        [SerializeField] private GameObject answerShowerPrefab;
        [SerializeField] private GameObject answerShowerDebugPrefab;

        public bool solveEveryPick = false;
        public bool debugMode = true;
        public bool detailDebugMode = true;
        public bool showAnswerDebug = false;

        public int[] customerLine;
        public int availableSpaceCount;

        private CancellationTokenSource solveToken = new();
        private int answerDebugPrefabPoolIndex;
        private int answerPrefabPoolIndex;

        private GameObject answerShower;
        private List<TextMeshPro> spawnedAnswersDebug;

        private Stack<ParkedVehicle> answers;
        private HashSet<string> visitedStates;
        [SerializeField] private int solveCount;

        public void Initialize(int[] customerLine, int availableSpaceCount)
        {
            this.customerLine = customerLine;
            this.availableSpaceCount = availableSpaceCount;
            spawnedAnswersDebug = new();
        }
        private void Start()
        {
            answerDebugPrefabPoolIndex = PoolManager.Instance.AssignPoolingObject(answerShowerDebugPrefab);
            answerPrefabPoolIndex = PoolManager.Instance.AssignPoolingObject(answerShowerPrefab);
        }

        public void CancelSolving()
        {
            if (solveToken != null)
            {
                solveToken.Cancel();
                solveToken.Dispose();
            }
            RemoveAnswer();
            if (spawnedAnswersDebug.Count > 0)
            {
                for (int i = 0; i < spawnedAnswersDebug.Count; i++)
                {
                    PoolManager.Instance.Put(answerDebugPrefabPoolIndex, spawnedAnswersDebug[i].gameObject);
                }
                spawnedAnswersDebug.Clear();
            }
            solveToken = new CancellationTokenSource();
        }

        private void OnApplicationQuit()
        {
            solveToken.Cancel();
        }

        public void SetAlwaysSolve(bool state)
        {
            solveEveryPick = state;
        }

        public void SetShowAnswer(bool state)
        {
            showAnswerDebug = state;
        }

        public void RemoveAnswer()
        {
            if (answerShower != null)
            {
                PoolManager.Instance.Put(answerPrefabPoolIndex, answerShower);
            }
        }

        public async UniTask<bool> StartSolve(int solveIndex, Vehicle vehicle = null)
        {
            CancelSolving();
            if (!solveEveryPick && vehicle != null) return true;

            CurrentLevelData initialData = RoundManager.Instance.GetCurrentSolverData(vehicle);
            answers = new();
            visitedStates = new();
            solveCount = 0;
            /*
            Debug.LogFormat("Remain Vehicle Count : {0}", initialData.remainVehicles.Count);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < initialData.parkedVehicleStates.Length; i++)
            {
                sb.Append(string.Format("Vehicle[{0}] = ({1} / {2})", i, initialData.parkedVehicleStates[i].Item1, initialData.parkedVehicleStates[i].Item2));
            }

            Debug.LogFormat("Current Customer Index : {0}", initialData.currentCustomerIndex);
            Debug.LogFormat("Vehicles || {0}", sb.ToString());

            for (int i = 0; i < initialData.vehicleExistGrid.Length; i++)
            {
                StringBuilder sb2 = new StringBuilder();
                for (int j = 0; j < initialData.vehicleExistGrid.Length; j++)
                {
                    sb2.Append((initialData.vehicleExistGrid[i][j] == true) ? "O\t" : "X\t");
                }
                Debug.Log(sb2.ToString());
            }
            */
            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            var result = await UniTask.RunOnThreadPool(() =>
            {
                return Solve(initialData, 1, solveToken.Token);
            });

            stopwatch.Stop();
            Debug.LogFormat("Solve Finished : {0} ms", stopwatch.ElapsedMilliseconds);

            if (solveIndex == TraceManager.Instance.solveIndex)
            {
                if (result)
                {
                    int count = 1;
                    if(answers.TryPop(out ParkedVehicle currentPick))
                    {
                        answerShower = PoolManager.Instance.Get(answerShowerPrefab);
                        answerShower.transform.position = RoundManager.Instance.GridPosToWorldSpace(currentPick.posInGrid) 
                                                            + RoundManager.Instance.GetVehicleOffsetFromDirection(currentPick.direction, currentPick.vehicleIndex)
                                                            + Vector3.up;
                    }
                    if (showAnswerDebug)
                    {
                        while (answers.TryPop(out ParkedVehicle pickedVehicle))
                        {
                            TextMeshPro answer = PoolManager.Instance.Get(answerShowerDebugPrefab).GetComponent<TextMeshPro>();
                            answer.transform.position = RoundManager.Instance.GridPosToWorldSpace(pickedVehicle.posInGrid) + Vector3.up;
                            answer.text = count.ToString();
                            spawnedAnswersDebug.Add(answer);
                            count++;
                        }
                    }
                    Debug.Log("Can Solve");
                }
                else
                {
                    Debug.Log("Cannot Solve");
                }
                return result;
            }
            return true;
        }

        public bool Solve(CurrentLevelData currentData, int iteration, CancellationToken token)
        {
            if (detailDebugMode)
                Debug.LogFormat("Solve Start, Iteration : {0}, remainVehicle : {1}", iteration, currentData.remainVehicles.Count);

            if (IsDefeat(currentData))
            {
                if (debugMode)
                    Debug.LogFormat("Defeat called!, Iteration : {0}", iteration);
                return false;
            }

            if (currentData.remainVehicles.Count <= 1 || currentData.currentCustomerIndex >= customerLine.Length)
            {
                if (debugMode)
                    Debug.LogFormat("There Is No Remain Vehicle : True!!, Iteration : {0}", iteration);
                return true;
            }
            solveCount++;
            ParkedVehicle[] orderedVehicles = currentData.remainVehicles
                .OrderBy(v => v.colorIndex != customerLine[currentData.currentCustomerIndex])
                .ToArray();

            foreach (var vehicle in orderedVehicles)
            {
                if (IsPoppable(currentData, vehicle))
                {
                    if (token.IsCancellationRequested) return false;

                    using (CurrentLevelData data = new CurrentLevelData(currentData))
                    {
                        if (!UpdateVehicleStates(data, vehicle)) return false;

                        RemoveVehicleFromGrid(data, vehicle);

                        if (!HandleCustomers(data)) return true;

                        data.parkedVehicleStates = data.parkedVehicleStates.OrderBy(item => item.Item1 == -1).ToArray();

                        string stateKey = GenerateStateKey(data);
                        if (visitedStates.Contains(stateKey)) continue;
                        visitedStates.Add(stateKey);

                        if (detailDebugMode)
                        {
                            LogDebugInfo(iteration, vehicle, currentData, data);
                        }

                        if (IsDefeat(data))
                        {
                            if (debugMode)
                                Debug.LogFormat("Defeat called!, Iteration : {0}", iteration);
                            return false;
                        }
                        if (Solve(data, iteration + 1, token))
                        {
                            answers.Push(vehicle);
                            if (debugMode)
                                Debug.LogFormat("Solve Called : {0} | customer Index : {1} | remainVehicleCount : {2} | Picked Vehicle : {3}",
                                                iteration, data.currentCustomerIndex, data.remainVehicles.Count, vehicle.index);
                            return true;
                        }
                        else
                        {
                            if (debugMode)
                                Debug.LogFormat("BackTrack Called : {0} | customer Index : {1} | remainVehicleCount : {2} | Picked Vehicle : {3}",
                                                iteration, data.currentCustomerIndex, data.remainVehicles.Count, vehicle.index);
                            continue;
                        }
                    }
                }
                else
                {
                    if (debugMode)
                        Debug.LogFormat("Cannot Pop Vehicle : {0} | Vehicle Index : {1}", iteration, vehicle.index);
                }
            }

            return false;
        }

        private bool UpdateVehicleStates(CurrentLevelData data, ParkedVehicle vehicle)
        {
            if (vehicle.colorIndex == -1) Debug.Log("Color Error");
            for (int j = 0; j < data.parkedVehicleStates.Length; j++)
            {
                if (data.parkedVehicleStates[j].Item1 == -1)
                {
                    data.parkedVehicleStates[j].Item1 = vehicle.colorIndex;
                    data.parkedVehicleStates[j].Item2 = Vehicle.GetCustomerCountByIndex(vehicle.vehicleIndex);
                    return true;
                }
            }
            Debug.Log("There is no place to park");
            return false;
        }

        private void RemoveVehicleFromGrid(CurrentLevelData data, ParkedVehicle vehicle)
        {
            for(int i = 0; i < data.remainVehicles.Count; i++)
            {
                if (data.remainVehicles[i].index == vehicle.index)
                {
                    data.remainVehicles.RemoveAt(i);
                    break;
                }
            }


            Vector2Int vehiclePos = vehicle.posInGrid;
            Vector2Int vehicleDirection = Vehicle.GetVehicleDirection(vehicle.direction);
            for (int j = 0; j < Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex); j++)
            {
                data.vehicleExistGrid[vehiclePos.x][vehiclePos.y] = false;
                vehiclePos += vehicleDirection;
            }
        }

        private bool HandleCustomers(CurrentLevelData data)
        {
            while (true)
            {
                if (data.currentCustomerIndex >= customerLine.Length) return false;

                bool customerHandled = false;
                for (int j = 0; j < data.parkedVehicleStates.Length; j++)
                {
                    if (data.parkedVehicleStates[j].Item1 == -1) continue;
                    if (customerLine[data.currentCustomerIndex] == data.parkedVehicleStates[j].Item1)
                    {
                        data.currentCustomerIndex++;
                        data.parkedVehicleStates[j].Item2--;
                        if (data.parkedVehicleStates[j].Item2 == 0)
                        {
                            data.parkedVehicleStates[j].Item1 = -1;
                        }
                        customerHandled = true;
                        break;
                    }
                }
                if (!customerHandled) break;
            }
            return true;
        }

        private void LogDebugInfo(int iteration, ParkedVehicle vehicle, CurrentLevelData currentData, CurrentLevelData data)
        {
            Debug.LogFormat("-------------------Solve Finished Iter {0} --------------------", iteration);
            Debug.LogFormat("PickedCar : {0}, {1}, |       Pos : {2}", GetColorNameFromIndex(vehicle.colorIndex), Vehicle.GetCustomerCountByIndex(vehicle.vehicleIndex), vehicle.posInGrid);
            Debug.LogFormat("currentCustomerIndex : {0} -> {1}", currentData.currentCustomerIndex, data.currentCustomerIndex);
            Debug.LogFormat("remainVehicleCounts : {0} -> {1}", currentData.remainVehicles.Count, data.remainVehicles.Count);

            for (int j = 0; j < currentData.parkedVehicleStates.Length; j++)
            {
                Debug.LogFormat("Before ParkedVehicleStates[{0}] = {1}, {2}", j, GetColorNameFromIndex(currentData.parkedVehicleStates[j].Item1), currentData.parkedVehicleStates[j].Item2);
            }
            Debug.Log("------------------------------------------------------------");

            for (int j = 0; j < data.parkedVehicleStates.Length; j++)
            {
                Debug.LogFormat("After ParkedVehicleStates[{0}] = {1}, {2}", j, GetColorNameFromIndex(data.parkedVehicleStates[j].Item1), data.parkedVehicleStates[j].Item2);
            }
            Debug.Log("============================================================");
        }

        private string GetColorNameFromIndex(int index)
        {
            if (index == 0) return "Blue";
            else if (index == 1) return "Brown";
            else if (index == 2) return "Orange";
            else if (index == 3) return "Pink";
            else if (index == 4) return "Purple";
            else if (index == 5) return "Red";
            else if (index == 6) return "Yellow";
            else if (index == -1) return "No";
            else return "Green";
        }

        private string GenerateStateKey(CurrentLevelData data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var state in data.parkedVehicleStates)
            {
                sb.Append(state.Item1).Append(",").Append(state.Item2).Append("|");
            }

            foreach (var vehicle in data.remainVehicles)
            {
                sb.Append(vehicle.index).Append(",");
            }

            return sb.ToString();
        }

        private bool IsDefeat(CurrentLevelData data)
        {
            int count = 0;
            for(int i = 0; i < data.parkedVehicleStates.Length; i++)
            {
                if (data.parkedVehicleStates[i].Item1 != -1) count++;
            }
            if (count >= availableSpaceCount) return true;
            return false;
        }


        private bool IsPoppable(CurrentLevelData data, ParkedVehicle vehicle)
        {
            Vector2Int dir = Vehicle.GetVehicleDirection(vehicle.direction);
            Vector2Int currentCheckPos = vehicle.posInGrid + dir * Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex);
            while (CheckBounds((data.vehicleExistGrid.Length, data.vehicleExistGrid[0].Length), currentCheckPos))
            {
                if (data.vehicleExistGrid[currentCheckPos.x][currentCheckPos.y] == true)
                {
                    return false;
                }
                currentCheckPos += dir;
            }
            return true;
        }


        private bool CheckBounds((int, int) gridSize, Vector2Int position)
        {
            if (position.x < 0 || position.x >= gridSize.Item1 || position.y < 0 || position.y >= gridSize.Item2)
                return false;
            return true;
        }

    }
}

