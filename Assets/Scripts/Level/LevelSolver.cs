using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Solver
{
    public class ParkedVehicle
    {
        public int index;
        public int[] blockedVehicleCount;
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
        public string GenerateKey()
        {

            ulong hash = 17;

            foreach (var row in vehicleExistGrid)
            {
                foreach (var v in row)
                {
                    hash = hash * 31 + (v ? 1UL : 0UL);
                }
            }
            foreach (var vehicle in parkedVehicleStates)
            {
                hash = hash * 31 + (ulong)(vehicle.Item1 * 100 + vehicle.Item2);
            }

            hash = hash * 31 + (ulong)currentCustomerIndex;
            return hash.ToString();
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
        ~CurrentLevelData()
        {
            Dispose(false);
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
        public int[] initialAnswer;

        private CancellationTokenSource solveToken = new();
        private int answerDebugPrefabPoolIndex;
        private int answerPrefabPoolIndex;

        private GameObject answerShower;
        private List<TextMeshPro> spawnedAnswersDebug;

        private Stack<ParkedVehicle> answers;
        private HashSet<string> visitedStates;
        private HashSet<string> answerStates;

        private int[,] remainPoppableSeatCounts;
        private int colorCount;
        [SerializeField] private int solveCount;
        [SerializeField] private int answerCount;

        private bool isCancelled = false;

        public void InitializeCysharp(int[] customerLine, int availableSpaceCount, int[] answer, int colorCount)
        {
            this.customerLine = customerLine;
            this.availableSpaceCount = availableSpaceCount;
            this.colorCount = colorCount;
            visitedStates = new();
            answerStates = new();
            spawnedAnswersDebug = new();
            initialAnswer = answer;
            remainPoppableSeatCounts = new int[colorCount, 3];
            //GetInitialAnswer();
        }

        public void GetInitialAnswer()
        {
            CurrentLevelData data = RoundManager.Instance.GetCurrentSolverData();

            
            while(data.remainVehicles.Count > 0)
            {
                foreach(int vehiclePos in initialAnswer)
                {
                    Vector2Int pos = new Vector2Int(vehiclePos / 25, vehiclePos % 25);
                    foreach(ParkedVehicle vehicle in data.remainVehicles)
                    {
                        if (vehicle.posInGrid != pos) continue;

                        UpdateVehicleStates(data, vehicle);
                        RemoveVehicleFromGrid(data, vehicle);
                        HandleCustomers(data);

                        data.parkedVehicleStates = data.parkedVehicleStates.OrderBy(item => item.Item1 == -1).ToArray();

                        string stateKey = data.GenerateKey();
                        answerStates.Add(stateKey);
                        break;
                    }
                }
            }
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

            if (RoundManager.Instance.GetPoppedVehicleCount() == 0)
            {
                Vector2Int pos = new Vector2Int(initialAnswer[0] / 25, initialAnswer[0] % 25);
                foreach (ParkedVehicle remainVehicle in initialData.remainVehicles)
                {
                    if(remainVehicle.posInGrid == pos)
                    {
                        answerShower = PoolManager.Instance.Get(answerShowerPrefab);
                        answerShower.transform.position = RoundManager.Instance.GridPosToWorldSpace(remainVehicle.posInGrid)
                                                            + RoundManager.Instance.GetVehicleOffsetFromDirection(remainVehicle.direction, remainVehicle.vehicleIndex)
                                                            + Vector3.up;
                        break;
                    }
                }
            } else
            {
                Debug.LogFormat("Start Solve - Visited Node Count : {0}, Answer Node Count : {1}", visitedStates.Count, answerStates.Count);
                answers = new();
                solveCount = 0;
                answerCount = 0;

                Stopwatch stopwatch = Stopwatch.StartNew();
                stopwatch.Start();

                //solveToken.CancelAfter(3000);
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
                        if (vehicle == null)
                        {
                            if (answers.TryPop(out ParkedVehicle currentPick))
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
                        }
                        Debug.Log("Can Solve");
                    }
                    else
                    {
                        Debug.Log("Cannot Solve");
                    }
                    return result;

                }
            }
            return true;
        }

        List<int> colorOrder = new();
        List<(int, int)> zeroColors = new();

        public bool Solve(CurrentLevelData currentData, int iteration, CancellationToken token)
        {
            if (detailDebugMode)
                Debug.LogFormat("Solve Start, Iteration : {0}, remainVehicle : {1}", iteration, currentData.remainVehicles.Count);
            if (currentData.remainVehicles.Count <= 1 || currentData.currentCustomerIndex >= customerLine.Length)
            {
                if (debugMode)
                    Debug.LogFormat("There Is No Remain Vehicle : True!!, Iteration : {0}", iteration);
                answerCount++;
                return true;
            }

            solveCount++;

            zeroColors.Clear();

            var poppables = currentData.remainVehicles.Where(v => IsPoppable(currentData, v));
            for (int i = 0; i < colorCount; i++)
            {
                for (int j = 0; j < 3; j++)
                    remainPoppableSeatCounts[i, j] = 0;
            }


            foreach(var vehicle in poppables)
            {
                remainPoppableSeatCounts[vehicle.colorIndex, vehicle.vehicleIndex]++;
            }
            for(int i = 0; i < colorCount; i++)
            {
                for (int j = 0; j < 3; j++)
                    if (remainPoppableSeatCounts[i, j] == 0) zeroColors.Add((i, j));
            }
            

            colorOrder.Clear();
            //for (int i = currentData.currentCustomerIndex; i < Math.Min(currentData.currentCustomerIndex + 17, customerLine.Length); i++)
            int counter = currentData.currentCustomerIndex;
            int firstColorCount = 1;
            while(counter < customerLine.Length)
            {
                int color = customerLine[counter];
                if(colorOrder.Count > 0 && color == colorOrder[0])
                {
                    counter++;
                    firstColorCount++;
                    continue;
                }
                if (!colorOrder.Contains(color))
                {
                    if (colorOrder.Count == Math.Max(availableSpaceCount, 3)) break;
                    colorOrder.Add(color);
                }
                counter++;
            }

            Func<ParkedVehicle, (int, int)> keySelector;
            switch (firstColorCount)
            {
                case 4:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), (v.colorIndex == colorOrder[0] && v.vehicleIndex == 0) ? 0 : 1);
                    };
                    break;
                case 6:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), (v.colorIndex == colorOrder[0] && v.vehicleIndex == 1) ? 0 : 1);
                    };
                    break;
                case 8:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), (v.colorIndex == colorOrder[0] && v.vehicleIndex == 0) ? 0 : 1);
                    };
                    break;
                case 12:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), (v.colorIndex == colorOrder[0] && (v.vehicleIndex == 0 || v.vehicleIndex == 1)) ? 0 : 1);
                    };
                    break;
                case 14:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), (v.colorIndex == colorOrder[0] && (v.vehicleIndex == 0 || v.vehicleIndex == 2)) ? 0 : 1);
                    };
                    break;
                case 16:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), (v.colorIndex == colorOrder[0] && (v.vehicleIndex == 1 || v.vehicleIndex == 2)) ? 0 : 1);
                    };
                    break;
                case 18:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), (v.colorIndex == colorOrder[0] && (v.vehicleIndex == 1 || v.vehicleIndex == 2)) ? 0 : 1);
                    };
                    break;
                default:
                    keySelector = v =>
                    {
                        int order = colorOrder.IndexOf(v.colorIndex);
                        return ((order != -1 ? order : int.MaxValue), 0);
                    };
                    break;
            }


            Func<ParkedVehicle, int> colorSelector;

            colorSelector = v =>
            {
                int index = 0;
                for (int i = 0; i < v.blockedVehicleCount.Length; i++)
                {
                    index += v.blockedVehicleCount[i] % 10 + (v.blockedVehicleCount[i] / 10) + (v.blockedVehicleCount[i] / 100);
                }
                if (zeroColors.Count != 0)
                {
                    index = zeroColors.Contains((v.colorIndex, v.vehicleIndex)) ? int.MaxValue : index;
                }
                return index;
            };

            var orderedVehicles = poppables
                .OrderBy(keySelector)
                .ThenByDescending(colorSelector)
                .ToArray();

            /*var orderedVehicles = currentData.remainVehicles
                .OrderBy(v => v.colorIndex != customerLine[currentData.currentCustomerIndex])
                .ThenByDescending(v => v.blockedVehicleCount)
                .ToArray();*/
            foreach (var vehicle in orderedVehicles)
            {
                if (token.IsCancellationRequested) return false;

                using (CurrentLevelData data = new CurrentLevelData(currentData))
                {
                    UpdateVehicleStates(data, vehicle);

                    RemoveVehicleFromGrid(data, vehicle);

                    HandleCustomers(data);

                    data.parkedVehicleStates = data.parkedVehicleStates.OrderBy(item => item.Item1 == -1).ToArray();


                    string stateKey = data.GenerateKey();
                    if (answerStates.Contains(stateKey))
                    {
                        answers.Push(vehicle);
                        return true;
                    }

                    if (visitedStates.Contains(stateKey))
                    {
                        if (debugMode)
                            Debug.LogFormat("Already Visited State!, Iteration : {0}", iteration);
                        continue;
                    }


                    if (detailDebugMode)
                    {
                        LogDebugInfo(iteration, vehicle, currentData, data);
                    }

                    if (IsDefeat(data))
                    {
                        if (token.IsCancellationRequested) return false;
                        visitedStates.Add(stateKey);
                        if (debugMode)
                            Debug.LogFormat("Defeat called!, Iteration : {0}", iteration);
                        continue;
                    }
                    if (Solve(data, iteration + 1, token))
                    {
                        if (token.IsCancellationRequested) return false;
                        answers.Push(vehicle);
                        answerStates.Add(stateKey);
                        if (debugMode)
                            Debug.LogFormat("Solve Called : {0} | customer Index : {1} | remainVehicleCount : {2} | Picked Vehicle : {3}",
                                            iteration, data.currentCustomerIndex, data.remainVehicles.Count, vehicle.index);
                        return true;
                    }
                    else
                    {
                        if (token.IsCancellationRequested) return false;
                        visitedStates.Add(stateKey);
                        if (debugMode)
                            Debug.LogFormat("BackTrack Called : {0} | customer Index : {1} | remainVehicleCount : {2} | Picked Vehicle : {3}",
                                            iteration, data.currentCustomerIndex, data.remainVehicles.Count, vehicle.index);
                    }
                }
            }
            return false;
        }

        private bool UpdateVehicleStates(CurrentLevelData data, ParkedVehicle vehicle)
        {
            for (int j = 0; j < data.parkedVehicleStates.Length; j++)
            {
                if (data.parkedVehicleStates[j].Item1 == -1)
                {
                    data.parkedVehicleStates[j].Item1 = vehicle.colorIndex;
                    data.parkedVehicleStates[j].Item2 = Vehicle.GetCustomerCountByIndex(vehicle.vehicleIndex);
                    return true;
                }
            }
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

        private bool IsDefeat(CurrentLevelData data)
        {
            int count = 0;
            for (int i = 0; i < data.parkedVehicleStates.Length; i++)
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





    }
}

