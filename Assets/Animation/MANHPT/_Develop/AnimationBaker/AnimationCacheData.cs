using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationCacheData", menuName = "ScriptableObjects/Animation Cache Data")]
[PreferBinarySerialization]
public class AnimationCacheData : ScriptableObject
{
    [HideInInspector] [SerializeField] private byte[] _compressedData;
    [SerializeField] private List<FrameData> _data = new();
    public int FrameCount => _data.Count;

    private const float QUANTIZE_SCALE = 1024.0f;
    
    [ContextMenu("REMOVE")]    
    
    public void DELELTE_LE()
    {
        for (int i = 0; i < _data.Count/2; i++)
        {
            _data.RemoveAt(i * 2);
        }
    }
    
    
    public FrameData GetFrameData(int index)
    {
        return _data[index];
    }

    public void AddFrameData(FrameData frameData)
    {
        _data.Add(frameData);
    }

    public void ClearData()
    {
        _data.Clear();
    }

    public void SaveData()
    {
        CompressData();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    private void CompressData()
    {
        if (_data.Count == 0)
        {
            _compressedData = Array.Empty<byte>();
            return;
        }

        using var memoryStream = new MemoryStream();
        using var writer       = new BinaryWriter(memoryStream);

        // Write header information
        writer.Write(_data.Count); // Frame count
        var objectCount = _data[0].position.Length;
        writer.Write(objectCount); // Object count

        // Write first frame completely (keyframe)
        WriteFullFrame(writer, _data[0]);

        var deltaData = new List<FrameData>();

        // For subsequent frames, use delta compression
        for (var frameIndex = 1; frameIndex < _data.Count; frameIndex++)
        {
            var currentFrame  = _data[frameIndex];
            var previousFrame = _data[frameIndex - 1];

            var deltaFrame = new FrameData
            {
                position = new Vector3[objectCount],
                rotation = new Quaternion[objectCount]
            };

            for (var objectIndex = 0; objectIndex < objectCount; objectIndex++)
            {
                deltaFrame.position[objectIndex] = currentFrame.position[objectIndex] - previousFrame.position[objectIndex];
                deltaFrame.rotation[objectIndex] = Quaternion.Inverse(previousFrame.rotation[objectIndex]) *
                                                   currentFrame.rotation[objectIndex];
            }

            deltaData.Add(deltaFrame);
        }

        // Write delta frames
        for (var frameIndex = 1; frameIndex < _data.Count; frameIndex++)
        {
            var currentFrame = deltaData[frameIndex - 1];
            WriteDeltaFrame(writer, currentFrame);
        }

        _compressedData = memoryStream.ToArray();
    }

    private static void WriteFullFrame(BinaryWriter writer, FrameData frame)
    {
        // Write positions
        for (var i = 0; i < frame.position.Length; i++)
        {
            writer.Write(frame.position[i].x);
            writer.Write(frame.position[i].y);
            writer.Write(frame.position[i].z);
        }

        // Write rotations
        for (var i = 0; i < frame.rotation.Length; i++)
        {
            writer.Write(frame.rotation[i].x);
            writer.Write(frame.rotation[i].y);
            writer.Write(frame.rotation[i].z);
        }
    }

    private static void WriteDeltaFrame(BinaryWriter writer, FrameData frame)
    {
        // Write positions
        for (var i = 0; i < frame.position.Length; i++)
        {
            writer.Write((short)(frame.position[i].x * QUANTIZE_SCALE));
            writer.Write((short)(frame.position[i].y * QUANTIZE_SCALE));
            writer.Write((short)(frame.position[i].z * QUANTIZE_SCALE));
        }

        // Write rotations
        for (var i = 0; i < frame.rotation.Length; i++)
        {
            writer.Write((short)(frame.rotation[i].x * QUANTIZE_SCALE));
            writer.Write((short)(frame.rotation[i].y * QUANTIZE_SCALE));
            writer.Write((short)(frame.rotation[i].z * QUANTIZE_SCALE));
        }
    }

    public void DecompressData()
    {
        if (_compressedData == null || _compressedData.Length == 0)
        {
            _data.Clear();
            return;
        }

        _data.Clear();

        using var memoryStream = new MemoryStream(_compressedData);
        using var reader       = new BinaryReader(memoryStream);

        // Read header
        var frameCount  = reader.ReadInt32();
        var objectCount = reader.ReadInt32();

        // Read first frame (keyframe)
        FrameData firstFrame = new FrameData
        {
            position = new Vector3[objectCount],
            rotation = new Quaternion[objectCount]
        };

        for (var i = 0; i < objectCount; i++)
        {
            firstFrame.position[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        for (var i = 0; i < objectCount; i++)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            var w = Mathf.Sqrt(1 - (x * x + y * y + z * z));
            firstFrame.rotation[i] = new Quaternion(x, y, z, w);
        }

        _data.Add(firstFrame);

        // Read delta frames
        for (int frameIndex = 1; frameIndex < frameCount; frameIndex++)
        {
            FrameData previousFrame = _data[frameIndex - 1];
            FrameData currentFrame = new FrameData
            {
                position = new Vector3[objectCount],
                rotation = new Quaternion[objectCount]
            };

            var posIndex = 0;
            while (posIndex < objectCount)
            {
                var     x     = reader.ReadInt16() / QUANTIZE_SCALE;
                var     y     = reader.ReadInt16() / QUANTIZE_SCALE;
                var     z     = reader.ReadInt16() / QUANTIZE_SCALE;
                Vector3 delta = new Vector3(x, y, z);
                currentFrame.position[posIndex] = previousFrame.position[posIndex] + delta;
                posIndex++;
            }

            var rotIndex = 0;
            while (rotIndex < objectCount)
            {
                var        x     = reader.ReadInt16() / QUANTIZE_SCALE;
                var        y     = reader.ReadInt16() / QUANTIZE_SCALE;
                var        z     = reader.ReadInt16() / QUANTIZE_SCALE;
                var        w     = Mathf.Sqrt(1 - (x * x + y * y + z * z));
                Quaternion delta = new Quaternion(x, y, z, w);
                currentFrame.rotation[rotIndex] = previousFrame.rotation[rotIndex] * delta;
                rotIndex++;
            }

            _data.Add(currentFrame);
        }
    }

    [Serializable]
    public struct FrameData
    {
        public Vector3[]    position;
        public Quaternion[] rotation;
    }

    #region <============================ RLE =============================>

    [SerializeField] private List<Optimize> _positions = new List<Optimize>();
    [SerializeField] private List<Optimize> _rotations = new List<Optimize>();
    
    [ContextMenu("RLE Nen")]
    public void RLE_Zip()
    {
        for (int i = 0; i < _data[0].position.Length; i++)
        {
            List<float> positions_x = new List<float>();
            List<float> positions_y = new List<float>();
            List<float> positions_z = new List<float>();

            Optimize optimize = new Optimize();
            foreach (FrameData frameData in _data)
            {
                positions_x.Add(frameData.position[i].x);
                positions_y.Add(frameData.position[i].y);
                positions_z.Add(frameData.position[i].z);
            }

            CompressRLE(positions_x,ref optimize.RLE_x);
            CompressRLE(positions_y,ref optimize.RLE_y);
            CompressRLE(positions_z,ref optimize.RLE_z);
            _positions.Add(optimize);
        }
        
        
        for (int i = 0; i < _data[0].rotation.Length; i++)
        {
            List<float> rotations_x = new List<float>();
            List<float> rotations_y = new List<float>();
            List<float> rotations_z = new List<float>();

            Optimize optimize = new Optimize();
            foreach (FrameData frameData in _data)
            {
                Vector3 eulerAngles = frameData.rotation[i].eulerAngles;
                rotations_x.Add(eulerAngles.x);
                rotations_y.Add(eulerAngles.y);
                rotations_z.Add(eulerAngles.z);
            }

            CompressRLE(rotations_x,ref optimize.RLE_x);
            CompressRLE(rotations_y,ref optimize.RLE_y);
            CompressRLE(rotations_z,ref optimize.RLE_z);
            
            _rotations.Add(optimize);
        }
        _data.Clear();
    }
    
    [ContextMenu("RLE Giai Nen")]
    public void RLE_Unzip()
    {
        _data.Clear();

        int frameCount = 0;

        // Giải nén RLE từ position để lấy lại số lượng frame
        if (_positions.Count > 0)
        {
            List<float> temp = new List<float>();
            DecompressRLE(_positions[0].RLE_x, temp);
            frameCount = temp.Count;
        }

        // Tạo danh sách frame trống
        for (int i = 0; i < frameCount; i++)
        {
            FrameData frame = new FrameData
            {
                position = new Vector3[_positions.Count],
                rotation = new Quaternion[_rotations.Count]
            };
            _data.Add(frame);
        }

        // Giải nén Position
        for (int i = 0; i < _positions.Count; i++)
        {
            List<float> px = new List<float>();
            List<float> py = new List<float>();
            List<float> pz = new List<float>();

            DecompressRLE(_positions[i].RLE_x, px);
            DecompressRLE(_positions[i].RLE_y, py);
            DecompressRLE(_positions[i].RLE_z, pz);

            for (int j = 0; j < frameCount; j++)
            {
                _data[j].position[i] = new Vector3(px[j], py[j], pz[j]);
            }
        }

        // Giải nén Rotation
        for (int i = 0; i < _rotations.Count; i++)
        {
            List<float> rx = new List<float>();
            List<float> ry = new List<float>();
            List<float> rz = new List<float>();
            List<float> rw = new List<float>();

            DecompressRLE(_rotations[i].RLE_x, rx);
            DecompressRLE(_rotations[i].RLE_y, ry);
            DecompressRLE(_rotations[i].RLE_z, rz);

            // Nếu bạn có thêm RLE_w thì thêm dòng này:
            // DecompressRLE(_rotations[i].RLE_w, rw);

            for (int j = 0; j < frameCount; j++)
            {
                _data[j].rotation[i] = Quaternion.Euler(rx[j], ry[j], rz[j]); // hoặc nếu bạn dùng raw Quaternion, dùng new Quaternion(x,y,z,w)
            }
        }
        
        _positions.Clear();
        _rotations.Clear();
    }

    void CompressRLE(List<float> list,ref List<RLEFloat> outputs)
    {
        outputs = new List<RLEFloat>();
        if (list.Count == 0)
            return;
        
        float currentFload = list[0];
        int count = 1;

        // Duyệt qua các phần tử trong danh sách
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i] == currentFload)
            {
                count++;
            }
            else
            {
                // Lưu trữ vector hiện tại và số lần lặp lại
//                Debug.Log(currentFload+"  " +count);
                outputs.Add(new RLEFloat(currentFload, count));
                currentFload = list[i];
                count = 1;
            }
        }

        // Thêm phần tử cuối cùng vào danh sách nén
        outputs.Add(new RLEFloat(currentFload, count));
    }

    void DecompressRLE(List<RLEFloat> rleList, List<float> outputs)
    {
        outputs.Clear();  // Đảm bảo rằng danh sách đầu ra được làm mới trước khi giải nén

        // Duyệt qua tất cả các phần tử trong danh sách nén
        foreach (var rle in rleList)
        {
            // Thêm giá trị 'Value' vào danh sách 'outputs' số lần 'Count' 
            for (int i = 0; i < rle.count; i++)
            {
                outputs.Add(rle.value);
            }
        }
    }
    
    
    [Serializable]
    public struct Optimize
    {
        public List<RLEFloat> RLE_x;
        public List<RLEFloat> RLE_y;
        public List<RLEFloat> RLE_z;
    }
    
    [Serializable]
    public struct RLEFloat
    {
        public float value;
        public int count;

        public RLEFloat(float v, int c)
        {
            value = v;
            count = c;
        }
    }

    #endregion
    
    
    
    #region <=================================Delta Compression============================>
    // Nén dữ liệu bằng Delta Compression
    [ContextMenu("DecompressData Nen")]
    public void CompressDataNen()
    {
        if (_data.Count == 0)
        {
            return;
        }

        // Giữ lại frame đầu tiên nguyên vẹn (keyframe)
        var deltaData = new List<FrameData>();
        FrameData firstFrame = _data[0];
        deltaData.Add(firstFrame);

        for (int i = 1; i < _data.Count; i++)
        {
            FrameData currentFrame = _data[i];
            FrameData previousFrame = _data[i - 1];

            FrameData deltaFrame = new FrameData
            {
                position = new Vector3[currentFrame.position.Length],
                rotation = new Quaternion[currentFrame.rotation.Length]
            };

            // Tính toán sự thay đổi delta (difference) giữa các frame
            for (int j = 0; j < currentFrame.position.Length; j++)
            {
                deltaFrame.position[j] = currentFrame.position[j] - previousFrame.position[j];
                deltaFrame.rotation[j] = Quaternion.Inverse(previousFrame.rotation[j]) * currentFrame.rotation[j];
            }

            deltaData.Add(deltaFrame);
        }

        // Cập nhật lại _data với các delta frames
        _data = deltaData;
    }

    // Giải nén dữ liệu bằng Delta Compression
    [ContextMenu("DecompressData Giai Nen")]
    public void DecompressDataGiaiNen()
    {
        if (_data.Count == 0)
        {
            return;
        }

        List<FrameData> decompressedData = new List<FrameData>();
        FrameData firstFrame = _data[0];
        decompressedData.Add(firstFrame);

        // Giải nén các delta frame
        for (int i = 1; i < _data.Count; i++)
        {
            FrameData currentDeltaFrame = _data[i];
            FrameData previousFrame = decompressedData[i - 1];

            FrameData decompressedFrame = new FrameData
            {
                position = new Vector3[currentDeltaFrame.position.Length],
                rotation = new Quaternion[currentDeltaFrame.rotation.Length]
            };

            // Tính lại position và rotation bằng cách cộng dồn delta với giá trị trước đó
            for (int j = 0; j < currentDeltaFrame.position.Length; j++)
            {
                decompressedFrame.position[j] = previousFrame.position[j] + currentDeltaFrame.position[j];
                decompressedFrame.rotation[j] = previousFrame.rotation[j] * currentDeltaFrame.rotation[j];
            }

            decompressedData.Add(decompressedFrame);
        }

        // Cập nhật lại _data với dữ liệu giải nén
        _data = decompressedData;
    }
    #endregion
}
