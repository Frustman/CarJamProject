using Solver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance = null;

    [Header("Prefab Settings")]
    [SerializeField] private VehicleSpace vehicleSpace;
    [SerializeField] private Transform whiteVehicleSpawnPos;
    [SerializeField] private LevelData levelData;
    [SerializeField] private PickVehicle pickVehicle;
    [SerializeField] private List<Transform> particlePos = new();
    [SerializeField] private Transform pickAnswer;

    [SerializeField] private List<GameObject> sedans = new();
    [SerializeField] private List<GameObject> miniBuses = new();
    [SerializeField] private List<GameObject> Buses = new();

    [SerializeField] private GameObject dustPrefab;
    [Header("Runtime Settings")]
    public Vector2 cellSize;
    [SerializeField] private int seed;

    private System.Random random;
    private VehicleData[] vehicleDatas;
    private int[][] vehicleGrid;
    private bool[][] grid;
    private Vector2Int gridSize, gridCenter;
    private VehicleDirection[][] directionGrid;
    private bool[][] checker;

    private List<Vehicle> fieldVehicles;

    public int pickCount = 0;

    private WaitForSeconds waitFor25milli = new WaitForSeconds(0.25f);


    private void Start()
    {
        Instance = this;

        random = new System.Random(seed);
        PoolManager.Instance.AssignPoolingObject(dustPrefab);
    }

    public void StartLevel(LevelData level)
    {
        levelData = level;

        seed = level.seed;
        random = new System.Random(seed);

        pickCount = 0;

        gridSize = level.gridSize;
        vehicleDatas = level.vehicleDatas;
        vehicleGrid = level.vehicleGrid;

        Initialize();
        vehicleSpace.Initialize(level);
    }

    public int GetPoppedVehicleCount()
    {
        return levelData.vehicleDatas.Length - fieldVehicles.Count;
    }

    public CurrentLevelData GetCurrentSolverData(Vehicle exclusiveVehicle = null)
    {
        CurrentLevelData currentData = new CurrentLevelData();

        currentData.vehicleExistGrid = new bool[grid.Length][];


        for(int i = 0; i < grid.Length; i++)
        {
            currentData.vehicleExistGrid[i] = new bool[grid.Length];
            for(int j = 0; j < grid[i].Length; j++)
            {
                currentData.vehicleExistGrid[i][j] = grid[i][j];
            }
        }



        if(exclusiveVehicle != null)
        {
            Vector2Int vehiclePos = exclusiveVehicle.gridPos;
            Vector2Int vehicleDirection = Vehicle.GetVehicleDirection(exclusiveVehicle.vehicleDirection);
            for (int j = 0; j < Vehicle.GetVehicleSizeByIndex(exclusiveVehicle.GetVehicleIndex()); j++)
            {
                currentData.vehicleExistGrid[vehiclePos.x][vehiclePos.y] = false;
                vehiclePos += vehicleDirection;
            }
        }

        int[,][] blockedCarList = new int[grid.Length, grid[0].Length][];

        for(int i = 0; i < grid.Length; i++)
        {
            for(int j = 0; j < grid[0].Length; j++)
            {
                blockedCarList[i, j] = new int[levelData.colorCount];
            }
        }

        
        for (int i = 0; i < fieldVehicles.Count; i++)
        {
            Vector2Int dir = Vehicle.GetVehicleDirection(fieldVehicles[i].vehicleDirection);
            Vector2Int currentCheckPos = fieldVehicles[i].gridPos + dir * fieldVehicles[i].GetVehicleSize();
            while (CheckBounds(grid, currentCheckPos))
            {
                if (grid[currentCheckPos.x][currentCheckPos.y] && directionGrid[currentCheckPos.x][currentCheckPos.y] != fieldVehicles[i].vehicleDirection)
                {
                    VehicleData vehicleData = vehicleDatas[vehicleGrid[currentCheckPos.x][currentCheckPos.y]];
                    Vector2Int subDir = Vehicle.GetVehicleDirection(vehicleData.direction);
                    Vector2Int newCheckPos = vehicleData.posInGrid + subDir * Vehicle.GetVehicleSizeByIndex(vehicleData.vehicleIndex);
                    while (CheckBounds(grid, newCheckPos))
                    {
                        blockedCarList[newCheckPos.x, newCheckPos.y][fieldVehicles[i].GetVehicleColorIndex()] += (int)Math.Pow(10f, fieldVehicles[i].GetVehicleIndex());
                        newCheckPos += subDir;
                    }
                }
                blockedCarList[currentCheckPos.x, currentCheckPos.y][fieldVehicles[i].GetVehicleColorIndex()] += (int)Math.Pow(10f, fieldVehicles[i].GetVehicleIndex());
                currentCheckPos += dir;
            }
        }

        currentData.remainVehicles = new List<ParkedVehicle>();

        for(int i = 0; i < fieldVehicles.Count; i++)
        {
            if (exclusiveVehicle != null && fieldVehicles[i].gridPos == exclusiveVehicle.gridPos) continue;
            ParkedVehicle vehicleData = new ParkedVehicle();
            vehicleData.index = i;
            vehicleData.direction = fieldVehicles[i].vehicleDirection;
            vehicleData.posInGrid = fieldVehicles[i].gridPos;
            vehicleData.vehicleIndex = fieldVehicles[i].GetVehicleIndex();
            vehicleData.colorIndex = fieldVehicles[i].GetVehicleColorIndex();
            vehicleData.blockedVehicleCount = CalculateBlockedVehicleCount(blockedCarList, fieldVehicles[i]);

            currentData.remainVehicles.Add(vehicleData);
        }
        TraceManager.Instance.GetCurrentSolverData(currentData); 


        return currentData;
    }

    public int[] CalculateBlockedVehicleCount(int[,][] blockedCarCount, Vehicle vehicle)
    {
        int colorCount = levelData.colorCount;
        int[] blockedVehicleCount = new int[levelData.colorCount];
        for(int i = 0; i < blockedVehicleCount.Length; i++)
        {
            blockedVehicleCount[i] = 0;
        }

        Vector2Int vehiclePos = vehicle.gridPos;
        Vector2Int vehicleDirection = Vehicle.GetVehicleDirection(vehicle.vehicleDirection);
        for (int j = 0; j < Vehicle.GetVehicleSizeByIndex(vehicle.GetVehicleIndex()); j++)
        {
            for(int i = 0; i < blockedVehicleCount.Length; i++)
            {
                blockedVehicleCount[i] += blockedCarCount[vehiclePos.x, vehiclePos.y][i];
            }
            vehiclePos += vehicleDirection;
        }

        return blockedVehicleCount;
    }




    public int GetGridSize(int neededGridCount)
    {
        for (int i = 1; ; i += 2)
        {
            int side = (i - 1) / 2;

            if (i * i - 2 * side * (side + 1) > neededGridCount)
            {
                return i;
            }
        }
    }

    public void IncreasePickCount()
    {
        pickCount++;
    }

    public void Initialize()
    {
        grid = new bool[gridSize.x][];
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = new bool[gridSize.y];
        }
        directionGrid = new VehicleDirection[gridSize.x][];
        for(int i = 0; i < directionGrid.Length; i++)
        {
            directionGrid[i] = new VehicleDirection[gridSize.y];
        }
        gridCenter = new Vector2Int(gridSize.x / 2, gridSize.y / 2);

        fieldVehicles = new List<Vehicle>();

        if (levelData.vehicleGrid == null)
        {
            int gridLength = GetGridSize(levelData.totalGridCount) + 6;

            levelData.vehicleGrid = new int[gridLength][];
            for (int i = 0; i < gridLength; i++)
            {
                levelData.vehicleGrid[i] = new int[gridLength];
                for (int j = 0; j < gridLength; j++)
                {
                    levelData.vehicleGrid[i][j] = -1;
                }
            }
            for (int i = 0; i < levelData.vehicleDatas.Length; i++)
            {
                VehicleData vehicle = levelData.vehicleDatas[i];
                Vector2Int[] posList = GetVehicleGrids(vehicle.posInGrid, vehicle.direction, Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex));

                for (int j = 0; j < posList.Length; j++)
                {
                    levelData.vehicleGrid[posList[j].x][posList[j].y] = i;
                }
            }

            vehicleGrid = levelData.vehicleGrid;
        }


        for (int i = 0; i < grid.Length; i++)
        {
            for(int j = 0; j < grid[0].Length; j++)
            {
                if (vehicleGrid[i][j] != -1)
                {
                    VehicleData vehicle = vehicleDatas[vehicleGrid[i][j]];
                    if (vehicle.posInGrid.x == i && vehicle.posInGrid.y == j)
                    {
                        SpawnVehicle(vehicle.posInGrid, vehicle.direction, vehicle.colorIndex, vehicle.vehicleIndex);
                        SetVehicleGrid(vehicle.posInGrid, vehicle.direction, Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex), true);
                    }
                }
            }
        }
    }

    private GameObject currentPrefab;
    public Vehicle SpawnVehicle(Vector2Int position, VehicleDirection direction, int colorIndex, int vehicleIndex)
    {
        Vector3 pos = transform.position
                        + Vector3.right * cellSize.x * 1.2f * position.x
                        + Vector3.forward * cellSize.y * 1.2f * position.y
                        - Vector3.right * cellSize.x * 0.6f * gridSize.x
                        - Vector3.forward * cellSize.y * 0.6f * gridSize.y
                        + Vector3.right * cellSize.x * 0.6f
                        + Vector3.forward * cellSize.y * 0.6f;

        pos += GetVehicleOffsetFromDirection(direction, vehicleIndex);

        if (vehicleIndex == 0)
        {
            currentPrefab = sedans[colorIndex];
        }
        else if (vehicleIndex == 1)
        {
            currentPrefab = miniBuses[colorIndex];
        }
        else
        {
            currentPrefab = Buses[colorIndex];
        }
        Vehicle vehicle = PoolManager.Instance.Get(currentPrefab).GetComponent<Vehicle>();
        vehicle.transform.position = pos;
        vehicle.transform.rotation = GetVehicleRotation(direction);

        vehicle.Initialize(vehicleSpace, direction, position, colorIndex, PoolManager.Instance.GetPoolIndex(currentPrefab));
        fieldVehicles.Add(vehicle);

        return vehicle;
    }

    public void UndoVehiclePick(VehicleLog vehicleData)
    {
        SpawnVehicle(vehicleData.posInGrid, vehicleData.direction, vehicleData.colorIndex, vehicleData.vehicleIndex);
        SetVehicleGrid(vehicleData.posInGrid, vehicleData.direction, Vehicle.GetVehicleSizeByIndex(vehicleData.vehicleIndex), true);
    }


    public Vector3 GetVehicleOffsetFromDirection(VehicleDirection direction, int vehicleIndex)
    {
        Vector3 pos = Vector3.zero;
        if (direction == VehicleDirection.Left)
        {
            pos += Vector3.right * (Vehicle.GetVehicleSizeByIndex(vehicleIndex) == 2 ? 1f : 2f) * cellSize.x * 0.6f;
        }
        else if (direction == VehicleDirection.Right)
        {
            pos -= Vector3.right * (Vehicle.GetVehicleSizeByIndex(vehicleIndex) == 2 ? 1f : 2f) * cellSize.x * 0.6f;
        }
        else if (direction == VehicleDirection.Up)
        {
            pos -= Vector3.forward * (Vehicle.GetVehicleSizeByIndex(vehicleIndex) == 2 ? 1f : 2f) * cellSize.y * 0.6f;
        }
        else if (direction == VehicleDirection.Down)
        {
            pos += Vector3.forward * (Vehicle.GetVehicleSizeByIndex(vehicleIndex) == 2 ? 1f : 2f) * cellSize.y * 0.6f;
        }
        return pos;
    }

    public Vector3 GridPosToWorldSpace(Vector2Int pos)
    {
        return transform.position
            + Vector3.right * cellSize.x * 1.2f * pos.x
            + Vector3.forward * cellSize.y * 1.2f * pos.y
            - Vector3.right * cellSize.x * 0.6f * gridSize.x
            - Vector3.forward * cellSize.y * 0.6f * gridSize.y
            + Vector3.right * cellSize.x * 0.6f
            + Vector3.forward * cellSize.y * 0.6f;
    }


    public bool CanPopFromField(Vehicle vehicle, out Vector3 vehiclePos)
    {
        Vector2Int dir = Vehicle.GetVehicleDirection(vehicle.vehicleDirection);
        Vector2Int currentCheckPos = vehicle.gridPos + dir * vehicle.GetVehicleSize();
        vehiclePos = Vector3.zero;
        while(CheckBounds(grid, currentCheckPos))
        {
            if (grid[currentCheckPos.x][currentCheckPos.y])
            {
                vehiclePos = GridPosToWorldSpace(currentCheckPos);
                return false; 
            }
            currentCheckPos += dir;
        }
        if (!vehicleSpace.CanParkVehicle())
            return false;
        return true;
    }

    private bool CheckBounds(bool[][] checker, Vector2Int position)
    {
        if (position.x < 0 || position.x >= checker.Length || position.y < 0 || position.y >= checker[0].Length ||
            (((position.x - gridCenter.x) * Mathf.Sign(position.x - gridCenter.x) + (position.y - gridCenter.y) * Mathf.Sign(position.y - gridCenter.y)) > gridCenter.x))
            return false;
        return true;
    }


    public void PopVehicle(Vehicle vehicle)
    {
        SetVehicleGrid(vehicle.gridPos, vehicle.vehicleDirection, vehicle.GetVehicleSize(), false);
        if (fieldVehicles.Contains(vehicle))
        {
            fieldVehicles.Remove(vehicle);
        }
    }
    
    private void SetVehicleGrid(Vector2Int position, VehicleDirection direction, int vehicleSize, bool state)
    {
        Vector2Int[] posList = GetVehicleGrids(position, direction, vehicleSize);

        for (int i = 0; i < posList.Length; i++)
        {
            grid[posList[i].x][posList[i].y] = state;
            directionGrid[posList[i].x][posList[i].y] = direction;
        }
    }


    private Vector2Int[] GetVehicleGrids(Vector2Int vehiclePosition, VehicleDirection direction, int size)
    {
        Vector2Int[] grids = new Vector2Int[size];
        Vector2Int dir = Vehicle.GetVehicleDirection(direction);

        Vector2Int currentPos = vehiclePosition;
        for (int i = 0; i < size; i++)
        {
            grids[i] = new Vector2Int(currentPos.x, currentPos.y);
            currentPos += dir;
        }

        return grids;
    }


    private Quaternion GetVehicleRotation(VehicleDirection direction)
    {
        switch (direction)
        {
            case VehicleDirection.Up:
                return Quaternion.Euler(0, 270, 0);

            case VehicleDirection.Down:
                return Quaternion.Euler(0, 90, 0);

            case VehicleDirection.Left:
                return Quaternion.Euler(0, 180, 0);

            case VehicleDirection.Right:
                return Quaternion.Euler(0, 0, 0);

            default:
                return Quaternion.Euler(0, 0, 0);
        }
    }


    public bool IsValidVehicle(Vector2Int position, VehicleDirection direction, int vehicleSize)
    {
        Vector2Int[] posList = GetVehicleGrids(position, direction, vehicleSize);

        for (int i = 0; i < posList.Length; i++)
        {
            if (!CheckBounds(grid, posList[i]) || grid[posList[i].x][posList[i].y])
            {
                return false;
            }
        }

        return true;
    }


    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Vector3 boxSize = new Vector3(cellSize.x, 0.2f, cellSize.y);
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                if (checker != null)
                {
                    if (checker[i][j])
                        Gizmos.color = Color.green;
                    else
                        Gizmos.color = Color.red;
                }

                Gizmos.DrawWireCube(transform.position
                        + Vector3.right * cellSize.x * 1.2f * i
                        + Vector3.forward * cellSize.y * 1.2f * j
                        - Vector3.right * cellSize.x * 0.6f * gridSize.x
                        - Vector3.forward * cellSize.y * 0.6f * gridSize.y
                        + Vector3.right * cellSize.x * 0.6f
                        + Vector3.forward * cellSize.y * 0.6f, boxSize);
            }
        }
    }
}
