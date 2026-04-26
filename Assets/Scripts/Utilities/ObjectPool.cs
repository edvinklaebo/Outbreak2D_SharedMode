using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool for GameObjects (muzzle flashes, impact VFX, etc.).
/// Objects are returned to the pool by disabling themselves (or by calling Release).
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    private class Pool
    {
        public GameObject Prefab;
        public int        InitialSize = 5;
    }

    [SerializeField] private List<Pool> _warmupPools = new();

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (Pool p in _warmupPools)
            Prewarm(p.Prefab, p.InitialSize);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Retrieve an inactive instance from the pool (spawns one if the pool is empty).</summary>
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null)
            return null;

        if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }

        GameObject obj = queue.Count > 0 ? queue.Dequeue() : Instantiate(prefab, transform);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>Return an object to the pool.</summary>
    public void Release(GameObject prefab, GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(false);
        obj.transform.SetParent(transform, worldPositionStays: false);

        if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }

        queue.Enqueue(obj);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null)
            return;

        if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }
    }
}
