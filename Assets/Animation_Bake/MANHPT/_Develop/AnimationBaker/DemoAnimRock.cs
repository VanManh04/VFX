using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class DemoAnimRock : MonoBehaviour
{
    [SerializeField] private Rigidbody[] _rigidBodies;
    [SerializeField] private Transform   _center;
    [SerializeField] private float       _dropForce      = 100f;
    [SerializeField] private float       _explosionForce = 100f;
    [SerializeField] private float       _explosionRadius = 100f;
    [SerializeField] private float       _upwardsModifier = 1.2f;

    [UsedImplicitly]
    public void StartRecord()
    {
        StartCoroutine(Fall());
    }
    
    private IEnumerator Fall()
    {
        _rigidBodies[0].isKinematic = false;
        _rigidBodies[0].AddForce(Vector3.down * _dropForce, ForceMode.Impulse);
        yield return new WaitForSeconds(1f);
        for(var i = 1; i < _rigidBodies.Length; i++)
        {
            _rigidBodies[i].isKinematic = false;
            _rigidBodies[i].AddExplosionForce( _explosionForce, _center.position, _explosionRadius, _upwardsModifier, ForceMode.Impulse);
        }
    }
}
