using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Level Data", menuName = "Level Data", order = int.MaxValue)]
public class LevelData : ScriptableObject
{
    public int level;

    public int difficulty;
    public int colorCount;

    public int[] vehicle2x2Count;
    public int[] vehicle3x2Count;
    public int[] vehicle5x2Count;
    public int[] customerCount;

    public int totalCustomerCount;
    public int totalVehicleCount;

    public int availableVehicleSpace;
    public int leastParkableSpace;

    public int[] customerColorLine;

    public int totalGridCount;
    public int fullSpaceCount;

    public float directionFlipRate;

    public int seed;

    public VehicleData[] vehicleDatas;
    public Vector2Int gridSize;
    public int[][] vehicleGrid;

    public int[] answer;
    public int[] pickAnswer;

    public string themaSceneName;
}


[Serializable]
public class VehicleData
{
    public Vector2Int posInGrid;
    public VehicleDirection direction;
    public int colorIndex;
    public int vehicleIndex;
}