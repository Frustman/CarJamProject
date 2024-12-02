using Anoprsst;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Solver
{
    public struct ParkedVehicleStruct
    {
        public int index;
        public Vector2Int posInGrid;
        public VehicleDirection direction;
        public sbyte colorIndex;
        public sbyte vehicleIndex;

        private int blockedVehiclesLow;  
        private int blockedVehiclesMid;  
        private int blockedVehiclesHigh;  

        public int GetBlockedVehicle(int index)
        {
            if (index < 0 || index >= 7)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index < 3)
            {
                return (blockedVehiclesLow >> (index * 10)) & 0x3FF;
            }
            else if (index < 6)
            {
                return (blockedVehiclesMid >> ((index - 3) * 10)) & 0x3FF;
            }
            else
            {
                return (blockedVehiclesHigh >> ((index - 6) * 10)) & 0x3FF;
            }
        }

        public void SetBlockedVehicle(int index, int value)
        {
            if (index < 0 || index >= 7)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (value < 0 || value > 999)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (index < 3)
            {
                blockedVehiclesLow &= ~(0x3FF << (index * 10));  
                blockedVehiclesLow |= (value & 0x3FF) << (index * 10); 
            }
            else if (index < 6)
            {
                int midIndex = index - 3;
                blockedVehiclesMid &= ~(0x3FF << (midIndex * 10));  
                blockedVehiclesMid |= (value & 0x3FF) << (midIndex * 10); 
            }
            else
            {
                int highIndex = index - 6;
                blockedVehiclesHigh &= ~(0x3FF << (highIndex * 10));
                blockedVehiclesHigh |= (value & 0x3FF) << (highIndex * 10);
            }
        }

        public ParkedVehicleStruct(ParkedVehicle vehicle)
        {
            this.index = vehicle.index;
            this.posInGrid = vehicle.posInGrid;
            this.direction = vehicle.direction;
            this.colorIndex = (sbyte)vehicle.colorIndex;
            this.vehicleIndex = (sbyte)vehicle.vehicleIndex;
            this.blockedVehiclesLow = 0;
            this.blockedVehiclesMid = 0;
            this.blockedVehiclesHigh = 0;
        }
    }


    public struct CurrentStateStruct
    {
        private ulong packedVehicleExistGrid0;
        private ulong packedVehicleExistGrid1;
        private ulong packedVehicleExistGrid2;
        private ulong packedVehicleExistGrid3;
        private ulong packedVehicleExistGrid4;
        private ulong packedVehicleExistGrid5;
        private ulong packedVehicleExistGrid6;
        private ulong packedVehicleExistGrid7;
        private ulong packedVehicleExistGrid8;
        private ulong packedVehicleExistGrid9;

        public sbyte gridSize;

        private ulong packedParkedVehicleStates;
        public List<ParkedVehicleStruct> remainVehicles;
        public int currentCustomerIndex;


        public CurrentStateStruct(CurrentStateStruct data)
        {
            gridSize = data.gridSize;
            packedVehicleExistGrid0 = data.packedVehicleExistGrid0;
            packedVehicleExistGrid1 = data.packedVehicleExistGrid1;
            packedVehicleExistGrid2 = data.packedVehicleExistGrid2;
            packedVehicleExistGrid3 = data.packedVehicleExistGrid3;
            packedVehicleExistGrid4 = data.packedVehicleExistGrid4;
            packedVehicleExistGrid5 = data.packedVehicleExistGrid5;
            packedVehicleExistGrid6 = data.packedVehicleExistGrid6;
            packedVehicleExistGrid7 = data.packedVehicleExistGrid7;
            packedVehicleExistGrid8 = data.packedVehicleExistGrid8;
            packedVehicleExistGrid9 = data.packedVehicleExistGrid9;

            packedParkedVehicleStates = data.packedParkedVehicleStates;
            remainVehicles = new(data.remainVehicles);
            currentCustomerIndex = data.currentCustomerIndex;
        }

        public CurrentStateStruct(bool[][] vehicleExistGrid, sbyte gridSize, List<ParkedVehicleStruct> remainVehicles, int currentCustomerIndex)
        {
            this.gridSize = gridSize;
            this.remainVehicles = new (remainVehicles);
            this.currentCustomerIndex = currentCustomerIndex;

            packedVehicleExistGrid0 = 0;
            packedVehicleExistGrid1 = 0;
            packedVehicleExistGrid2 = 0;
            packedVehicleExistGrid3 = 0;
            packedVehicleExistGrid4 = 0;
            packedVehicleExistGrid5 = 0;
            packedVehicleExistGrid6 = 0;
            packedVehicleExistGrid7 = 0;
            packedVehicleExistGrid8 = 0;
            packedVehicleExistGrid9 = 0;

            for (int i = 0; i < gridSize * gridSize; i++)
            {
                if (vehicleExistGrid[i / gridSize][i % gridSize])
                {
                    int ulongIndex = i / 64;
                    int bitIndex = i % 64;

                    switch (ulongIndex)
                    {
                        case 0: packedVehicleExistGrid0 |= 1UL << bitIndex; break;
                        case 1: packedVehicleExistGrid1 |= 1UL << bitIndex; break;
                        case 2: packedVehicleExistGrid2 |= 1UL << bitIndex; break;
                        case 3: packedVehicleExistGrid3 |= 1UL << bitIndex; break;
                        case 4: packedVehicleExistGrid4 |= 1UL << bitIndex; break;
                        case 5: packedVehicleExistGrid5 |= 1UL << bitIndex; break;
                        case 6: packedVehicleExistGrid6 |= 1UL << bitIndex; break;
                        case 7: packedVehicleExistGrid7 |= 1UL << bitIndex; break;
                        case 8: packedVehicleExistGrid8 |= 1UL << bitIndex; break;
                        case 9: packedVehicleExistGrid9 |= 1UL << bitIndex; break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }

            packedParkedVehicleStates = 0; 
        }

        public void SetVehicleExist(int x, int y, bool value)
        {
            if (x < 0 || x >= gridSize || y < 0 || y >= gridSize)
                throw new ArgumentOutOfRangeException();

            int index = x * gridSize + y;
            int ulongIndex = index / 64;
            int bitIndex = index % 64;

            if (value)
            {
                switch (ulongIndex)
                {
                    case 0: packedVehicleExistGrid0 |= 1UL << bitIndex; break;
                    case 1: packedVehicleExistGrid1 |= 1UL << bitIndex; break;
                    case 2: packedVehicleExistGrid2 |= 1UL << bitIndex; break;
                    case 3: packedVehicleExistGrid3 |= 1UL << bitIndex; break;
                    case 4: packedVehicleExistGrid4 |= 1UL << bitIndex; break;
                    case 5: packedVehicleExistGrid5 |= 1UL << bitIndex; break;
                    case 6: packedVehicleExistGrid6 |= 1UL << bitIndex; break;
                    case 7: packedVehicleExistGrid7 |= 1UL << bitIndex; break;
                    case 8: packedVehicleExistGrid8 |= 1UL << bitIndex; break;
                    case 9: packedVehicleExistGrid9 |= 1UL << bitIndex; break;
                    default: break;
                }
            }
            else
            {
                switch (ulongIndex)
                {
                    case 0: packedVehicleExistGrid0 &= ~(1UL << bitIndex); break;
                    case 1: packedVehicleExistGrid1 &= ~(1UL << bitIndex); break;
                    case 2: packedVehicleExistGrid2 &= ~(1UL << bitIndex); break;
                    case 3: packedVehicleExistGrid3 &= ~(1UL << bitIndex); break;
                    case 4: packedVehicleExistGrid4 &= ~(1UL << bitIndex); break;
                    case 5: packedVehicleExistGrid5 &= ~(1UL << bitIndex); break;
                    case 6: packedVehicleExistGrid6 &= ~(1UL << bitIndex); break;
                    case 7: packedVehicleExistGrid7 &= ~(1UL << bitIndex); break;
                    case 8: packedVehicleExistGrid8 &= ~(1UL << bitIndex); break;
                    case 9: packedVehicleExistGrid9 &= ~(1UL << bitIndex); break;
                    default: break;
                }
            }
        }

        public bool GetVehicleExist(int x, int y)
        {
            if (x < 0 || x >= gridSize || y < 0 || y >= gridSize)
                throw new ArgumentOutOfRangeException();

            int index = x * gridSize + y;
            int ulongIndex = index / 64;
            int bitIndex = index % 64;

            switch (ulongIndex)
            {
                case 0: return (packedVehicleExistGrid0 & (1UL << bitIndex)) != 0;
                case 1: return (packedVehicleExistGrid1 & (1UL << bitIndex)) != 0;
                case 2: return (packedVehicleExistGrid2 & (1UL << bitIndex)) != 0;
                case 3: return (packedVehicleExistGrid3 & (1UL << bitIndex)) != 0;
                case 4: return (packedVehicleExistGrid4 & (1UL << bitIndex)) != 0;
                case 5: return (packedVehicleExistGrid5 & (1UL << bitIndex)) != 0;
                case 6: return (packedVehicleExistGrid6 & (1UL << bitIndex)) != 0;
                case 7: return (packedVehicleExistGrid7 & (1UL << bitIndex)) != 0;
                case 8: return (packedVehicleExistGrid8 & (1UL << bitIndex)) != 0;
                case 9: return (packedVehicleExistGrid9 & (1UL << bitIndex)) != 0;
                default: return false;
            }
        }

        public void SetParkedVehicleState(int index, int item1, int item2)
        {
            if (index < 0 || index >= 7)
                throw new ArgumentOutOfRangeException(nameof(index));

            ulong packedState = ((ulong)(item1 + 1) & 0xF) | (((ulong)(item2 + 1) & 0xF) << 4);

            packedParkedVehicleStates &= ~(0xFFUL << (index * 8));
            packedParkedVehicleStates |= packedState << (index * 8);
        }

        public (int, int) GetParkedVehicleState(int index)
        {
            if (index < 0 || index >= 7)
                throw new ArgumentOutOfRangeException(nameof(index));

            ulong packedState = (packedParkedVehicleStates >> (index * 8)) & 0xFF;

            int item1 = (int)(packedState & 0xF);
            int item2 = (int)((packedState >> 4) & 0xF);

            return (item1 - 1, item2 - 1);
        }

        public ulong GenerateKey()
        {
            ulong hash = 17;

            hash = hash * 31 + packedVehicleExistGrid0;
            hash = hash ^ (packedVehicleExistGrid1 * 31);
            hash = hash * 31 + packedVehicleExistGrid2;
            hash = hash ^ (packedVehicleExistGrid3 * 31);
            hash = hash * 31 + packedVehicleExistGrid4;
            hash = hash ^ (packedVehicleExistGrid5 * 31);
            hash = hash * 31 + packedVehicleExistGrid6;
            hash = hash ^ (packedVehicleExistGrid7 * 31);
            hash = hash * 31 + packedVehicleExistGrid8;
            hash = hash ^ (packedVehicleExistGrid9 * 31);

            hash = hash * 31 + packedParkedVehicleStates;

            hash = hash ^ (ulong)(currentCustomerIndex * 17);

            return hash;
        }
    }

    public struct ParkedVehicleComparer : IOrdering<ParkedVehicleStruct>
    {
        Func<ParkedVehicleStruct, (int, int)> keySelector;
        Func<ParkedVehicleStruct, int> colorSelector;


        public ParkedVehicleComparer(Func<ParkedVehicleStruct, (int, int)> keySelector, Func<ParkedVehicleStruct, int> colorSelector)
        {
            this.keySelector = keySelector;
            this.colorSelector = colorSelector;
        }



        public bool LessThan(ParkedVehicleStruct a, ParkedVehicleStruct b)
        {
            var keyA = keySelector(a);
            var keyB = keySelector(b);
            if (keyA == keyB)
                return colorSelector(a) > (colorSelector(b));
            if (keyA.Item1 < keyB.Item2)
                return true;
            else if (keyA.Item1 > keyB.Item1)
                return false;
            else
            {
                return keyA.Item2 <= keyB.Item2;
            }
        }
    }




    public class LevelSolverMultiThread : MonoBehaviour
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

        private Stack<ParkedVehicleStruct> answers;
        private HashSet<ulong> visitedStates;
        private HashSet<ulong> answerStates;

        private int[,] remainPoppableSeatCounts;
        private int colorCount;

        public int threadCount = 4;


        [SerializeField] private int solveCount;
        [SerializeField] private int answerCount;




        private bool isCancelled = false;

        public void InitializeCysharp(int[] customerLine, int availableSpaceCount, int[] answer, int colorCount)
        {
            this.customerLine = customerLine;
            this.availableSpaceCount = availableSpaceCount;
            this.colorCount = colorCount;
            answerStates = new();
            visitedStates = new();
            spawnedAnswersDebug = new();
            initialAnswer = answer;
            remainPoppableSeatCounts = new int[colorCount, 3];
            //GetInitialAnswer();
        }

        public void GetInitialAnswer()
        {
            CurrentLevelData currentData = RoundManager.Instance.GetCurrentSolverData();
            CurrentStateStruct data = ToCurrentStateStruct(currentData);
            
            while(data.remainVehicles.Count > 0)
            {
                foreach(int vehiclePos in initialAnswer)
                {
                    Vector2Int pos = new Vector2Int(vehiclePos / 25, vehiclePos % 25);
                    foreach(ParkedVehicleStruct vehicle in data.remainVehicles)
                    {
                        if (vehicle.posInGrid != pos) continue;

                        UpdateVehicleStates(ref data, vehicle);
                        RemoveVehicleFromGrid(ref data, vehicle);
                        HandleCustomers(ref data);


                        Span<(int, int)> parkedVehicles = stackalloc (int, int)[availableSpaceCount];
                        for (int i = 0; i < parkedVehicles.Length; i++)
                        {
                            parkedVehicles[i] = data.GetParkedVehicleState(i);
                        }

                        int parkCount = 0;
                        for (int i = 0; i < availableSpaceCount; i++)
                        {
                            if (parkedVehicles[i].Item1 != -1)
                            {
                                data.SetParkedVehicleState(parkCount, parkedVehicles[i].Item1, parkedVehicles[i].Item2);
                                parkCount++;
                            }
                        }

                        for (int i = parkCount; i < availableSpaceCount; i++)
                        {
                            data.SetParkedVehicleState(i, -1, 0);
                        }
                        parkedVehicles.Clear();

                        ulong stateKey = data.GenerateKey();
                        answerStates.Add(stateKey);
                        break;
                    }
                }
            }
        }

        [ContextMenu("Check")]
        public void Check()
        {
            CurrentStateStruct a = new CurrentStateStruct();
            a.gridSize = 25;

            a.remainVehicles = new List<ParkedVehicleStruct>();
            for(int i = 0; i < 10; i++)
            {
                a.remainVehicles.Add(new ParkedVehicleStruct());
            }
            a.currentCustomerIndex = 1;

            a.SetParkedVehicleState(2, 6, 7);
            a.SetVehicleExist(24, 24, false);
            Debug.LogFormat("Item1 : {0}, Item2 : {1}", a.GetParkedVehicleState(2).Item1, a.GetParkedVehicleState(2).Item2);
            Debug.LogFormat("Vehicle Exist : {0}", a.GetVehicleExist(22,22));

            ParkedVehicleStruct b = new();
            b.SetBlockedVehicle(6, 201);
            Debug.LogFormat("Vehicle Block : {0}", b.GetBlockedVehicle(6));
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

        public CurrentStateStruct ToCurrentStateStruct(CurrentLevelData data)
        {
            List<ParkedVehicleStruct> remainVehicles = new();



            foreach(var vehicle in data.remainVehicles)
            {
                ParkedVehicleStruct pv = new ParkedVehicleStruct(vehicle);
                for(int i = 0; i < colorCount; i++)
                {
                    pv.SetBlockedVehicle(i, vehicle.blockedVehicleCount[i]);
                }
                remainVehicles.Add(pv);
            }
            sbyte dataGridSize = (sbyte)data.vehicleExistGrid.Length;
            CurrentStateStruct state = new CurrentStateStruct(data.vehicleExistGrid, (sbyte)(data.vehicleExistGrid.Length), remainVehicles, data.currentCustomerIndex);

            for(int i = 0; i < data.parkedVehicleStates.Length; i++)
            {
                state.SetParkedVehicleState(i, data.parkedVehicleStates[i].Item1, data.parkedVehicleStates[i].Item2);
            }
            return state;
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
                CurrentStateStruct initialState = ToCurrentStateStruct(initialData);

                Stopwatch stopwatch = Stopwatch.StartNew();
                stopwatch.Start();



                bool result;

                solveToken.CancelAfter(5000);
                result = await UniTask.RunOnThreadPool(() =>
                {
                    return Solve_SingelThread(ref initialState, solveToken.Token);
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
                            if (answers.TryPop(out ParkedVehicleStruct currentPick))
                            {
                                answerShower = PoolManager.Instance.Get(answerShowerPrefab);
                                answerShower.transform.position = RoundManager.Instance.GridPosToWorldSpace(currentPick.posInGrid)
                                                                    + RoundManager.Instance.GetVehicleOffsetFromDirection(currentPick.direction, currentPick.vehicleIndex)
                                                                    + Vector3.up;
                            }else
                            {
                                Debug.Log("Cannot Found Answer");
                            }
                            if (showAnswerDebug)
                            {
                                while (answers.TryPop(out ParkedVehicleStruct pickedVehicle))
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
        private readonly object visitedStatesLock = new object();
        private readonly object answerStatesLock = new object();

        List<int> colorOrder = new();
        List<(int, int)> zeroColors = new();
        public async UniTask<bool> EnqueueSolveToThreadPool(CurrentStateStruct initialData, int threadCount, CancellationToken token)
        {

            SemaphoreSlim semaphore = new SemaphoreSlim(threadCount);
            List<ParkedVehicleStruct> poppables = new();
            foreach (var v in initialData.remainVehicles)
            {
                if (IsPoppable(ref initialData, v))
                {
                    poppables.Add(v);
                }
            }


            foreach (var vehicle in poppables)
            {
                remainPoppableSeatCounts[vehicle.colorIndex, vehicle.vehicleIndex]++;
            }
            for (int i = 0; i < colorCount; i++)
            {
                for (int j = 0; j < 3; j++)
                    if (remainPoppableSeatCounts[i, j] == 0) zeroColors.Add((i, j));
            }

            colorOrder.Clear();
            //for (int i = currentData.currentCustomerIndex; i < Math.Min(currentData.currentCustomerIndex + 17, customerLine.Length); i++)
            int counter = initialData.currentCustomerIndex;
            int firstColorCount = 1;
            while (counter < customerLine.Length)
            {
                int color = customerLine[counter];
                if (colorOrder.Count > 0 && color == colorOrder[0])
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

            Func<ParkedVehicleStruct, (int, int)> keySelector;
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


            Func<ParkedVehicleStruct, int> colorSelector;

            colorSelector = v =>
            {
                int index = 0;
                for (int i = 0; i < colorCount; i++)
                {
                    int blockedVehicleCount = v.GetBlockedVehicle(i);
                    index += blockedVehicleCount % 10 + (blockedVehicleCount / 10) + (blockedVehicleCount / 100);
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


            
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            List<UniTask<bool>> tasks = new ();
            int count = 0;
            foreach (var vehicle in orderedVehicles)
            {
                CurrentStateStruct data = new CurrentStateStruct(initialData);

                UpdateVehicleStates(ref data, vehicle);
                RemoveVehicleFromGrid(ref data, vehicle);
                HandleCustomers(ref data);

                List<(int, int)> parkedVehicles = new();
                for (int i = 0; i < availableSpaceCount; i++)
                {
                    parkedVehicles.Add(data.GetParkedVehicleState(i));
                }

                int parkCount = 0;
                for (int i = 0; i < availableSpaceCount; i++)
                {
                    if (parkedVehicles[i].Item1 != -1)
                    {
                        data.SetParkedVehicleState(parkCount, parkedVehicles[i].Item1, parkedVehicles[i].Item2);
                        parkCount++;
                    }
                }

                for (int i = parkCount; i < availableSpaceCount; i++)
                {
                    data.SetParkedVehicleState(i, -1, 0);
                }
                parkedVehicles.Clear();



                await semaphore.WaitAsync();
                tasks.Add(UniTask.RunOnThreadPool(() =>
                {
                    try
                    {
                        List<int> colorOrders = new();
                        List<(int, int)> zeroColors = new();
                        HashSet<ulong> _visitedStates = new();
                        HashSet<ulong> _answerStates = new();
                        bool result = Solve(ref data, cts.Token, _visitedStates, _answerStates, colorOrders, zeroColors);
                        if (result == true)
                        {
                            cts.Cancel();
                        }

                        lock (visitedStatesLock)
                        {
                            visitedStates.AddRange(_visitedStates);
                        }
                        lock (answerStatesLock)
                        {
                            answerStates.AddRange(_answerStates);
                        }
                        return result;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
                count++;
            }
            bool[] results = await UniTask.WhenAll(tasks);
            for(int i = 0; i < results.Length; i++)
            {
                if (results[i] == true)
                {
                    return true;
                }
            }
            return false;
        }



        public bool Solve_SingelThread(ref CurrentStateStruct currentData, CancellationToken token)
        {
            if (currentData.remainVehicles.Count <= 1 || currentData.currentCustomerIndex >= customerLine.Length)
            {
                return true;
            }
            solveCount++;
            zeroColors.Clear();

            int count = 0;
            foreach (var v in currentData.remainVehicles)
            {
                if (IsPoppable(ref currentData, v))
                {
                    count++;
                }
            }

            Span<ParkedVehicleStruct> poppables = stackalloc ParkedVehicleStruct[count];
            count = 0;
            foreach (var v in currentData.remainVehicles)
            {
                if (IsPoppable(ref currentData, v))
                {
                    poppables[count] = v;
                    count++;
                }
            }
            foreach (var vehicle in poppables)
            {
                remainPoppableSeatCounts[vehicle.colorIndex, vehicle.vehicleIndex]++;
            }
            for (int i = 0; i < colorCount; i++)
            {
                for (int j = 0; j < 3; j++)
                    if (remainPoppableSeatCounts[i, j] == 0) zeroColors.Add((i, j));
            }

            colorOrder.Clear();
            //for (int i = currentData.currentCustomerIndex; i < Math.Min(currentData.currentCustomerIndex + 17, customerLine.Length); i++)
            int counter = currentData.currentCustomerIndex;
            int firstColorCount = 1;
            while (counter < customerLine.Length)
            {
                int color = customerLine[counter];
                if (colorOrder.Count > 0 && color == colorOrder[0])
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

            Func<ParkedVehicleStruct, (int, int)> keySelector;
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

            Func<ParkedVehicleStruct, int> colorSelector;

            colorSelector = v =>
            {
                int index = 0;
                for (int i = 0; i < colorCount; i++)
                {
                    int blockedVehicleCount = v.GetBlockedVehicle(i);
                    index += blockedVehicleCount % 10 + (blockedVehicleCount / 10) + (blockedVehicleCount / 100);
                }
                if (zeroColors.Count != 0)
                {
                    index = zeroColors.Contains((v.colorIndex, v.vehicleIndex)) ? int.MaxValue : index;
                }
                return index;
            };


            poppables.WithOrder(new ParkedVehicleComparer(keySelector, colorSelector)).Sort();

            foreach (var vehicle in poppables)
            {
                if (token.IsCancellationRequested) return false;
                CurrentStateStruct data = new CurrentStateStruct(currentData);

                UpdateVehicleStates(ref data, vehicle);
                RemoveVehicleFromGrid(ref data, vehicle);
                HandleCustomers(ref data);

                Span<(int, int)> parkedVehicles = stackalloc (int, int)[availableSpaceCount];
                for (int i = 0; i < parkedVehicles.Length; i++)
                {
                    parkedVehicles[i] = data.GetParkedVehicleState(i);
                }

                int parkCount = 0;
                for (int i = 0; i < availableSpaceCount; i++)
                {
                    if (parkedVehicles[i].Item1 != -1)
                    {
                        data.SetParkedVehicleState(parkCount, parkedVehicles[i].Item1, parkedVehicles[i].Item2);
                        parkCount++;
                    }
                }

                for (int i = parkCount; i < availableSpaceCount; i++)
                {
                    data.SetParkedVehicleState(i, -1, 0);
                }
                parkedVehicles.Clear();

                ulong stateKey = data.GenerateKey();

                if (detailDebugMode) LogDebugInfo(0, vehicle, ref currentData, ref data);

                if (answerStates.Contains(stateKey))
                {
                    answers.Push(vehicle);
                    return true;
                }

                if (visitedStates.Contains(stateKey))
                {
                    continue;
                }

                if (IsDefeat(ref data))
                {
                    if (token.IsCancellationRequested) return false;
                    visitedStates.Add(stateKey);
                    continue;
                }

                if (Solve_SingelThread(ref data, token))
                {
                    answers.Push(vehicle);
                    answerStates.Add(stateKey);
                    return true;
                }
                else
                {
                    if (token.IsCancellationRequested) return false;
                    visitedStates.Add(stateKey);
                }
                data.remainVehicles.Clear();
            }
            return false;
        }


        public bool Solve(ref CurrentStateStruct currentData, CancellationToken token, HashSet<ulong> visitedStates, HashSet<ulong> answerStates, List<int> colorOrder, List<(int, int)> zeroColors)
        {
            if (currentData.remainVehicles.Count <= 1 || currentData.currentCustomerIndex >= customerLine.Length)
            {
                return true;
            }
            solveCount++;
            zeroColors.Clear();


            List<ParkedVehicleStruct> poppables = new();
            foreach (var v in currentData.remainVehicles)
            {
                if (IsPoppable(ref currentData, v))
                {
                    poppables.Add(v);
                }
            }


            foreach (var vehicle in poppables)
            {
                remainPoppableSeatCounts[vehicle.colorIndex, vehicle.vehicleIndex]++;
            }
            for (int i = 0; i < colorCount; i++)
            {
                for (int j = 0; j < 3; j++)
                    if (remainPoppableSeatCounts[i, j] == 0) zeroColors.Add((i, j));
            }

            colorOrder.Clear();
            //for (int i = currentData.currentCustomerIndex; i < Math.Min(currentData.currentCustomerIndex + 17, customerLine.Length); i++)
            int counter = currentData.currentCustomerIndex;
            int firstColorCount = 1;
            while (counter < customerLine.Length)
            {
                int color = customerLine[counter];
                if (colorOrder.Count > 0 && color == colorOrder[0])
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

            Func<ParkedVehicleStruct, (int, int)> keySelector;
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


            Func<ParkedVehicleStruct, int> colorSelector;

            colorSelector = v =>
            {
                int index = 0;
                for (int i = 0; i < colorCount; i++)
                {
                    int blockedVehicleCount = v.GetBlockedVehicle(i);
                    index += blockedVehicleCount % 10 + (blockedVehicleCount / 10) + (blockedVehicleCount / 100);
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

            foreach (var vehicle in orderedVehicles)
            {
                if (token.IsCancellationRequested) return false;
                CurrentStateStruct data = new CurrentStateStruct(currentData);

                UpdateVehicleStates(ref data, vehicle);
                RemoveVehicleFromGrid(ref data, vehicle);
                HandleCustomers(ref data);

                Span<(int, int)> parkedVehicles = stackalloc (int, int)[availableSpaceCount];
                for(int i = 0; i < parkedVehicles.Length; i++)
                {
                    parkedVehicles[i] = data.GetParkedVehicleState(i);
                }

                int parkCount = 0;
                for(int i = 0; i < availableSpaceCount; i++)
                {
                    if (parkedVehicles[i].Item1 != -1)
                    {
                        data.SetParkedVehicleState(parkCount, parkedVehicles[i].Item1, parkedVehicles[i].Item2);
                        parkCount++;
                    }
                }

                for(int i = parkCount; i < availableSpaceCount; i++)
                {
                    data.SetParkedVehicleState(i, -1, 0);
                }
                parkedVehicles.Clear();

                ulong stateKey = data.GenerateKey();

                if(detailDebugMode) LogDebugInfo(0, vehicle, ref currentData, ref data);

                if (answerStates.Contains(stateKey))
                {
                    answers.Push(vehicle);
                    return true;
                }

                if (visitedStates.Contains(stateKey))
                {
                    continue;
                }

                if (IsDefeat(ref data))
                {
                    if (token.IsCancellationRequested) return false;
                    visitedStates.Add(stateKey);
                    continue;
                }

                if(Solve(ref data, token, visitedStates, answerStates, colorOrder, zeroColors))
                {
                    answers.Push(vehicle);
                    answerStates.Add(stateKey);
                    return true;
                }
                else
                {
                    if (token.IsCancellationRequested) return false;
                    visitedStates.Add(stateKey);
                }
            }
            return false;
        }

        private bool UpdateVehicleStates(ref CurrentStateStruct data, ParkedVehicleStruct vehicle)
        {
            for (int i = 0; i < availableSpaceCount; i++)
            {
                int item1, item2;
                (item1, item2) = data.GetParkedVehicleState(i);
                if (item1 == -1)
                {
                    data.SetParkedVehicleState(i, vehicle.colorIndex, Vehicle.GetCustomerCountByIndex(vehicle.vehicleIndex));
                    return true;
                }
            }
            return false;
        }

        private void RemoveVehicleFromGrid(ref CurrentStateStruct data, ParkedVehicleStruct vehicle)
        {
            for (int i = 0; i < data.remainVehicles.Count; i++)
            {
                if (data.remainVehicles[i].index == vehicle.index)
                {
                    data.remainVehicles.RemoveAt(i);
                    break;
                }
            }


            Vector2Int vehiclePos = vehicle.posInGrid;
            Vector2Int vehicleDirection = Vehicle.GetVehicleDirection(vehicle.direction);
            for (int i = 0; i < Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex); i++)
            {
                data.SetVehicleExist(vehiclePos.x, vehiclePos.y, false);
                vehiclePos += vehicleDirection;
            }
        }

        private bool HandleCustomers(ref CurrentStateStruct data)
        {
            while (true)
            {
                if (data.currentCustomerIndex >= customerLine.Length) return false;

                bool customerHandled = false;
                for (int i = 0; i < availableSpaceCount; i++)
                {
                    int item1, item2;
                    (item1, item2) = data.GetParkedVehicleState(i);
                    if (item1 == -1) continue;
                    if (customerLine[data.currentCustomerIndex] == item1)
                    {
                        data.currentCustomerIndex++;
                        item2--;
                        if (item2 == 0)
                        {
                            item1 = -1;
                        }
                        data.SetParkedVehicleState(i, item1, item2);
                        customerHandled = true;
                        break;
                    }
                }
                if (!customerHandled) break;
            }
            return true;
        }

        private bool IsDefeat(ref CurrentStateStruct data)
        {
            int count = 0;
            for (int i = 0; i < availableSpaceCount; i++)
            {
                if (data.GetParkedVehicleState(i).Item1 != -1) count++;
            }
            if (count >= availableSpaceCount) return true;
            return false;
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

        private bool IsPoppable(ref CurrentStateStruct data, ParkedVehicleStruct vehicle)
        {
            Vector2Int dir = Vehicle.GetVehicleDirection(vehicle.direction);
            Vector2Int currentCheckPos = vehicle.posInGrid + dir * Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex);
            while (CheckBounds((data.gridSize, data.gridSize), currentCheckPos))
            {
                if (data.GetVehicleExist(currentCheckPos.x, currentCheckPos.y) == true)
                {
                    return false;
                }
                currentCheckPos += dir;
            }
            return true;
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


        private void LogDebugInfo(int iteration, ParkedVehicleStruct vehicle, ref CurrentStateStruct currentData, ref CurrentStateStruct data)
        {
            Debug.LogFormat("-------------------Solve Finished Iter {0} --------------------", iteration);
            Debug.LogFormat("PickedCar : {0}, {1}, |       Pos : {2}", GetColorNameFromIndex(vehicle.colorIndex), Vehicle.GetCustomerCountByIndex(vehicle.vehicleIndex), vehicle.posInGrid);
            Debug.LogFormat("currentCustomerIndex : {0} -> {1}", currentData.currentCustomerIndex, data.currentCustomerIndex);
            Debug.LogFormat("remainVehicleCounts : {0} -> {1}", currentData.remainVehicles.Count, data.remainVehicles.Count);

            for (int j = 0; j < availableSpaceCount; j++)
            {
                int item1, item2;
                (item1, item2) = currentData.GetParkedVehicleState(j);
                Debug.LogFormat("Before ParkedVehicleStates[{0}] = {1}, {2}", j, GetColorNameFromIndex(item1), item2);
            }
            Debug.Log("------------------------------------------------------------");

            for (int j = 0; j < availableSpaceCount; j++)
            {
                int item1, item2;
                (item1, item2) = data.GetParkedVehicleState(j);
                Debug.LogFormat("After ParkedVehicleStates[{0}] = {1}, {2}", j, GetColorNameFromIndex(item1), item2);
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

