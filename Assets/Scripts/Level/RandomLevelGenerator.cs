using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class RandomLevelGenerator : MonoBehaviour
{
    [Header("Map Generate Parameter")]
    public int colorCount;

    public float directionFlipRate = 0.5f;


    public int maxCustomerLineCount = 5;
    public int minCustomerLineCount = 2;

    public int availableSpaceCount = 3;

    public string themeName = "Thema_City";

    private int seed;
    private System.Random random;

    private bool[][] grid;
    private int colorIndex, vehicleIndex, vehicleSize;
    private VehicleDirection tempDirection;
    private bool[][] checker;
    private Vector2Int gridCenter;
    private VehicleDirection[][] directionGrid;

    private int[][] remainingVehicles;

    private List<VehicleData> vehicleDataList;
    private Vector2Int gridSize;
    private int[][] vehicleGrid;
    private float[] hues;
    private float[] vehiclePickRate = new float[3];

    private int leastParkableSpace;

    private int[][] vehicleCounts;

#if UNITY_EDITOR
    [ContextMenu("Generate Level To Folder")]
    public void GenerateLevelToFolder()
    {
        string path = "Assets/Resources/Levels";
        for(int i = 1; i < 200; i++)
        {
            for(int j = 0; j < 2; j++)
            {
                LevelData data = GenerateLevelByDifficulty(i, j);
                AssetDatabase.CreateAsset(data, string.Format("{0}/{1}-{2}.asset", path, i, j+1));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
#endif

    float pickedValue;
    public LevelData GenerateLevelByDifficulty(int level, int difficulty)
    {
        random = new System.Random(10000 * level + difficulty);
        int levelIndex;
        leastParkableSpace = random.Next(2, 3 + (int)(3f - 60f / (level + 20f)));
        if (difficulty == 0)
        {
            levelIndex = 1;
            if(level < 10)
            {
                colorCount = 4;
            } else
            {
                colorCount = random.Next(3, 5);
            }
            availableSpaceCount = Max(2 - (int)(levelIndex / 2f) + leastParkableSpace, leastParkableSpace);
        } else
        {
            if(level < 10)
            {
                levelIndex = 2 + Min(0, (level - 1) / 3);
                availableSpaceCount = Max(2 - (int)(levelIndex / 2f) + leastParkableSpace, leastParkableSpace);
            } else
            {
                levelIndex = 4 - (level % 3);
                availableSpaceCount = Max(2 - (int)(levelIndex / 2f) + leastParkableSpace, leastParkableSpace);
                if (levelIndex == 2) availableSpaceCount--;
            }
            colorCount = 4 + (int)(levelIndex / 1.5f);
        }


        directionFlipRate = 0.4f + (levelIndex) * 0.05f;
        vehicleCounts = new int[colorCount][];
        for(int i = 0; i < colorCount; i++)
        {
            vehicleCounts[i] = new int[3];
        }


        vehiclePickRate[0] = 0.4f;
        vehiclePickRate[1] = 0.4f;
        vehiclePickRate[2] = 0.2f;


        int vehiclePickCount = (int)((4f - 8f / (level + 1f)) * colorCount);
        if (difficulty == 0) vehiclePickCount = Min(vehiclePickCount, 10 - colorCount);
        for(int i = 0; i < colorCount; i++)
        {
            vehicleCounts[i][0] = 1 + (int)((levelIndex) / 1.5f);    // 1 ~ 4
            vehicleCounts[i][1] = (int)((levelIndex) / 1.5f);        // 0 ~ 3
            vehicleCounts[i][2] = 0 + (levelIndex) / 3;    // 0 ~ 1

            if(level > 10 && levelIndex == 2)
            {
                vehicleCounts[i][0]++;
                vehicleCounts[i][1]++;
            }
        }

        //Debug.LogFormat("Vehicle 0 : {0}, Vehicle 1 : {1}, Vehicle 2 : {2}", vehiclePickRate[0])

        for(int i = 0; i < vehiclePickCount; i++)
        {
            int pickedColor = random.Next(0, colorCount);
            pickedValue = (float)random.NextDouble();

            if(pickedValue < vehiclePickRate[2])
            {
                vehicleCounts[pickedColor][2]++;
            } else if (pickedValue < vehiclePickRate[1] + vehiclePickRate[2])
            {
                vehicleCounts[pickedColor][1]++;
            } else
            {
                vehicleCounts[pickedColor][0]++;
            }
        }

        seed = 10000 * level + difficulty;


        return GenerateLevel(level, difficulty);
    }

    private int Max(int a, int b)
    {
        if (a > b) return a;
        else return b;
    }

    public LevelData GenerateLevel(int level, int difficulty)
    {
        LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
        levelData.name = string.Format("{0}-{1}", level, difficulty + 1);
        levelData.difficulty = difficulty;
        levelData.colorCount = colorCount;
        levelData.availableVehicleSpace = availableSpaceCount;
        levelData.level = difficulty;
        levelData.leastParkableSpace = leastParkableSpace;

        levelData.vehicle2x2Count = new int[colorCount];
        levelData.vehicle3x2Count = new int[colorCount];
        levelData.vehicle5x2Count = new int[colorCount];

        levelData.customerCount = new int[colorCount];

        levelData.totalCustomerCount = 0;
        int vehicleCount0 = 0, vehicleCount1 = 0, vehicleCount2 = 0;
        for (int i = 0; i < colorCount; i++)
        {
            vehicleCount0 += vehicleCounts[i][0];
            vehicleCount1 += vehicleCounts[i][1];
            vehicleCount2 += vehicleCounts[i][2];

            levelData.vehicle2x2Count[i] = vehicleCounts[i][0];
            levelData.vehicle3x2Count[i] = vehicleCounts[i][1];
            levelData.vehicle5x2Count[i] = vehicleCounts[i][2];

            levelData.customerCount[i] = vehicleCounts[i][0] * 4 + vehicleCounts[i][1] * 6 + vehicleCounts[i][2] * 10;
            levelData.totalCustomerCount += levelData.customerCount[i];
        }

        levelData.totalGridCount = (vehicleCount0 * Vehicle.GetVehicleSizeByIndex(0) + vehicleCount1 * Vehicle.GetVehicleSizeByIndex(1) + vehicleCount2 * Vehicle.GetVehicleSizeByIndex(2));
        levelData.totalVehicleCount = vehicleCount0 + vehicleCount1 + vehicleCount2;
        levelData.seed = seed;
        levelData.directionFlipRate = directionFlipRate;

        GenerateGridData(levelData);
        levelData.customerColorLine = GenerateCustomerLineFromGrid(levelData);

        levelData.vehicleDatas = vehicleDataList.ToArray();
        levelData.gridSize = gridSize;
        levelData.vehicleGrid = vehicleGrid;
        levelData.themaSceneName = themeName;
        return levelData;
    }



    public bool CanPopFromField(bool[][] grid, VehicleData vehicle)
    {
        Vector2Int dir = Vehicle.GetVehicleDirection(vehicle.direction);
        Vector2Int currentCheckPos = vehicle.posInGrid + dir * Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex);
        while (CheckBounds(grid, currentCheckPos))
        {
            if (grid[currentCheckPos.x][currentCheckPos.y])
                return false;
            currentCheckPos += dir;
        }
        return true;
    }


    public int[] GenerateCustomerLineFromGrid(LevelData level)
    {
        random = new System.Random(level.seed);

        /*
            level.vehicleDatas = vehicleDataList.ToArray();
            level.gridSize = gridSize;
            level.vehicleGrid = vehicleGrid;
         */

        List<VehicleData> vehicleDataList = new List<VehicleData>();
        List<VehicleData> popOrder = new List<VehicleData>();
        for (int i = 0; i < level.vehicleDatas.Length; i++)
        {
            vehicleDataList.Add(level.vehicleDatas[i]);
        }

        int vehicleIdx;
        VehicleData currentVehicle;
        while (vehicleDataList.Count > 0)
        {
            vehicleIdx = random.Next(vehicleDataList.Count);

            currentVehicle = vehicleDataList[vehicleIdx];
            if (CanPopFromField(grid, currentVehicle))
            {
                SetVehicleGrid(currentVehicle.posInGrid, currentVehicle.direction, Vehicle.GetVehicleSizeByIndex(currentVehicle.vehicleIndex), false);
                vehicleDataList.Remove(currentVehicle);
                popOrder.Add(currentVehicle);
            }
        }

        for (int i = 0; i < level.vehicleDatas.Length; i++)
        {
            vehicleDataList.Add(level.vehicleDatas[i]);
        }

        int[] answer = new int[vehicleDataList.Count];
        for (int i = 0; i < popOrder.Count; i++)
        {
            answer[i] = vehicleDataList.IndexOf(popOrder[i]);
        }
        level.pickAnswer = answer;

        return GenerateCustomerLineFromPopOrder(popOrder, level);
    }


    public int[] GenerateCustomerLineFromPopOrder(List<VehicleData> popOrder, LevelData level)
    {
        int[] customerLine = new int[level.totalCustomerCount];
        int customerLineIdx = 0;
        int[] customerLineIndexFromPopOrder = new int[popOrder.Count];
        for (int i = 0; i < popOrder.Count; i++)
        {
            customerLineIndexFromPopOrder[i] = Vehicle.GetCustomerCountByIndex(popOrder[i].vehicleIndex);
            for (int j = 0; j < Vehicle.GetCustomerCountByIndex(popOrder[i].vehicleIndex); j++)
            {
                customerLine[customerLineIdx + j] = popOrder[i].colorIndex;
            }
            customerLineIdx += Vehicle.GetCustomerCountByIndex(popOrder[i].vehicleIndex);
        }
        if(level.leastParkableSpace == 2)
        {
            int shuffleCount, temp;
            customerLineIdx = Vehicle.GetCustomerCountByIndex(popOrder[0].vehicleIndex);
            for (int i = 1; i < popOrder.Count; i++)
            {
                shuffleCount = random.Next(1, Min(Vehicle.GetCustomerCountByIndex(popOrder[i].vehicleIndex), Vehicle.GetCustomerCountByIndex(popOrder[i - 1].vehicleIndex)) / 2);

                for (int j = 1; j < shuffleCount + 1; j++)
                {
                    temp = customerLine[customerLineIdx + j - 1];
                    customerLine[customerLineIdx + j - 1] = customerLine[customerLineIdx - j];
                    customerLine[customerLineIdx - j] = temp;
                }

                customerLineIdx += Vehicle.GetCustomerCountByIndex(popOrder[i].vehicleIndex);
            }
        }
        else
        {
            customerLineIdx = 0;
            int shuffleVehicleCount, maxShuffleIdx, vehicleIdx = 0;
            int changeIdx1, changeIdx2, tempValue;
            while (vehicleIdx < customerLineIndexFromPopOrder.Length)
            {
                shuffleVehicleCount = random.Next(2, level.leastParkableSpace + 1);
                if (vehicleIdx + shuffleVehicleCount >= customerLineIndexFromPopOrder.Length) break;
                maxShuffleIdx = customerLineIdx;
                for (int i = 0; i < shuffleVehicleCount; i++)
                {
                    maxShuffleIdx += customerLineIndexFromPopOrder[vehicleIdx + i];
                }
                if (shuffleVehicleCount != 1)
                {
                    for (int i = 0; i < maxShuffleIdx - customerLineIdx; i++)
                    {
                        changeIdx1 = random.Next(customerLineIdx, maxShuffleIdx);
                        changeIdx2 = random.Next(customerLineIdx, maxShuffleIdx);
                        if (changeIdx1 == changeIdx2) continue;

                        tempValue = customerLine[changeIdx1];
                        customerLine[changeIdx1] = customerLine[changeIdx2];
                        customerLine[changeIdx2] = tempValue;
                    }
                }
                customerLineIdx = maxShuffleIdx;
                vehicleIdx += shuffleVehicleCount;
            }
        }


        return customerLine;
    }


    public int Min(int a, int b)
    {
        if (a < b) return a;
        return b;
    }


    public int[] GenerateCustomerLine(LevelData level, int colorCount)
    {
        random = new System.Random(level.seed);
        int[] customerLine = new int[level.totalCustomerCount];
        int[] customerCounts = new int[level.colorCount];
        for (int i = 0; i < level.colorCount; i++)
        {
            customerCounts[i] = level.customerCount[i];
        }

        int remainingCustomerCount = level.totalCustomerCount;

        int currentColor = 0, currentCustomer;
        int counter = 0;

        while (remainingCustomerCount > 0)
        {
            (currentCustomer, currentColor) = PickColor(currentColor, customerCounts);
            for (int i = 0; i < currentCustomer; i++)
            {
                customerLine[counter + i] = currentColor;
                remainingCustomerCount--;
            }
            customerCounts[currentColor] -= currentCustomer;
            counter += currentCustomer;
        }

        return customerLine;
    }


    public (int, int) PickColor(int beforeColor, int[] customerCounts)
    {
        bool isValid = false;

        int currentColor = 0;
        int customerCount = 0;
        while (!isValid)
        {
            currentColor = random.Next(0, colorCount);
            if (currentColor == beforeColor)
            {
                bool flag = false;
                for (int i = 0; i < colorCount; i++)
                {
                    if (i != currentColor && customerCounts[i] > 0)
                        flag = true;
                }
                if (flag) continue;
            }

            if (customerCounts[currentColor] > 0)
            {
                if (customerCounts[currentColor] < minCustomerLineCount)
                {
                    customerCount = customerCounts[currentColor];
                }
                else
                {
                    customerCount = (customerCounts[currentColor] > maxCustomerLineCount) ? random.Next(minCustomerLineCount, maxCustomerLineCount) : random.Next(minCustomerLineCount, customerCounts[currentColor]);
                }

                isValid = true;
            }
            else
            {
                isValid = false;
            }
        }

        return (customerCount, currentColor);
    }

    public void GenerateGridData(LevelData level)
    {
        random = new System.Random(level.seed);

        int gridLength = GetGridSize(level.totalGridCount) + 6;

        remainingVehicles = new int[level.colorCount][];


        for (int i = 0; i < level.colorCount; i++)
        {
            remainingVehicles[i] = new int[3];

            remainingVehicles[i][0] = level.vehicle2x2Count[i];
            remainingVehicles[i][1] = level.vehicle3x2Count[i];
            remainingVehicles[i][2] = level.vehicle5x2Count[i];

        }

        gridSize.x = gridLength;
        gridSize.y = gridLength;
        vehicleDataList = new List<VehicleData>();
        vehicleGrid = new int[gridLength][];
        for (int i = 0; i < gridLength; i++)
        {
            vehicleGrid[i] = new int[gridLength];
            for (int j = 0; j < gridLength; j++)
            {
                vehicleGrid[i][j] = -1;
            }
        }

        grid = new bool[gridSize.x][];
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = new bool[gridSize.y];
        }
        directionGrid = new VehicleDirection[gridSize.x][];
        for (int i = 0; i < directionGrid.Length; i++)
        {
            directionGrid[i] = new VehicleDirection[gridSize.y];
        }

        gridCenter = new Vector2Int(gridSize.x / 2, gridSize.y / 2);

        checker = new bool[gridSize.x][];
        for (int i = 0; i < grid.Length; i++)
        {
            checker[i] = new bool[gridSize.y];
        }

        int count = 0;

        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[0].Length; j++)
            {
                checker[i][j] = ((i - gridCenter.x) * Mathf.Sign(i - gridCenter.x) + (j - gridCenter.y) * Mathf.Sign(j - gridCenter.y)) > gridCenter.x;
                grid[i][j] = checker[i][j];
                if (checker[i][j]) count++;
            }
        }


        Vector2Int currentPos = new Vector2Int(gridCenter.x - 1, gridCenter.y);
        Vector2Int currentDirection = new(1, 1);

        (colorIndex, vehicleIndex) = PickNextCar();
        vehicleSize = Vehicle.GetVehicleSizeByIndex(vehicleIndex);
        SpawnVehicle(gridCenter, tempDirection, colorIndex, vehicleIndex);
        SetVehicleGrid(gridCenter, tempDirection, vehicleSize, true);
        remainingVehicles[colorIndex][vehicleIndex]--;
        checker[gridCenter.x][gridCenter.y] = true;


        int maxIterationCount = 10000;
        int exitCount = 0;
        int remainDist = 2;
        int lastDist = 1;
        int iterCount = 0;
        for (int i = 0; i < count;)
        {
            exitCount++;
            if(exitCount > maxIterationCount)
            {
                Debug.LogFormat("Iter Exit Occured! Level {0}", level.level);
                break;
            }
            if (grid[currentPos.x][currentPos.y])                                   // If vehicle is already placed
            {
                checker[currentPos.x][currentPos.y] = true;
                remainDist--;
                if (remainDist == 0)
                {
                    currentDirection = ChangeDirection(currentDirection);
                    if (currentDirection.x == 1 && currentDirection.y == 1)
                    {
                        lastDist++;
                        remainDist = lastDist;
                    }
                    else if (currentDirection.x == -1 && currentDirection.y == 0)
                    {
                        remainDist = 1;
                    }
                    else
                    {
                        remainDist = lastDist;
                    }
                }
                currentPos += currentDirection;
                i++;
                continue;
            }
            (colorIndex, vehicleIndex) = PickNextCar();                           // Random Pick Vehicle
            if (colorIndex == -1 && vehicleIndex == -1) break;
            vehicleSize = Vehicle.GetVehicleSizeByIndex(vehicleIndex);

            tempDirection = GetVehicleDirectionFromPosition(currentPos);                    // Random Pick Direction

            if (IsValidVehicle(currentPos, tempDirection, vehicleSize))              // If vehicle can be placed
            {
                SpawnVehicle(currentPos, tempDirection, colorIndex, vehicleIndex);
                SetVehicleGrid(currentPos, tempDirection, vehicleSize, true);

                remainingVehicles[colorIndex][vehicleIndex]--;
                checker[currentPos.x][currentPos.y] = true;
                remainDist--;
                if (remainDist == 0)
                {
                    currentDirection = ChangeDirection(currentDirection);
                    if (currentDirection.x == 1 && currentDirection.y == 1)
                    {
                        lastDist++;
                        remainDist = lastDist;
                    }
                    else if (currentDirection.x == -1 && currentDirection.y == 0)
                    {
                        remainDist = 1;
                    }
                    else
                    {
                        remainDist = lastDist;
                    }

                }
                currentPos += currentDirection;
                i++;
            }
            else
            {
                iterCount++;
                i++;
                if (iterCount > 100)
                {
                    iterCount = 0;
                    checker[currentPos.x][currentPos.y] = true;
                    remainDist--;
                    if (remainDist == 0)
                    {
                        currentDirection = ChangeDirection(currentDirection);
                        if (currentDirection.x == 1 && currentDirection.y == 1)
                        {
                            lastDist++;
                            remainDist = lastDist;
                        }
                        else if (currentDirection.x == -1 && currentDirection.y == 0)
                        {
                            remainDist = 1;
                        }
                        else
                        {
                            remainDist = lastDist;
                        }

                    }
                    currentPos += currentDirection;
                }
            }
        }
        level.vehicleDatas = vehicleDataList.ToArray();
        level.gridSize = gridSize;
        level.vehicleGrid = vehicleGrid;
    }



    private void SpawnVehicle(Vector2Int position, VehicleDirection direction, int colorIndex, int vehicleIndex)
    {
        VehicleData vehicleData = new VehicleData();
        vehicleData.posInGrid = position;
        vehicleData.direction = direction;
        vehicleData.colorIndex = colorIndex;
        vehicleData.vehicleIndex = vehicleIndex;

        Vector2Int[] posList = GetVehicleGrids(position, direction, Vehicle.GetVehicleSizeByIndex(vehicleIndex));

        for (int i = 0; i < posList.Length; i++)
        {
            vehicleGrid[posList[i].x][posList[i].y] = vehicleDataList.Count;
        }

        vehicleDataList.Add(vehicleData);
    }

    public (int, int) PickNextCar()
    {
        int nextColor = 0, nextVehicleSize = 0;

        bool isValid = false;
        bool isAllZero = true;
        for (int i = 0; i < remainingVehicles.Length; i++)
        {
            for (int j = 0; j < remainingVehicles[0].Length; j++)
            {
                if (remainingVehicles[i][j] > 0)
                {
                    isAllZero = false;
                    break;
                }
            }
        }
        if (isAllZero) return (-1, -1);

        int pickIterCount = 0;
        while (!isValid)
        {
            pickIterCount++;
            if(pickIterCount > 1000)
            {
                Debug.LogFormat("Pick Failed!");
                break;
            }
            nextColor = random.Next(0, colorCount);
            nextVehicleSize = random.Next(0, 3);

            if (remainingVehicles[nextColor][nextVehicleSize] > 0)
            {
                isValid = true;
            }
        }
        return (nextColor, nextVehicleSize);
    }


    private VehicleDirection GetVehicleDirectionFromPosition(Vector2Int position)
    {
        VehicleDirection result = VehicleDirection.Up;
        bool isValid = false;
        int iterCount = 0;
        while (!isValid)
        {
            iterCount++;
            if(iterCount > 400)
            {
                break;
            }
            if (position.x > gridCenter.x && position.y > gridCenter.y)
            {
                if (random.NextDouble() > directionFlipRate)
                {
                    result = VehicleDirection.Left;
                }
                else
                {
                    result = VehicleDirection.Down;
                }
            }
            else if (position.x < gridCenter.x && position.y > gridCenter.y)
            {
                if (random.NextDouble() > directionFlipRate)
                {
                    result = VehicleDirection.Down;
                }
                else
                {
                    result = VehicleDirection.Right;
                }
            }
            else if (position.x > gridCenter.x && position.y < gridCenter.y)
            {
                if (random.NextDouble() > directionFlipRate)
                {
                    result = VehicleDirection.Up;
                }
                else
                {
                    result = VehicleDirection.Left;
                }
            }
            else if (position.x < gridCenter.x && position.y < gridCenter.y)
            {
                if (random.NextDouble() > directionFlipRate)
                {
                    result = VehicleDirection.Right;
                }
                else
                {
                    result = VehicleDirection.Up;
                }
            }
            else if (position.x == gridCenter.x)
            {
                if (random.NextDouble() > directionFlipRate)
                {
                    result = (random.NextDouble() > 0.5f) ? VehicleDirection.Left : VehicleDirection.Right;
                }
                else
                {
                    result = (position.y < gridCenter.y) ? VehicleDirection.Up : VehicleDirection.Down;
                }
            }
            else
            {
                if (random.NextDouble() > directionFlipRate)
                {
                    result = (random.NextDouble() > 0.5f) ? VehicleDirection.Up : VehicleDirection.Down;
                }
                else
                {
                    result = (position.x > gridCenter.x) ? VehicleDirection.Left : VehicleDirection.Right;
                }
            }

            isValid = IsValidDirectionInGrid(position, result);
        }

        return result;
    }

    private bool IsValidDirectionInGrid(Vector2Int position, VehicleDirection direction)
    {
        Vector2Int nextPos = position + GetVehicleDirection(direction);
        if (!CheckBounds(grid, nextPos)) return true;
        switch (direction)
        {
            case VehicleDirection.Up:
                if (directionGrid[nextPos.x][nextPos.y] == VehicleDirection.Down)
                    return false;
                break;
            case VehicleDirection.Down:
                if (directionGrid[nextPos.x][nextPos.y] == VehicleDirection.Up)
                    return false;
                break;
            case VehicleDirection.Left:
                if (directionGrid[nextPos.x][nextPos.y] == VehicleDirection.Right)
                    return false;
                break;
            case VehicleDirection.Right:
                if (directionGrid[nextPos.x][nextPos.y] == VehicleDirection.Left)
                    return false;
                break;
        }

        return true;
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
        Vector2Int dir = GetVehicleDirection(direction);

        Vector2Int currentPos = vehiclePosition;
        for (int i = 0; i < size; i++)
        {
            grids[i] = new Vector2Int(currentPos.x, currentPos.y);
            currentPos += dir;
        }

        return grids;
    }

    public Vector2Int GetVehicleDirection(VehicleDirection direction)
    {
        Vector2Int dir = new Vector2Int(0, 1);

        switch (direction)
        {
            case VehicleDirection.Up:
                dir.x = 0;
                dir.y = -1;
                break;
            case VehicleDirection.Down:
                dir.x = 0;
                dir.y = 1;
                break;
            case VehicleDirection.Left:
                dir.x = 1;
                dir.y = 0;
                break;
            case VehicleDirection.Right:
                dir.x = -1;
                dir.y = 0;
                break;
        }
        return dir;
    }


    private bool CheckBounds(bool[][] checker, Vector2Int position)
    {
        if (position.x < 0 || position.x >= checker.Length || position.y < 0 || position.y >= checker[0].Length ||
            (((position.x - gridCenter.x) * Mathf.Sign(position.x - gridCenter.x) + (position.y - gridCenter.y) * Mathf.Sign(position.y - gridCenter.y)) > gridCenter.x))
            return false;
        return true;
    }


    private Vector2Int ChangeDirection(Vector2Int direction)
    {
        Vector2Int changedDirection = direction;
        if (direction.x == 1 && direction.y == 1)
        {
            changedDirection.y = -1;
        }
        else if (direction.x == 1 && direction.y == -1)
        {
            changedDirection.x = -1;
        }
        else if (direction.x == -1 && direction.y == 1)
        {
            changedDirection.x = 1;
        }
        else if (direction.x == -1 && direction.y == -1)
        {
            changedDirection.y = 0;
        }
        else if (direction.x == -1 && direction.y == 0)
        {
            changedDirection.y = 1;
        }
        return changedDirection;
    }

    private bool IsValidVehicle(Vector2Int position, VehicleDirection direction, int vehicleSize)
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

}
