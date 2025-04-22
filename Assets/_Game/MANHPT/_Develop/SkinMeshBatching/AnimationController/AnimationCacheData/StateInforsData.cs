using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StateInforsData", menuName = "ScriptableObjects/StateInforsData", order = 1)]
[PreferBinarySerialization]
public class StateInforsData : ScriptableObject , ICacheData<StateInforsData.StateInfo>
{
    [field: SerializeField]
    public List<StateInfo> StateInfors { get; set; }

    public StateInfo GetStateInfo(string stateName)
    {
        if (StateInfors == null || StateInfors.Count == 0) return default;
        
        var stateInfo = StateInfors.Find(x => x.stateName == stateName);

        if (stateInfo.Equals((StateInfo)default)) return default;

        return stateInfo;
    }

    public StateInfo GetDefaultStateInfo()
    {
        if (StateInfors == null || StateInfors.Count == 0) return default;
        return StateInfors[0];
    }

    [Serializable]
    public struct StateInfo
    {
        public string stateName;
        public VAT_Utilities.AnimationInfo animationInfo;
    }
}




