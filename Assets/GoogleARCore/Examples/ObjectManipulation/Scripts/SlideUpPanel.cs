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
    private ObjectStorage objectStorage;
    private Transform categoryButtonsContainer;
    private Transform furButtonsContainer;
    private List<List<GameObject>> allFurnituresButton;
    private int choosingCategoryIndex = 0;
    public static bool wasClickedOnUI = false;
    public GameObject manipulatorPrefab;
    public Camera FirstPersonCamera;
    void Awake()
    {
        categoryButtonsContainer = transform.Find("FurCategoryListView/Content");
        furButtonsContainer = transform.Find("FurListView/Content");
        objectStorage = GameObject.Find("/ObjectStorage").GetComponent<ObjectStorage>();
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
            // Debug.Log("Oh my god!! " + i);
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
        Debug.Log($"i {categoryIndex} j {furIndex}");
        var choosenFur = objectStorage.allFurnitures[categoryIndex].furnitures[furIndex];

        var camToItemVector = Vector3.Normalize(new Vector3(FirstPersonCamera.transform.forward.x,
                                        0f,
                                        FirstPersonCamera.transform.forward.z));
        var camToItemVectorPoint = 1f * camToItemVector + new Vector3(FirstPersonCamera.transform.position.x,
                                                            UpdateFloorOfTheHouse.floorY,
                                                            FirstPersonCamera.transform.position.z);
        var itemRotation = Quaternion.LookRotation(-camToItemVector); // hướng đồ vật vào mặt mình

        List<TrackableHit> hitResults = new List<TrackableHit>();
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

        Vector3 raycastPoint = new Vector3(camToItemVectorPoint.x, 0, camToItemVectorPoint.z);
        if (Frame.RaycastAll(raycastPoint, Vector3.down, hitResults, 1.5f, raycastFilter))
        {
            foreach (var hit in hitResults)
            {
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

    void InstantiateFurniture(GameObject objectPrefab, Pose pose, DetectedPlane arPlane)
    {
        Debug.Log($"Floor detected plane is null? {(UpdateFloorOfTheHouse.floorDetectedPlane == null).ToString()}");
        if (arPlane == null)
        {
            return;
        }
        var anchor = arPlane.CreateAnchor(pose);
        var manipulator = Instantiate(manipulatorPrefab, pose.position, pose.rotation, anchor.transform);
        var gameObject = Instantiate(objectPrefab, pose.position, pose.rotation, manipulator.transform);

        manipulator.GetComponent<Manipulator>().Select();
    }
}
