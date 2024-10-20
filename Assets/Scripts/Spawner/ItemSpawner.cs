using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Random = System.Random;

public class ItemSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private BoxCollider boxBounds;
    [SerializeField] private int seed;

    [SerializeField] private List<GameObject> spawnPrefabs = new List<GameObject>();


    private Random random;
    private Vector3 leftTopVert;
    private Vector3 size;


    public void Start()
    {
        random = new Random(seed);

        leftTopVert = boxBounds.center - boxBounds.size / 2f;
        size = boxBounds.size;
    }


    public Vector3 GetRandomPoint()
    {
        return new Vector3((float)(leftTopVert.x + random.NextDouble() * size.x),
            (float)(leftTopVert.y + random.NextDouble() * size.y),
            (float)(leftTopVert.z + random.NextDouble() * size.z));
    }


}
