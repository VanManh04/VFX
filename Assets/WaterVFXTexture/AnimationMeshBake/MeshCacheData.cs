using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public struct MeshData
{
    public Vector3[] vertices;
}


[CreateAssetMenu(fileName = "MeshCacheData", menuName = "ScriptableObjects/Mesh Cache Data")]
public class MeshCacheData : ScriptableObject
{
    public List<MeshData> _data;

    #region <============================ RLE =============================>

    

    #endregion

    #region <=================================Delta Compression============================>

    [ContextMenu("DecompressData Zip")]
    public void CompressDataNen()
    {
        if (_data.Count == 0)
        {
            return;
        }

        for (int data = 0; data < _data.Count; data++)
        {
            List<Vector3> deltaData = new List<Vector3>();
            Vector3 firstFrame = _data[data].vertices[0];
            deltaData.Add(firstFrame);

            for (int i = 1; i < _data[data].vertices.Length; i++)
            {
                Vector3 currentFrame = _data[data].vertices[i];
                Vector3 previousFrame = _data[data].vertices[i - 1];

                Vector3 deltaFrame = currentFrame - previousFrame;

                deltaData.Add(deltaFrame);
            }

            // Cập nhật lại _data với các delta frames
            for (int i = 0; i < _data[data].vertices.Length; i++)
            {
                _data[data].vertices[i] = deltaData[i];
            }
        }
    }

    [ContextMenu("DecompressData UnZip")]
    public void DecompressDataGiaiNen()
    {
        if (_data.Count == 0)
        {
            return;
        }

        for (int data = 0; data < _data.Count; data++)
        {
            List<Vector3> decompressedData = new List<Vector3>();
            Vector3 firstFrame = _data[data].vertices[0];
            decompressedData.Add(firstFrame);

            for (int i = 1; i < _data[data].vertices.Length; i++)
            {
                Vector3 currentDeltaFrame = _data[data].vertices[i];
                Vector3 previousFrame = decompressedData[i - 1];

                Vector3 decompressedFrame = previousFrame + currentDeltaFrame;
                decompressedData.Add(decompressedFrame);
            }

            // Cập nhật lại _data với dữ liệu giải nén
            for (int i = 0; i < _data[data].vertices.Length; i++)
            {
                _data[data].vertices[i] = decompressedData[i];
            }
        }
    }
    #endregion
}
