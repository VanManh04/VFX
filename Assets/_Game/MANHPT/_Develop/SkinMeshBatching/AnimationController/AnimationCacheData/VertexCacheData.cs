using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

[CreateAssetMenu(fileName = "VertexCacheData", menuName = "ScriptableObjects/VAT/VertexCacheData", order = 0)]
[PreferBinarySerialization]
public class VertexCacheData : ScriptableObject, ICacheData<VertexCacheData.StateInfo>
{
    [field: SerializeField] public List<StateInfo> StateInfors { get; set; }
    [NonSerialized]public Texture2D VAT;
    [HideInInspector] public byte[] bytes;
    [HideInInspector] public float frameIndex;
    [HideInInspector] public string BotName;
    private bool _isInit;
    private string _path = "\\_Game\\VAT_BYTES\\";
    public void GetFrameRangeAndBoundingBox(string stateName, out Vector2 frameRange,
        out ModelBoundingBox boundingBox)
    {
        var info = GetStateInfo(stateName);
        frameRange = new(info.animationInfo.frameRange.x / frameIndex, info.animationInfo.frameRange.y / frameIndex);
        boundingBox = info.boundingBox;
    }


    public StateInfo GetStateInfo(string stateName)
    {
        return StateInfors.FirstOrDefault(x => x.stateName == stateName);
    }

    public StateInfo GetDefaultStateInfo()
    {
        if (StateInfors == null || StateInfors.Count == 0) return default;
        return StateInfors[0];
    }

    public void SaveTexture()
    {
        frameIndex = 0;

        foreach (var animInfo in StateInfors)
        {
            animInfo.CalculateBoundingBox();
        }


        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        foreach (var animInfo in StateInfors)
        {
            texture = CombineImages(texture, animInfo.ConvertToTexture());
            animInfo.animationInfo.frameRange = new(frameIndex, frameIndex + animInfo.frameCount);
            frameIndex += animInfo.frameCount;
        }
        texture.Apply();
        bytes = texture.EncodeToPNG();
        // var base64 = Convert.ToBase64String(bytes);
        // PlayerPrefs.SetString("VAT", base64);
        // EncodeTexture2D(texture, out var bitmap);
        // ZipBitmap(bitmap, "D:\\test.zip", "test.byte");
    }
    
    private Texture2D CombineImages(Texture2D image1, Texture2D image2)
    {
        if (image1.height == 2) return image2;

        int width = image1.width;
        int height = image1.height + image2.height;

        Texture2D combinedTexture = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels1 = image1.GetPixels();
        var pixels2 = image2.GetPixels();

        combinedTexture.SetPixels(0, 0, width, image1.height, pixels1);
        combinedTexture.SetPixels(0, image1.height, width, image2.height, pixels2);
        combinedTexture.Apply();
        return combinedTexture;
    }

    public void InitTextures()
    {
        if (_isInit) return;
        VAT = new Texture2D(2, 2, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        // var bitmap = UnzipBitmap("D:\\test.zip", "test.byte");
        // VAT = DecodeTexture2D(bitmap);
        // bytes = Convert.FromBase64String(PlayerPrefs.GetString("VAT"));
        VAT.LoadImage(bytes);
        VAT.Apply();
        _isInit = true;
    }

    public void Dispose()
    {
        foreach (var animInfo in StateInfors)
        {
            animInfo.Dispose();
        }

        _isInit = false;
    }


    [Serializable]
    public class StateInfo
    {
        [NonSerialized] public List<FrameInfo> frameInfos;
        [NonSerialized] public Texture2D texture;
        [HideInInspector] public byte[] bytes;
        public VAT_Utilities.AnimationInfo animationInfo;
        public ModelBoundingBox boundingBox;
        public string stateName;
        public int frameCount;


        public void CalculateBoundingBox()
        {
            boundingBox = CalculateBoundingBox(frameInfos);
        }

        public void InitTexture()
        {
            texture = new Texture2D(2, 2, TextureFormat.RGB24, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.LoadImage(bytes);
        }

        public void SaveTexture()
        {
            var texBytes = ConvertToBytes();
            bytes = new byte[texBytes.Length];
            texBytes.CopyTo(bytes, 0);
        }

        public byte[] ConvertToBytes()
        {
            if (frameInfos.Count == 0) return null;
            frameCount = frameInfos.Count;
            var tex = new Texture2D(frameInfos[0].vertices.Length, frameInfos.Count, TextureFormat.RGB24, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var colors = new Color[frameInfos[0].vertices.Length * frameInfos.Count];

            for (var i = 0; i < frameInfos.Count; i++)
            {
                for (var j = 0; j < frameInfos[i].vertices.Length; j++)
                {
                    var normalizedX = (frameInfos[i].vertices[j].x - boundingBox.min.x) /
                                      (boundingBox.max.x - boundingBox.min.x);
                    var normalizedY = (frameInfos[i].vertices[j].y - boundingBox.min.y) /
                                      (boundingBox.max.y - boundingBox.min.y);
                    var normalizedZ = (frameInfos[i].vertices[j].z - boundingBox.min.z) /
                                      (boundingBox.max.z - boundingBox.min.z);
                    colors[i * frameInfos[i].vertices.Length + j] = new Color(normalizedX, normalizedY, normalizedZ);
                }
            }

            tex.SetPixels(colors);
            tex.Apply(); 
            return tex.EncodeToPNG();
        }

        public Texture2D ConvertToTexture()
        {
            if (frameInfos.Count == 0) return null;
            frameCount = frameInfos.Count;
            var tex = new Texture2D(frameInfos[0].vertices.Length, frameInfos.Count, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                
            };
            var colors = new Color[frameInfos[0].vertices.Length * frameInfos.Count];

            for (var i = 0; i < frameInfos.Count; i++)
            {
                for (var j = 0; j < frameInfos[i].vertices.Length; j++)
                {
                    var normalizedX = (frameInfos[i].vertices[j].x - boundingBox.min.x) /
                                      (boundingBox.max.x - boundingBox.min.x);
                    var normalizedY = (frameInfos[i].vertices[j].y - boundingBox.min.y) /
                                      (boundingBox.max.y - boundingBox.min.y);
                    var normalizedZ = (frameInfos[i].vertices[j].z - boundingBox.min.z) /
                                      (boundingBox.max.z - boundingBox.min.z);
                    colors[i * frameInfos[i].vertices.Length + j] = new Color(normalizedX, normalizedY, normalizedZ);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private static ModelBoundingBox CalculateBoundingBox(List<FrameInfo> frameInfos)
        {
            var modelBoundingBox = new ModelBoundingBox
            {
                min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                max = new Vector3(float.MinValue, float.MinValue, float.MinValue)
            };

            foreach (var frameInfo in frameInfos)
            {
                foreach (var vertex in frameInfo.vertices)
                {
                    modelBoundingBox.min = Vector3.Min(modelBoundingBox.min, vertex);
                    modelBoundingBox.max = Vector3.Max(modelBoundingBox.max, vertex);
                }
            }

            return modelBoundingBox;
        }

        public void Dispose()
        {
            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }

            texture = null;
        }
    }

    [Serializable]
    public struct FrameInfo
    {
        public Vector3[] vertices;
    }

    [Serializable]
    public struct ModelBoundingBox
    {
        public Vector3 min;
        public Vector3 max;
    }
}