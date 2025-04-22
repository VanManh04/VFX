using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICacheData<T>
{
    public List<T> StateInfors { get; set; }
    
    T GetStateInfo(string stateName);
    
    T GetDefaultStateInfo();
}
