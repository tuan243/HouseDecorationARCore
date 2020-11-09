using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TensorFlowLite;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using GoogleARCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
public class ObjectDetection : MonoBehaviour
{
    Dictionary<int, Color> m_categoryColorMap = new Dictionary<int, Color>()
    {
        {61, new Color(1f, 0f, 0f, 1f)},
        {72, new Color(0f, 1f, 0f, 1f)},
        {46, new Color(0f, 0f, 1f, 1f)},
        {73, new Color(1f, 1f, 0f, 1f)}
    };
    private float m_minSPDistance = 0.2f;
    private int m_MaxPointCount = 1000;
    private LinkedList<Vector3> m_CachedPoints;
    private List<GameObject> m_ObjectList;
    private Dictionary<int, PointCloudPoint> m_CachedPointsDict;
    private LinkedList<SuperPoint> m_SuperPoints;
    public Camera FirstPersonCamera;
    public GameObject objectPoint;
    private float detectFrequency = 0.25f;
    private float timePassed = 0f;
    ScreenOrientation m_cachedOrientation = ScreenOrientation.Portrait;
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

    bool runningSSD = false;

    SSDAsync sSDAsync;

    public RawImage rawImage;
    Text[] frames;

    public string[] labels;

    // Start is called before the first frame update
    void Start()
    {
        // Instantiate(objectPoint, new Vector3(0f, 0f, 1f), Quaternion.identity);
        // m_CachedPoints = new LinkedList<Vector3>();
        m_CachedPointsDict = new Dictionary<int, PointCloudPoint>();
        m_SuperPoints = new LinkedList<SuperPoint>();
        m_ObjectList = new List<GameObject>();

        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        // Debug.Log("fucking path " + path);
        // ssd = new SSD(path);
        sSDAsync = new SSDAsync(path);
        frames = new Text[10];

        var parent = rawImage.transform;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Instantiate(framePrefab, Vector3.zero, Quaternion.identity, parent);
        }

        // Labels
        labels = labelMap.text.Split('\n');
    }

    bool isPointInsideRect(Vector2 point, Rect rect)
    {
        if (point.x > rect.x && point.x < rect.x + rect.width && point.y < rect.y && point.y > rect.y - rect.height)
            return true;
        return false;
    }

    Vector3? getMedianObjectPoint(SSDAsync.Result ssdResult)
    {
        Vector3 sumPoint = Vector3.zero;
        int count = 0;
        // LinkedListNode<Vector3> pointNode;
        // Debug.Log("cached point count " + m_CachedPoints.Count);
        // for (pointNode = m_CachedPoints.First; pointNode != null; pointNode = pointNode.Next)
        // {
        //     Vector2 pointInScreen = Camera.main.WorldToScreenPoint(pointNode.Value);

        //     Vector2 pointInScreenNormalized = new Vector2((0.2f + pointInScreen.x) / (1.67f * Screen.width), pointInScreen.y / Screen.height);
        //     if (isPointInsideRect(pointInScreenNormalized, ssdResult.rect))
        //     {
        //         sumPoint += pointNode.Value;
        //         count++;
        //     }
        // }

        foreach (KeyValuePair<int, PointCloudPoint> pair in m_CachedPointsDict)
        {
            Vector2 pointInScreen = Camera.main.WorldToScreenPoint(pair.Value.Position);

            Vector2 pointInScreenNormalized = new Vector2((0.2f + pointInScreen.x) / (1.67f * Screen.width), pointInScreen.y / Screen.height);
            if (isPointInsideRect(pointInScreenNormalized, ssdResult.rect))
            {
                sumPoint += pair.Value.Position;
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }
        return sumPoint / count;
    }

    void UpdateSuperPoints(Vector3 loc, float p, int label, Vector3 view, float scale)
    {
        LinkedListNode<SuperPoint> pointNode;
        List<SuperPoint> spSubset = new List<SuperPoint>();

        for (pointNode = m_SuperPoints.First; pointNode != null; pointNode = pointNode.Next)
        {
            if ((pointNode.Value.loc - loc).magnitude < m_minSPDistance)
            {
                spSubset.Add(pointNode.Value);
            }
        }

        float sdiff = 0f;
        float vdiff = 0f;
        float labelScore = ComputeLabelScore(ref spSubset, p, label, view, scale, out vdiff, out sdiff);

        if (spSubset.Count != 0)
        {
            foreach (var sp in spSubset)
            {
                if (sp.list_score.ContainsKey(label))
                {
                    sp.list_score[label] += labelScore;
                }
                else
                {
                    sp.list_score.Add(label, 0f);
                }

                float maxValue = -1f;
                int l = 0;
                foreach (var score in sp.list_score)
                {
                    if (score.Value > maxValue)
                    {
                        maxValue = score.Value;
                        l = score.Key;
                    }
                }

                Debug.Log("Best category " + l);
                if (m_categoryColorMap.ContainsKey(l))
                {
                    m_ObjectList[sp.id].GetComponent<MeshRenderer>().material.color = m_categoryColorMap[l];
                }
            }

            if (vdiff >= 45f)
            {
                foreach (var sp in spSubset)
                {
                    sp.list_view.Add(view);
                }
            }

            if (sdiff >= 1f)
            {
                foreach (var sp in spSubset)
                {
                    sp.list_scale.Add(scale);
                }
            }
        }
        else
        {
            m_SuperPoints.AddLast(new SuperPoint(m_SuperPoints.Count, loc, label, labelScore, view, scale));
            Vector3 lookVector = new Vector3(FirstPersonCamera.transform.position.x - loc.x, loc.y, FirstPersonCamera.transform.position.z - loc.z);
            Quaternion lookCameraRotation = Quaternion.LookRotation(lookVector, Vector3.up);
            var gameObject = Instantiate(objectPoint, loc, lookCameraRotation);
            if (m_categoryColorMap.ContainsKey(label))
            {
                gameObject.GetComponent<MeshRenderer>().material.color = m_categoryColorMap[label];
            }
            m_ObjectList.Add(gameObject);
        }
    }

    float ComputeWv(ref List<SuperPoint> spSubset, Vector3 view, out float vdiff)
    {
        vdiff = 180;
        foreach (var sp in spSubset)
        {
            foreach (var v in sp.list_view)
            {
                var angleDiff = Vector3.Angle(v, view);
                if (angleDiff < vdiff)
                {
                    vdiff = angleDiff;
                }
            }
        }

        if (vdiff < 45)
            return 0;
        if (vdiff >= 45 && vdiff <= 90)
            return (vdiff - 45) / 45f;
        return 1;
    }

    float ComputeWs(ref List<SuperPoint> spSubset, float scale, out float sdiff)
    {
        sdiff = 5;
        foreach (var sp in spSubset)
        {
            foreach (var s in sp.list_scale)
            {
                var diff = Mathf.Abs(scale - s);
                if (diff < sdiff)
                {
                    sdiff = diff;
                }
            }
        }

        return 0.2f * sdiff;
    }

    float ComputeLabelScore(ref List<SuperPoint> spSubset, float p, int label, Vector3 view, float scale, out float sdiff, out float vdiff)
    {
        float wv = ComputeWv(ref spSubset, view, out vdiff);
        float ws = ComputeWs(ref spSubset, scale, out sdiff);

        return (wv + ws) * p / 2f;
    }

    async UniTaskVoid DemoAsync()
    {
        runningSSD = true;
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        using (var image = Frame.CameraImage.AcquireCameraImageBytes())
        {
            if (!image.IsAvailable)
            {
                runningSSD = false;
                return;
            }

            Debug.Log($"Screen size ({Screen.width}, {Screen.height})");

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
                runningSSD = false;
                Debug.LogWarning("Color conversion - k != 0");
                return;
            }
            await UniTask.SwitchToMainThread();

            textureNoRotate.LoadRawTextureData(RGBimage);
            // result.SetPixels32(rotateTexture(textureNoRotate, true));
            // result.Apply();
            textureNoRotate.Apply();
            // System.IO.File.WriteAllBytes(Application.persistentDataPath + "/tex_480_640.png", result.EncodeToPNG());

            var SSDBoxs = await sSDAsync.InvokeAsync(textureNoRotate);

            var size = rawImage.rectTransform.rect.size;

            for (int i = 0; i < 10; i++)
            {
                var adjustedBox = AdjustSSDResult(SSDBoxs[i]);

                SetFrame(frames[i], adjustedBox, size);
                Debug.Log($"Box label {adjustedBox.classID} {GetLabelName(adjustedBox.classID)}, {adjustedBox.rect.ToString()}, {adjustedBox.score}");
                if (SSDBoxs[i].score < 0.5f)
                {
                    continue;
                }

                var objectPos = getMedianObjectPoint(adjustedBox);

                if (objectPos == null)
                {
                    Debug.Log("NO POINT CLOUD IN BOUNDING BOX!");
                    continue;
                }

                var position = (Vector3)objectPos;
                // var colliders = Physics.OverlapSphere(position, 0.1f, LayerMask.GetMask("CloudPoint"));
                // if (colliders.Length < 1)
                // {
                //     Vector3 lookVector = new Vector3(FirstPersonCamera.transform.position.x - position.x, position.y, FirstPersonCamera.transform.position.z - position.z);
                //     Quaternion lookCameraRotation = Quaternion.LookRotation(lookVector, Vector3.up);
                //     var bubbleSpeech = Instantiate(objectPoint, position, lookCameraRotation);
                //     bubbleSpeech.GetComponent<LabelBubble>().SetText(GetLabelName(adjustedBox.classID));
                // }
                Vector3 view = (FirstPersonCamera.transform.position - position).normalized;
                float scale = Mathf.Log((FirstPersonCamera.transform.position - position).magnitude, 2);
                UpdateSuperPoints(position, SSDBoxs[i].score, SSDBoxs[i].classID, view, scale);

                // gameObject.transform.parent = anchor.transform;
                // if (SSDBoxs[i].classID == 73) //73: mouse
                // {
                //     var positionX = Screen.width * (SSDBoxs[i].rect.xMin + SSDBoxs[i].rect.width / 2);
                //     var positionY = Screen.height * (SSDBoxs[i].rect.yMin - SSDBoxs[i].rect.height / 2);
                //     Debug.Log($"Box {SSDBoxs[i].rect.ToString()}");
                //     Debug.Log($"SSD Raycast Position ({positionX}, {positionY})");
                //     TrackableHit hitResult;
                //     if (Frame.Raycast(positionX, positionY, TrackableHitFlags.PlaneWithinBounds, out hitResult)) {
                //         var hitPlane = hitResult.Trackable as DetectedPlane;
                //         Debug.Log("mouse hitted!");
                //         if (hitPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing) 
                //         {
                //             if (UpdateFloorOfTheHouse.planeWithTypeDict.ContainsKey(hitPlane))
                //             {
                //                 UpdateFloorOfTheHouse.planeWithTypeDict[hitPlane] = 1;
                //             }
                //             else
                //             {
                //                 Debug.Log("Something wrong!");
                //             }
                //         }
                //     }
                //     else 
                //     {
                //         Debug.Log("No SSD Hit!");
                //     }
                // }
            }

            YUVhandle.Free();
            RGBhandle.Free();
        }

        runningSSD = false;
    }

    void Update()
    {
        if (Screen.orientation != m_cachedOrientation)
        {
            m_cachedOrientation = Screen.orientation;
            switch (m_cachedOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    rawImage.transform.localScale = new Vector3(1.67f, 1f, 1f);
                    break;
                case ScreenOrientation.LandscapeLeft:
                case ScreenOrientation.LandscapeRight:
                    rawImage.transform.localScale = new Vector3(1f, 1.67f, 1f);
                    break;
            }
        }

        if (Session.Status != SessionStatus.Tracking)
        {
            _ClearCachedPoints();
        }

        timePassed += Time.deltaTime;

        if (Frame.PointCloud.IsUpdatedThisFrame)
        {
            for (int i = 0; i < Frame.PointCloud.PointCount; i++)
            {
                _AddPointToCache(Frame.PointCloud.GetPointAsStruct(i));
            }
        }

        if (timePassed > 1 / detectFrequency)
        {
            timePassed = 0f;
            DemoAsync();
        }
    }

    private void _ClearCachedPoints()
    {
        // m_CachedPoints.Clear();
        m_CachedPointsDict.Clear();
    }

    private void _AddPointToCache(PointCloudPoint point)
    {
        // if (m_CachedPoints.Count >= m_MaxPointCount)
        // {
        //     m_CachedPoints.RemoveFirst();
        // }

        // m_CachedPoints.AddLast(point);
        if (m_CachedPointsDict.Count >= m_MaxPointCount)
        {
            var rand = new System.Random();
            int key = m_CachedPointsDict.ElementAt(rand.Next(m_CachedPointsDict.Count)).Key;

            m_CachedPointsDict.Remove(key);
        }

        if (!m_CachedPointsDict.ContainsKey(point.Id))
        {
            m_CachedPointsDict.Add(point.Id, point);
        }
        else
        {
            m_CachedPointsDict[point.Id] = point;
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

    SSDAsync.Result AdjustSSDResult(SSDAsync.Result result)
    {
        bool isPortrait = true;
        switch (m_cachedOrientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                isPortrait = true;
                break;
            case ScreenOrientation.LandscapeLeft:
            case ScreenOrientation.LandscapeRight:
                isPortrait = false;
                break;
        }
        if (isPortrait)
        {
            result.rect.position = new Vector2(result.rect.position.x, (result.rect.position.y * 6 + 1) / 8f);
            result.rect.size = new Vector2(result.rect.size.x, result.rect.size.y * 0.75f);
        }
        else
        {
            result.rect.position = new Vector2((result.rect.position.x * 6 + 1) / 8f, result.rect.position.y);
            result.rect.size = new Vector2(result.rect.size.x * 0.75f, result.rect.size.y);
        }
        return result;
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
        // var newPos = new Vector2(result.rect.position.x, (result.rect.position.y * 6 + 1) / 8f);
        rt.anchoredPosition = result.rect.position * size - size * 0.5f;
        // rt.anchoredPosition = newPos * size - size * 0.5f;
        // var newSize = new Vector2(result.rect.size.x, result.rect.size.y * 0.75f);
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
