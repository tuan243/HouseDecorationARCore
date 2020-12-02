using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System;
using GoogleARCore.Examples.ObjectManipulation;
using GoogleARCore;
public static class ButtonExtension
{
    public static void AddEventListener<T1, T2>(this Button button, T1 param1, T2 param2, Action<T1, T2> OnClick)
    {
        button.onClick.AddListener(delegate ()
        {
            OnClick(param1, param2);
        });
    }

    public static void AddEventListener<T>(this Button button, T param, Action<T> OnClick)
    {
        button.onClick.AddListener(delegate ()
        {
            OnClick(param);
        });
    }
}
public class SlideUpPanel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject categoryButton;
    public GameObject furnitureButton;
    private CurrentObjectManager objectManager;
    private ObjectStorage objectStorage;
    private Transform categoryButtonsContainer;
    private Transform furButtonsContainer;
    private List<List<GameObject>> allFurnituresButton;
    private int choosingCategoryIndex = 0;
    public static bool wasClickedOnUI = false;
    public GameObject manipulatorPrefab;
    public Camera FirstPersonCamera;
    [SerializeField] private GameObject m_SnackBar = null;
    [SerializeField] private Text m_SnackBarText = null;
    private int numberOfCoroutineRunning = 0;
    private float snackBarShowTime = 3f;
    void Awake()
    {
        categoryButtonsContainer = transform.Find("FurCategoryListView/Content");
        furButtonsContainer = transform.Find("FurListView/Content");
        objectStorage = GameObject.Find("/ObjectStorage").GetComponent<ObjectStorage>();
        objectManager = GameObject.Find("/CurrentObjectManager").GetComponent<CurrentObjectManager>();
    }
    void Start()
    {
        if (categoryButtonsContainer == null || furButtonsContainer == null)
        {
            Debug.Log("cant get container of button :(");
            return;
        }

        allFurnituresButton = new List<List<GameObject>>();
        for (int i = 0; i < objectStorage.allFurnitures.Count; i++)
        {
            allFurnituresButton.Add(new List<GameObject>());

            var catBtn = Instantiate(categoryButton, categoryButtonsContainer);
            catBtn.GetComponentInChildren<Text>().text = objectStorage.allFurnitures[i].categoryName;
            catBtn.GetComponent<Button>().AddEventListener(i, CategoryButtonClick);

            for (int j = 0; j < objectStorage.allFurnitures[i].furnitures.Count; j++)
            {
                var furBtn = Instantiate(furnitureButton, furButtonsContainer);
                furBtn.SetActive(false);
                furBtn.transform.GetChild(0).GetComponent<Image>().sprite = objectStorage.allFurnitures[i].icons[j];
                furBtn.GetComponent<Button>().AddEventListener(i, j, FurButtonClick);
                allFurnituresButton[i].Add(furBtn);
            }
        }

        for (int i = 0; i < allFurnituresButton[0].Count; i++)
        {
            allFurnituresButton[0][i].SetActive(true);
        }
    }

    public void FurButtonClick(int categoryIndex, int furIndex)
    {
        Debug.Log("object manager null? " + (objectManager == null).ToString());
        Debug.Log("CurrentObjectManager.maxObjectCount null " + CurrentObjectManager.maxObjectCount);
        if (objectManager.CurObjectCount >= CurrentObjectManager.maxObjectCount)
        {
            StartCoroutine("ShowSnackBar");
            return;
        }
        // Debug.Log("check -1");
        // Debug.Log("object storage null: " + (objectStorage == null).ToString());
        var choosenFur = objectStorage.allFurnitures[categoryIndex].furnitures[furIndex];
        // Debug.Log("object storage null: " + (objectStorage == null).ToString());
        Debug.Log("first person camera null: " + (FirstPersonCamera == null).ToString());

        var camToItemVector = Vector3.Normalize(new Vector3(FirstPersonCamera.transform.forward.x,
                                        0f,
                                        FirstPersonCamera.transform.forward.z));
        // Debug.Log("first person camera null: " + (FirstPersonCamera == null).ToString());
        var camToItemVectorPoint = 1f * camToItemVector + new Vector3(FirstPersonCamera.transform.position.x,
                                                            UpdateFloorOfTheHouse.floorY,
                                                            FirstPersonCamera.transform.position.z);
        // Debug.Log("object storage null: " + (objectStorage == null).ToString());
        // Debug.Log("first person camera null: " + (FirstPersonCamera == null).ToString());

        var itemRotation = Quaternion.LookRotation(-camToItemVector); // hướng đồ vật vào mặt mình

        // Debug.Log("check 0");

        if (categoryIndex == objectStorage.allFurnitures.Count - 1) //last index is wall furniture
        {
            var walls = UpdateFloorOfTheHouse.wallDetectedPlanes;
            var projectedPoint = Vector3.zero;
            TrackableHit hit = new TrackableHit();
            if (Frame.Raycast(FirstPersonCamera.transform.position, camToItemVector, out hit, 2f))
            {
                var itemPose = new Pose(projectedPoint, 
                            Quaternion.LookRotation(Vector3.up, hit.Pose.rotation * Vector3.up));

                InstantiateFurniture(choosenFur, itemPose, hit.Trackable as DetectedPlane);
                return;
            }

            if (walls.Count == 0)
            {
                Debug.Log("wall is empty :(");
            }
            foreach (DetectedPlane wall in walls) {
                Plane wallPlane = new Plane(wall.CenterPose.rotation * Vector3.up, wall.CenterPose.position);
                if (wallPlane.GetDistanceToPoint(camToItemVectorPoint) < 1.2f)
                {
                    var planeNormal = wall.CenterPose.rotation * Vector3.up;

                    Ray ray = new Ray(FirstPersonCamera.transform.position, camToItemVector);

                    float enter;

                    if (wallPlane.Raycast(ray, out enter))
                    {
                        projectedPoint = ray.GetPoint(enter);
                        
                        var itemPose = new Pose(projectedPoint, 
                                    Quaternion.LookRotation(Vector3.up, wall.CenterPose.rotation * Vector3.up));

                        InstantiateFurniture(choosenFur, itemPose, wall);
                        Debug.Log("Yeah");
                    }

                    break;
                }
                else
                {
                    Debug.Log("Not reach wall :(");
                }
            }
            
            return;
        }
        // Debug.Log("Check 1");
        List<TrackableHit> hitResults = new List<TrackableHit>();
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

        Vector3 raycastPoint = new Vector3(camToItemVectorPoint.x, 0, camToItemVectorPoint.z);
        if (Frame.RaycastAll(raycastPoint, Vector3.down, hitResults, 1.5f, raycastFilter))
        {
            foreach (var hit in hitResults)
            {
                // Debug.Log("Check 2");
                if ((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0)
                {
                    Debug.Log("Hit at back of the current DetectedPlane");
                    continue;
                }
                DetectedPlane hitPlane = hit.Trackable as DetectedPlane;
                if (hitPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing)
                {
                    var itemPose = new Pose(hit.Pose.position, itemRotation);
                    InstantiateFurniture(choosenFur, itemPose, hitPlane);
                    return;
                }
            }
        }
        else 
        {
            var itemPose = new Pose(camToItemVectorPoint, itemRotation);
            // Debug.Log("Check 1");

            InstantiateFurniture(choosenFur, itemPose, UpdateFloorOfTheHouse.floorDetectedPlane);
        }
    }

    public void CategoryButtonClick(int categoryIndex)
    {
        for (int i = 0; i < objectStorage.allFurnitures[choosingCategoryIndex].furnitures.Count; i++)
        {
            allFurnituresButton[choosingCategoryIndex][i].SetActive(false);
        }
        choosingCategoryIndex = categoryIndex;
        furButtonsContainer.transform.parent.GetComponent<ScrollRect>().velocity = Vector2.zero;
        furButtonsContainer.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        for (int i = 0; i < objectStorage.allFurnitures[choosingCategoryIndex].furnitures.Count; i++)
        {
            allFurnituresButton[choosingCategoryIndex][i].SetActive(true);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        wasClickedOnUI = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        wasClickedOnUI = false;
    }

    IEnumerator ShowSnackBar()
    {
        // Debug.Log("Coroutine ^-^");
        numberOfCoroutineRunning++;
        m_SnackBar.SetActive(true);
        m_SnackBarText.text = "Reached maximum number of item. Try to remove some furniture.";
        yield return new WaitForSeconds(3f);
        numberOfCoroutineRunning--;
        if (numberOfCoroutineRunning == 0)
        {
            m_SnackBar.SetActive(false);
        }
    }

    void InstantiateFurniture(GameObject objectPrefab, Pose pose, DetectedPlane arPlane)
    {
        if (arPlane == null)
        {
            return;
        }
        var anchor = arPlane.CreateAnchor(pose);
        var manipulator = Instantiate(manipulatorPrefab, pose.position, pose.rotation, anchor.transform);
        var gameObject = Instantiate(objectPrefab, pose.position, pose.rotation, manipulator.transform);

        manipulator.GetComponent<Manipulator>().Select();

        Debug.Log("curr object count: " + objectManager.CurObjectCount.ToString());
    }
}
