using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleBurster : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private ParticleSystem particle;
    public int poolIdx;


    public void Play()
    {
        particle.Play();

        DOVirtual.DelayedCall(4f, () =>
        {
            PoolManager.Instance.Put(poolIdx, gameObject);
        });
    }
}
