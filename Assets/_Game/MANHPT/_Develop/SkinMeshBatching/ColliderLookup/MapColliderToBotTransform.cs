using System;
using System.Collections.Generic;
using UnityEngine;

public class MapColliderToBotTransform : MonoBehaviour
{
    public static Dictionary<int, Transform> lookup = new();
    private void Awake()
    {
        lookup.Clear();
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            lookup.Add(col.gameObject.GetInstanceID(), transform);
        }
    }
}