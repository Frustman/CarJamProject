
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance = null;



    [SerializeField] private List<GameObject> poolingPrefabs;
    private List<List<GameObject>> pools;
    private List<List<GameObject>> enabledObjectsPools;

    private Dictionary<GameObject, int> poolingKey = new();

    private void Awake()
    {
        if(null == Instance)
        {
            Instance = this;
        } else
        {
            Destroy(this);
        }

        pools = new List<List<GameObject>>();
        enabledObjectsPools = new List<List<GameObject>>();
        poolingPrefabs = new();
    }

    public int AssignPoolingObject(GameObject prefab)
    {
        if (poolingKey.ContainsKey(prefab))
        {
            return poolingKey[prefab];
        }

        int key = poolingPrefabs.Count;

        poolingPrefabs.Add(prefab);
        poolingKey.Add(prefab, key);
        pools.Add(new List<GameObject>());
        enabledObjectsPools.Add(new List<GameObject>());

        return key;
    }
    public GameObject Get(GameObject prefab)
    {
        GameObject item;
        if (!poolingKey.ContainsKey(prefab))
        {
            Debug.Log("There is no prefab as pooled");
            return null;
        }
        int index = poolingKey[prefab];
        if (pools[index].Count > 0)
        {
            item = pools[index][0];
            pools[index].RemoveAt(0);

            if (pools[index].Contains(item))
            {
                pools[index].Remove(item);
            }
            item.SetActive(true);
        }else
        {
            item = Instantiate(poolingPrefabs[index]);
        }
        enabledObjectsPools[index].Add(item);
        return item;
    }
    public int GetPoolIndex(GameObject prefab)
    {
        if (poolingKey.ContainsKey(prefab))
            return poolingKey[prefab];
        else return -1;
    }

    public void Put(GameObject prefab, GameObject item)
    {
        int index = poolingKey[prefab];
        Put(index, item);
    }

    public void Put(int index, GameObject item)
    {
        item.SetActive(false);
        if (!pools[index].Contains(item))
            pools[index].Add(item);
        if (enabledObjectsPools[index].Contains(item))
        {
            enabledObjectsPools[index].Remove(item);
        }
    }

    List<GameObject> objectsToReset;
    public void ResetObjects()
    {
        for (int i = 0; i < enabledObjectsPools.Count; i++)
        {
            objectsToReset = new List<GameObject>(enabledObjectsPools[i]);
            foreach (var obj in objectsToReset)
            {
                Put(i, obj);
            }
            enabledObjectsPools[i].Clear();
        }
    }
}
