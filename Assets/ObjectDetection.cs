using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TensorFlowLite;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using GoogleARCore;
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
public class ObjectDetection : MonoBehaviour
{
    Texture2D result = null;
    Texture2D textureNoRotate = null;
    byte[] YUVimage;
    byte[] RGBimage;
    [DllImport("SharedObject1")]
    public static extern int ConvertYUV2RGBA(IntPtr input, IntPtr output, int width, int height);
    [SerializeField, FilePopup("*.tflite")] string fileName = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField] Text framePrefab;
    [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.5f;
    [SerializeField] TextAsset labelMap;
    SSD ssd;

    SSDAsync sSDAsync;

    public RawImage rawImage;
    Text[] frames;

    public string[] labels;
    // Start is called before the first frame update
    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log("fucking path " + path);
        ssd = new SSD(path);
        sSDAsync = new SSDAsync(path);
        frames = new Text[10];

        var parent = rawImage.transform;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Instantiate(framePrefab, Vector3.zero, Quaternion.identity, parent);
        }

        // Labels
        labels = labelMap.text.Split('\n');
        // InvokeRepeating("runSSDOnBackground", 1f, 0.1f);
        Invoke("runSSDOnBackground", 1f);
    }

    async UniTaskVoid DemoAsync()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        using (var image = Frame.CameraImage.AcquireCameraImageBytes())
        {
            if (!image.IsAvailable)
            {
                return;
            }

            if (result == null)
            {
                Debug.Log("result null !!!");
                textureNoRotate = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false, false);
                result = new Texture2D(image.Height, image.Width, TextureFormat.RGB24, false, false);
                YUVimage = new byte[(int)(image.Width * image.Height * 1.5f)];
                RGBimage = new byte[image.Width * image.Height * 3];
            }
            
            await UniTask.SwitchToThreadPool();

            unsafe
            {
                for (int i = 0; i < image.Width * image.Height; i++)
                {
                    YUVimage[i] = *((byte*)image.Y.ToPointer() + (i * sizeof(byte)));
                }

                for (int i = 0; i < image.Width * image.Height / 4; i++)
                {
                    YUVimage[(image.Width * image.Height) + 2 * i] = *((byte*)image.U.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
                    YUVimage[(image.Width * image.Height) + 2 * i + 1] = *((byte*)image.V.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
                }
            }

            GCHandle YUVhandle = GCHandle.Alloc(YUVimage, GCHandleType.Pinned);
            GCHandle RGBhandle = GCHandle.Alloc(RGBimage, GCHandleType.Pinned);

            // Call the C++ function that we created.
            int k = ConvertYUV2RGBA(YUVhandle.AddrOfPinnedObject(), RGBhandle.AddrOfPinnedObject(), image.Width, image.Height);

            // If OpenCV conversion failed, return null
            if (k != 0)
            {
                Debug.LogWarning("Color conversion - k != 0");
                return;
            }
            await UniTask.SwitchToMainThread();

            textureNoRotate.LoadRawTextureData(RGBimage);
            // result.SetPixels32(rotateTexture(textureNoRotate, true));
            // result.Apply();
            textureNoRotate.Apply();
            // // rawImage.texture = result;

            // ssd.Invoke(result);
            // var SSDBoxs = ssd.GetResults();

            var SSDBoxs = await sSDAsync.InvokeAsync(textureNoRotate);

            // var size = new Vector2(rawImage.rectTransform.rect.width, rawImage.rectTransform.rect.height * 0.625f);
            var size = rawImage.rectTransform.rect.size;

            for (int i = 0; i < frames.Length; i++)
            {
                Debug.Log($"Box label {SSDBoxs[i].classID} {GetLabelName(SSDBoxs[i].classID)}");
                SetFrame(frames[i], SSDBoxs[i], size);
                if (SSDBoxs[i].classID == 73) //73: mouse
                {
                    var positionX = Screen.width * (SSDBoxs[i].rect.xMin + SSDBoxs[i].rect.width / 2);
                    var positionY = Screen.height * (SSDBoxs[i].rect.yMin - SSDBoxs[i].rect.height / 2);
                    Debug.Log($"Box {SSDBoxs[i].rect.ToString()}");
                    Debug.Log($"SSD Raycast Position ({positionX}, {positionY})");
                    TrackableHit hitResult;
                    if (Frame.Raycast(positionX, positionY, TrackableHitFlags.PlaneWithinBounds, out hitResult)) {
                        var hitPlane = hitResult.Trackable as DetectedPlane;
                        Debug.Log("mouse hitted!");
                        if (hitPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing) 
                        {
                            if (UpdateFloorOfTheHouse.planeWithTypeDict.ContainsKey(hitPlane))
                            {
                                UpdateFloorOfTheHouse.planeWithTypeDict[hitPlane] = 1;
                            }
                            else
                            {
                                Debug.Log("Something wrong!");
                            }
                        }
                    }
                    else 
                    {
                        Debug.Log("No SSD Hit!");
                    }
                }
            }

            // Debug.Log("path " + Application.persistentDataPath);
            // File.WriteAllBytes(Application.persistentDataPath + "/tex.png", result.EncodeToPNG());

            YUVhandle.Free();
            RGBhandle.Free();

            Debug.Log("used ssd");
        }
        
        DemoAsync();
    }

    // void Update() {
    //     useSSD();
    // }

    void runSSDOnBackground()
    {
        DemoAsync();
        // Thread thread1 = new Thread(useSSD);

        // using (var image = Frame.CameraImage.AcquireCameraImageBytes())
        // {
        //     if (!image.IsAvailable)
        //     {
        //         return;
        //     }

        //     if (result == null)
        //     {
        //         Debug.Log("result null !!!");
        //         textureNoRotate = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false, false);
        //         result = new Texture2D(image.Height, image.Width, TextureFormat.RGB24, false, false);
        //         YUVimage = new byte[(int)(image.Width * image.Height * 1.5f)];
        //         RGBimage = new byte[image.Width * image.Height * 3];
        //     }

        //     unsafe
        //     {
        //         for (int i = 0; i < image.Width * image.Height; i++)
        //         {
        //             YUVimage[i] = *((byte*)image.Y.ToPointer() + (i * sizeof(byte)));
        //         }

        //         for (int i = 0; i < image.Width * image.Height / 4; i++)
        //         {
        //             YUVimage[(image.Width * image.Height) + 2 * i] = *((byte*)image.U.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
        //             YUVimage[(image.Width * image.Height) + 2 * i + 1] = *((byte*)image.V.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
        //         }
        //     }

        //     GCHandle YUVhandle = GCHandle.Alloc(YUVimage, GCHandleType.Pinned);
        //     GCHandle RGBhandle = GCHandle.Alloc(RGBimage, GCHandleType.Pinned);

        //     // Call the C++ function that we created.
        //     int k = ConvertYUV2RGBA(YUVhandle.AddrOfPinnedObject(), RGBhandle.AddrOfPinnedObject(), image.Width, image.Height);

        //     // If OpenCV conversion failed, return null
        //     if (k != 0)
        //     {
        //         Debug.LogWarning("Color conversion - k != 0");
        //         return;
        //     }

        //     textureNoRotate.LoadRawTextureData(RGBimage);
        //     result.SetPixels32(rotateTexture(textureNoRotate, true));
        //     result.Apply();

        //     useSSD();
        // }

        // thread1.Start();
    }
    void useSSD()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        using (var image = Frame.CameraImage.AcquireCameraImageBytes())
        {
            if (!image.IsAvailable)
            {
                return;
            }

            // Debug.Log($"Screen resolution {Screen.currentResolution.ToString()}");
            // Debug.Log($"Main camera size {Camera.main.rect.size.ToString()}");
            // Debug.Log($"Rawimage Size {rawImage.rectTransform.rect.size.ToString()}");

            if (result == null)
            {
                Debug.Log("result null !!!");
                textureNoRotate = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false, false);
                result = new Texture2D(image.Height, image.Width, TextureFormat.RGB24, false, false);
                YUVimage = new byte[(int)(image.Width * image.Height * 1.5f)];
                RGBimage = new byte[image.Width * image.Height * 3];
            }

            unsafe
            {
                for (int i = 0; i < image.Width * image.Height; i++)
                {
                    YUVimage[i] = *((byte*)image.Y.ToPointer() + (i * sizeof(byte)));
                }

                for (int i = 0; i < image.Width * image.Height / 4; i++)
                {
                    YUVimage[(image.Width * image.Height) + 2 * i] = *((byte*)image.U.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
                    YUVimage[(image.Width * image.Height) + 2 * i + 1] = *((byte*)image.V.ToPointer() + (i * image.UVPixelStride * sizeof(byte)));
                }
            }

            GCHandle YUVhandle = GCHandle.Alloc(YUVimage, GCHandleType.Pinned);
            GCHandle RGBhandle = GCHandle.Alloc(RGBimage, GCHandleType.Pinned);

            // Call the C++ function that we created.
            int k = ConvertYUV2RGBA(YUVhandle.AddrOfPinnedObject(), RGBhandle.AddrOfPinnedObject(), image.Width, image.Height);

            // If OpenCV conversion failed, return null
            if (k != 0)
            {
                Debug.LogWarning("Color conversion - k != 0");
                return;
            }


            textureNoRotate.LoadRawTextureData(RGBimage);
            result.SetPixels32(rotateTexture(textureNoRotate, true));
            result.Apply();
            // rawImage.texture = result;

            ssd.Invoke(result);
            var SSDBoxs = ssd.GetResults();

            // var size = new Vector2(rawImage.rectTransform.rect.width, rawImage.rectTransform.rect.height * 0.625f);
            var size = rawImage.rectTransform.rect.size;

            for (int i = 0; i < frames.Length; i++)
            {
                Debug.Log($"Box label {SSDBoxs[i].classID} {GetLabelName(SSDBoxs[i].classID)}");
                SetFrame(frames[i], SSDBoxs[i], size);
                if (SSDBoxs[i].classID == 73) //73: mouse
                {
                    var positionX = Screen.width * (SSDBoxs[i].rect.xMin + SSDBoxs[i].rect.width / 2);
                    var positionY = Screen.height * (SSDBoxs[i].rect.yMin - SSDBoxs[i].rect.height / 2);
                    Debug.Log($"Box {SSDBoxs[i].rect.ToString()}");
                    Debug.Log($"SSD Raycast Position ({positionX}, {positionY})");
                    TrackableHit hitResult;
                    if (Frame.Raycast(positionX, positionY, TrackableHitFlags.PlaneWithinBounds, out hitResult)) {
                        var hitPlane = hitResult.Trackable as DetectedPlane;
                        Debug.Log("mouse hitted!");
                        if (hitPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing) 
                        {
                            if (UpdateFloorOfTheHouse.planeWithTypeDict.ContainsKey(hitPlane))
                            {
                                UpdateFloorOfTheHouse.planeWithTypeDict[hitPlane] = 1;
                            }
                            else
                            {
                                Debug.Log("Something wrong!");
                            }
                        }
                    }
                    else 
                    {
                        Debug.Log("No SSD Hit!");
                    }
                }
            }

            // Debug.Log("path " + Application.persistentDataPath);
            // File.WriteAllBytes(Application.persistentDataPath + "/tex.png", result.EncodeToPNG());

            YUVhandle.Free();
            RGBhandle.Free();

            Debug.Log("used ssd");
        }
    }


    Color32[] rotateTexture(Texture2D originalTexture, bool clockwise)
    {
        Color32[] original = originalTexture.GetPixels32();
        Color32[] rotated = new Color32[original.Length];
        int w = originalTexture.width;
        int h = originalTexture.height;

        int iRotated, iOriginal;

        for (int j = 0; j < h; ++j)
        {
            for (int i = 0; i < w; ++i)
            {
                iRotated = (i + 1) * h - j - 1;
                iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                rotated[iRotated] = original[iOriginal];
            }
        }

        // Texture2D rotatedTexture = new Texture2D(h, w);
        // rotatedTexture.SetPixels32(rotated);
        // rotatedTexture.Apply();

        // return rotatedTexture;
        return rotated;
    }

    void SetFrame(Text frame, SSD.Result result, Vector2 size)
    {
        if (result.score < scoreThreshold)
        {
            frame.gameObject.SetActive(false);
            return;
        }
        else
        {
            frame.gameObject.SetActive(true);
        }

        frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
        var rt = frame.transform as RectTransform;
        // var newPos = new Vector2(result.rect.position.x, (result.rect.position.y + 0.125f) / 1.375f);
        rt.anchoredPosition = result.rect.position * size - size * 0.5f;
        // rt.anchoredPosition = newPos * size - size * 0.5f;
        // var newSize = new Vector2(result.rect.size.x, result.rect.size.y / 1.375f);
        rt.sizeDelta = result.rect.size * size;
        // rt.sizeDelta = newSize * size;
    }

    //hàm test setframe cho ssd async
    void SetFrame(Text frame, SSDAsync.Result result, Vector2 size)
    {
        if (result.score < scoreThreshold)
        {
            frame.gameObject.SetActive(false);
            return;
        }
        else
        {
            frame.gameObject.SetActive(true);
        }

        frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
        var rt = frame.transform as RectTransform;
        // var newPos = new Vector2(result.rect.position.x, (result.rect.position.y + 0.125f) / 1.375f);
        rt.anchoredPosition = result.rect.position * size - size * 0.5f;
        // rt.anchoredPosition = newPos * size - size * 0.5f;
        // var newSize = new Vector2(result.rect.size.x, result.rect.size.y / 1.375f);
        rt.sizeDelta = result.rect.size * size;
        // rt.sizeDelta = newSize * size;
    }

    string GetLabelName(int id)
    {
        if (id < 0 || id >= labels.Length - 1)
        {
            return "?";
        }
        return labels[id + 1];
    }
}
