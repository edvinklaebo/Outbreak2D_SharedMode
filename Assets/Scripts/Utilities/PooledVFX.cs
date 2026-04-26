using UnityEngine;

/// <summary>
/// Attach to any pooled VFX particle system. Returns itself to the ObjectPool
/// when the particle effect finishes playing.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class PooledVFX : MonoBehaviour
{
    [SerializeField] private GameObject _prefabKey; // the prefab this instance was created from

    private ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        _ps.Play();
    }

    private void Update()
    {
        if (!_ps.IsAlive(withChildren: true))
        {
            if (_prefabKey != null)
                ObjectPool.Instance?.Release(_prefabKey, gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
