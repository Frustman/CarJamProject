using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    LevelData level;

    private int lastColorCount = -1;
    private int totalCustomerCount = 0;
    private int totalVehicleCount = 0;
    private int totalGridCount = 0;


    private System.Random random;
    private int minCustomerLineCount = 2;
    private int maxCustomerLineCount = 8;
    private float directionFlipRate = 0.5f;

    private int[] customerCounts;

    private Texture2D arrowTex;
    private Texture2D frontArrowTex;
    private Texture2D middleArrowTex;
    private Texture2D backArrowTex;

    private List<Vector2Int> selectedGrid;
    private List<VehicleData> selectedVehicle;


    private static readonly Color[] colorPalette =
    {
        Color.red,
        new Color(0, 136f / 255f, 255f),
        Color.green,
        Color.yellow,
        new Color(102f/ 255f, 0f, 1f),
        new Color(255f / 255f, 140f / 255f, 0),
        new Color(171f / 255f, 242f / 255f, 0),
        Color.gray,
    };

    private void OnEnable()
    {
        level = (LevelData)target;
        random = new System.Random();

        arrowTex = (Texture2D)Resources.Load("leftArrow");
        frontArrowTex = (Texture2D)Resources.Load("frontArrow");
        middleArrowTex = (Texture2D)Resources.Load("middleArrow");
        backArrowTex = (Texture2D)Resources.Load("backArrow");

        selectedGrid = new List<Vector2Int>();
        selectedVehicle = new List<VehicleData>();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Level", EditorStyles.boldLabel, GUILayout.Width(50));
        level.level = EditorGUILayout.IntField(level.level, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20f);


        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Thema Scene Name", EditorStyles.boldLabel, GUILayout.Width(120));
        level.themaSceneName = EditorGUILayout.TextField(level.themaSceneName, GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space(20f);


        #region Color Settings

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(new GUIContent("Colors", "해당 스테이지에서 나올 색의 개수를 정합니다."), EditorStyles.boldLabel, GUILayout.Width(50));
        int newColorCount = EditorGUILayout.IntField(level.colorCount, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(new GUIContent("Available Space Count", "해당 스테이지에서 사용 가능한 주차 공간의 수를 정합니다."), EditorStyles.boldLabel, GUILayout.Width(150));
        level.availableVehicleSpace = EditorGUILayout.IntField(level.availableVehicleSpace, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(new GUIContent("Least Space Count", "실제로 깨기 위한 최소 주차 공간의 수를 정합니다."), EditorStyles.boldLabel, GUILayout.Width(150));
        level.leastParkableSpace = EditorGUILayout.IntField(level.leastParkableSpace, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (newColorCount != level.colorCount)
        {
            Undo.RecordObject(level, "Change Color Count");

            Color[] newColors = new Color[newColorCount];
            int[] vehicel2x2Count = new int[newColorCount];
            int[] vehicel3x2Count = new int[newColorCount];
            int[] vehicle5x2Count = new int[newColorCount];
            level.customerCount = new int[newColorCount];
            for (int i = 0; i < Mathf.Min(level.colorCount, newColorCount); i++)
            {
                vehicel2x2Count[i] = level.vehicle2x2Count[i];
                vehicel3x2Count[i] = level.vehicle3x2Count[i];
                vehicle5x2Count[i] = level.vehicle5x2Count[i];
            }

            level.colorCount = newColorCount;
            lastColorCount = newColorCount;

            level.vehicle2x2Count = vehicel2x2Count;
            level.vehicle3x2Count = vehicel3x2Count;
            level.vehicle5x2Count = vehicle5x2Count;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Load Default Colors", GUILayout.Width(200)))
        {
            level.colorCount = 7;
            lastColorCount = 7;

            level.vehicle2x2Count = new int[level.colorCount];
            level.vehicle3x2Count = new int[level.colorCount];
            level.vehicle5x2Count = new int[level.colorCount];
            level.customerCount = new int[level.colorCount];



            level.vehicleDatas = null;
            level.gridSize = Vector2Int.zero;
            level.vehicleGrid = null;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (level.colorCount > 0)
        {
            float inspectorWidth = EditorGUIUtility.currentViewWidth;
            int colorsPerRow = Mathf.FloorToInt(inspectorWidth / 120);

            EditorGUILayout.BeginVertical();
            
            for (int i = 0; i < level.colorCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(new GUIContent("2x2 Vehicle Counts", "4인승 차량의 개수를 정합니다."), GUILayout.Width(150));
                level.vehicle2x2Count[i] = EditorGUILayout.IntField(level.vehicle2x2Count[i], GUILayout.Width(100));
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(new GUIContent("3x2 Vehicle Counts", "6인승 차량의 개수를 정합니다."), GUILayout.Width(150));
                level.vehicle3x2Count[i] = EditorGUILayout.IntField(level.vehicle3x2Count[i], GUILayout.Width(100));
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(new GUIContent("5x2 Vehicle Counts", "10인승 차량의 개수를 정합니다."), GUILayout.Width(150));
                level.vehicle5x2Count[i] = EditorGUILayout.IntField(level.vehicle5x2Count[i], GUILayout.Width(100));
                EditorGUILayout.EndVertical();

                level.customerCount[i] = level.vehicle2x2Count[i] * 4 + level.vehicle3x2Count[i] * 6 + level.vehicle5x2Count[i] * 10;


                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(new GUIContent("Customer Counts", "해당 색깔의 손님 수를 나타냅니다."), GUILayout.Width(150));
                GUI.enabled = false;
                EditorGUILayout.IntField(level.customerCount[i], GUILayout.Width(100));
                GUI.enabled = true;
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20f);

            totalCustomerCount = 0;
            totalVehicleCount = 0;
            totalGridCount = 0;
            for (int i = 0; i < level.colorCount; i++)
            {
                totalCustomerCount += level.vehicle2x2Count[i] * 4;
                totalCustomerCount += level.vehicle3x2Count[i] * 6;
                totalCustomerCount += level.vehicle5x2Count[i] * 10;

                totalVehicleCount += level.vehicle2x2Count[i];
                totalVehicleCount += level.vehicle5x2Count[i];
                totalGridCount += level.vehicle2x2Count[i] * Vehicle.GetVehicleSizeByIndex(0) + level.vehicle3x2Count[i] * Vehicle.GetVehicleSizeByIndex(1) + level.vehicle5x2Count[i] * Vehicle.GetVehicleSizeByIndex(2);
            }
            level.totalCustomerCount = totalCustomerCount;
            level.totalVehicleCount = totalVehicleCount;
            level.totalGridCount = totalGridCount;


            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Total Customer Count", GUILayout.Width(150));
            GUI.enabled = false;
            EditorGUILayout.IntField(level.totalCustomerCount, GUILayout.Width(50));
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Total Vehicle Count", GUILayout.Width(150));
            GUI.enabled = false;
            EditorGUILayout.IntField(level.totalVehicleCount, GUILayout.Width(50));
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Total Grid Count", GUILayout.Width(150));
            GUI.enabled = false;
            EditorGUILayout.IntField(level.totalGridCount, GUILayout.Width(50));
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Expected Grid", GUILayout.Width(150));
            GUI.enabled = false;
            EditorGUILayout.IntField(GetGridSize(level.totalGridCount), GUILayout.Width(50));
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(20f);


            #endregion

            #region Customer Line Settings

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Customer Line Settings", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);


            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(new GUIContent("Min Customer Line Count", "같은 색의 사람이 최소 얼마나 이어져야 하는지 정합니다."), GUILayout.Width(150));
            minCustomerLineCount = EditorGUILayout.IntField(minCustomerLineCount, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(new GUIContent("Max Customer Line Count", "같은 색의 사람이 최대 얼마나 이어질 수 있는지 정합니다."), GUILayout.Width(150));
            maxCustomerLineCount = EditorGUILayout.IntField(maxCustomerLineCount, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(new GUIContent("Random Seed", "같은 시드 값, 같은 파라미터를 가지고 있는 경우 같은 결과가 나옵니다."), GUILayout.Width(150));
            level.seed = EditorGUILayout.IntField(level.seed, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate With Randomize", GUILayout.Width(200)))
            {
                level.seed = random.Next(10000);
                level.customerColorLine = GenerateCustomerLine(level.colorCount);
            }
            if (GUILayout.Button("Generate With Seed", GUILayout.Width(200)))
            {
                level.customerColorLine = GenerateCustomerLine(level.colorCount);
            }
            if (GUILayout.Button("Generate With Grid", GUILayout.Width(200)))
            {
                level.customerColorLine = GenerateCustomerLineFromGrid(level);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (level.customerColorLine != null && level.customerColorLine.Length > 0)
            {
                EditorGUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Customer Color Line", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10f);
                float boxSize = 10;
                float totalWidth = 200;
                float spacing = 0;

                colorsPerRow = Mathf.FloorToInt(totalWidth / (boxSize + spacing));

                EditorGUILayout.BeginVertical();
                customerCounts = new int[level.colorCount];
                for(int i = 0; i < level.colorCount; i++)
                {
                    customerCounts[i] = 0;
                }

                for (int i = 0; i < level.customerColorLine.Length; i++)
                {
                    if (i % colorsPerRow == 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                    }

                    Color color = colorPalette[level.customerColorLine[i]];
                    Rect rect = GUILayoutUtility.GetRect(boxSize, boxSize);

                    EditorGUI.DrawRect(rect, color);
                    customerCounts[level.customerColorLine[i]]++;

                    Color borderColor = Color.black;
                    Handles.BeginGUI();
                    Handles.color = borderColor;
                    Handles.DrawSolidRectangleWithOutline(rect, Color.clear, borderColor);
                    Handles.EndGUI();

                    if (i % colorsPerRow == colorsPerRow - 1 || i == level.customerColorLine.Length - 1)
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.Space(20f);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int i = 0; i < level.colorCount; i++)
                {
                    EditorGUILayout.Space(20f);
                    EditorGUI.DrawRect(GUILayoutUtility.GetRect(10f, 10f), colorPalette[i]);
                    EditorGUILayout.IntField(customerCounts[i], GUILayout.Width(50));
                }

                EditorGUILayout.Space(20f);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            #endregion


            #region Grid Settings

            /** 
             * To do :  랜덤 시드가 고정이다 보니, 차량의 배열을 미리 확인 가능할듯? 
             *          차량의 배열을 미리 확인하고 에디터에서 수정하는 것이 미래적으로 좋을 듯 함.
            **/

            EditorGUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Car Grid Settings", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);


            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(new GUIContent("Direction Flip Rate", "값이 커질수록, 흐름과 다른 방향이 나올 가능성이 증가합니다."), GUILayout.Width(150));
            directionFlipRate = EditorGUILayout.FloatField(directionFlipRate, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate With Randomize", GUILayout.Width(200)))
            {
                level.seed = random.Next(10000);

                level.customerColorLine = GenerateCustomerLine(level.colorCount);
                GenerateGridData();
            }
            if (GUILayout.Button("Generate With Seed", GUILayout.Width(200)))
            {
                GenerateGridData();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);

            if (level.vehicleDatas != null && level.vehicleDatas.Length > 0 && level.gridSize.x > 0 && level.gridSize.y > 0)
            {
                if(level.vehicleGrid == null)
                {
                    int gridLength = GetGridSize(level.totalGridCount) + 6;

                    level.vehicleGrid = new int[gridLength][];
                    for (int i = 0; i < gridLength; i++)
                    {
                        level.vehicleGrid[i] = new int[gridLength];
                        for (int j = 0; j < gridLength; j++)
                        {
                            level.vehicleGrid[i][j] = -1;
                        }
                    }
                    for(int i = 0; i < level.vehicleDatas.Length; i++)
                    {
                        VehicleData vehicle = level.vehicleDatas[i];
                        Vector2Int[] posList = GetVehicleGrids(vehicle.posInGrid, vehicle.direction, Vehicle.GetVehicleSizeByIndex(vehicle.vehicleIndex));

                        for(int j = 0; j < posList.Length; j++)
                        {
                            level.vehicleGrid[posList[j].x][posList[j].y] = i;
                        }
                    }
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                
                for (int i = level.gridSize.x - 1; i >= 0; i--)
                {
                    GUILayout.Space(1f);
                    EditorGUILayout.BeginVertical();
                    for (int j = 0; j < level.gridSize.y; j++)
                    {
                        GUILayout.Space(1f);
                        VehicleData data = null;
                        if (level.vehicleGrid[i][j] != -1)
                        {
                            data = level.vehicleDatas[level.vehicleGrid[i][j]];
                        }

                        Rect rect = GUILayoutUtility.GetRect(40f, 40f);
                        Vector2Int gridVec = new Vector2Int(i, j);

                        Event e = Event.current;
                        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                        {
                            if (e.shift)
                            {
                                if(selectedGrid.Contains(gridVec))
                                {
                                    selectedGrid.Remove(gridVec);
                                    if (level.vehicleGrid[i][j] != -1)
                                        selectedVehicle.Remove(level.vehicleDatas[level.vehicleGrid[i][j]]);
                                }
                                else
                                {
                                    selectedGrid.Add(gridVec);
                                    if (level.vehicleGrid[i][j] != -1)
                                        selectedVehicle.Add(level.vehicleDatas[level.vehicleGrid[i][j]]);
                                }
                            } else
                            {
                                selectedGrid.Clear();
                                selectedGrid.Add(gridVec);
                                if (level.vehicleGrid[i][j] != -1)
                                    selectedVehicle.Add(level.vehicleDatas[level.vehicleGrid[i][j]]);
                                e.Use();
                            }
                        }

                        Color color = Color.red;
                        if(data != null) { 
                            color = colorPalette[data.colorIndex];
                            EditorGUI.DrawRect(rect, color);
                            if (arrowTex != null)
                            {
                                if(Vector2Int.Distance(data.posInGrid, new Vector2Int(i, j)) == 0)
                                {
                                    switch (data.direction)
                                    {
                                        case VehicleDirection.Right:
                                            DrawRotatedTexture(rect, backArrowTex, 180f);
                                            break;
                                        case VehicleDirection.Left:
                                            DrawRotatedTexture(rect, backArrowTex, 0);
                                            break;
                                        case VehicleDirection.Up:
                                            DrawRotatedTexture(rect, backArrowTex, 90f);
                                            break;
                                        case VehicleDirection.Down:
                                            DrawRotatedTexture(rect, backArrowTex, 270f);
                                            break;
                                    }
                                }
                                else if (data.vehicleIndex == 2 && Vector2Int.Distance(data.posInGrid, new Vector2Int(i, j)) == 1)
                                {
                                    switch (data.direction)
                                    {
                                        case VehicleDirection.Right:
                                            DrawRotatedTexture(rect, middleArrowTex, 180f);
                                            break;
                                        case VehicleDirection.Left:
                                            DrawRotatedTexture(rect, middleArrowTex, 0);
                                            break;
                                        case VehicleDirection.Up:
                                            DrawRotatedTexture(rect, middleArrowTex, 90f);
                                            break;
                                        case VehicleDirection.Down:
                                            DrawRotatedTexture(rect, middleArrowTex, 270f);
                                            break;
                                    }
                                } else if ((data.vehicleIndex == 2 && Vector2Int.Distance(data.posInGrid, new Vector2Int(i, j)) == 2) ||
                                            (data.vehicleIndex != 2 && Vector2Int.Distance(data.posInGrid, new Vector2Int(i, j)) == 1))
                                {
                                    switch (data.direction)
                                    {
                                        case VehicleDirection.Right:
                                            DrawRotatedTexture(rect, frontArrowTex, 180f);
                                            break;
                                        case VehicleDirection.Left:
                                            DrawRotatedTexture(rect, frontArrowTex, 0);
                                            break;
                                        case VehicleDirection.Up:
                                            DrawRotatedTexture(rect, frontArrowTex, 90f);
                                            break;
                                        case VehicleDirection.Down:
                                            DrawRotatedTexture(rect, frontArrowTex, 270f);
                                            break;
                                    }
                                }
                            }
                        }

                        Color borderColor = (selectedGrid.Contains(gridVec)) ? Color.white : Color.black;
                        Handles.BeginGUI();
                        Handles.color = borderColor;
                        Handles.DrawSolidRectangleWithOutline(rect, Color.clear, borderColor);
                        Handles.EndGUI();


                    }
                    EditorGUILayout.EndVertical();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Yet there is no grid!", GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

            }

            if(selectedGrid.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Selected Vehicle " + selectedGrid[0].x + ", " + selectedGrid[0].y, GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Flip Vehicle Direction", GUILayout.Width(200)))
                {
                    for(int i = 0; i < selectedGrid.Count; i++)
                    {
                        if (level.vehicleGrid[selectedGrid[i].x][selectedGrid[i].y] != -1)
                        {
                            VehicleData vehicleData = level.vehicleDatas[level.vehicleGrid[selectedGrid[i].x][selectedGrid[i].y]];
                            switch (vehicleData.direction)
                            {
                                case VehicleDirection.Up:
                                    if (vehicleData.vehicleIndex > 0)
                                        vehicleData.posInGrid.y -= 1;
                                    vehicleData.direction = VehicleDirection.Down;
                                    break;
                                case VehicleDirection.Down:
                                    if (vehicleData.vehicleIndex > 0)
                                        vehicleData.posInGrid.y += 1;
                                    vehicleData.direction = VehicleDirection.Up;
                                    break;
                                case VehicleDirection.Left:
                                    if (vehicleData.vehicleIndex > 0)
                                        vehicleData.posInGrid.x += 1;
                                    vehicleData.direction = VehicleDirection.Right;
                                    break;
                                case VehicleDirection.Right:
                                    if (vehicleData.vehicleIndex > 0)
                                        vehicleData.posInGrid.x -= 1;
                                    vehicleData.direction = VehicleDirection.Left;
                                    break;
                            }
                        }
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(50f);
            #endregion
        }
        else
        {
            if (lastColorCount != -1)
            {
                Undo.RecordObject(level, "Reset Colors");
                lastColorCount = -1;
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(level);
        }
    }

    #region Customer Line Func


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
        if (level.leastParkableSpace == 2)
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



    public int[] GenerateCustomerLine(int colorCount)
    {
        random = new System.Random(level.seed);
        int[] customerLine = new int[level.totalCustomerCount];
        int[] customerCounts = new int[level.colorCount];
        for(int i = 0; i < level.colorCount; i++)
        {
            customerCounts[i] = level.customerCount[i];
        }

        int remainingCustomerCount = level.totalCustomerCount;

        int currentColor = 0, currentCustomer;
        int counter = 0;

        while(remainingCustomerCount > 0)
        {
            (currentCustomer, currentColor) = PickColor(currentColor, customerCounts);
            for(int i = 0; i < currentCustomer; i++)
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
            currentColor = random.Next(0, level.colorCount);
            if (currentColor == beforeColor)
            {
                bool flag = false;
                for(int i = 0; i < level.colorCount; i++)
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
            } else
            {
                isValid = false;
            }
        }

        return (customerCount, currentColor);
    }




    #endregion


    #region Grid Func



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

    public void GenerateGridData()
    {
        random = new System.Random(level.seed);

        int gridLength = GetGridSize(level.totalGridCount) + 2;

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
            for(int j = 0; j < gridLength; j++)
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

        int remainDist = 2;
        int lastDist = 1;
        int iterCount = 0;
        for (int i = 0; i < count;)
        {
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
            } else
            {
                iterCount++;
                i++;
                if(iterCount > 100)
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

        while (!isValid)
        {
            nextColor = random.Next(0, level.colorCount);
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
        while (!isValid)
        {
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

    #endregion


    #region Utility


    void DrawRotatedTexture(Rect rect, Texture2D texture, float rotationAngle)
    {
        Vector2 pivot = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);

        Handles.BeginGUI();
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(rotationAngle, pivot);
        GUI.DrawTexture(rect, texture);
        GUI.matrix = matrixBackup;
        Handles.EndGUI();
    }


    #endregion
}
