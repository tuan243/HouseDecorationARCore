using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

public class AutoPlacementFurniture : MonoBehaviour
{
    //Nếu diện tích của plane lớn hơn số này thì nó là bàn
    float tableAreaThres = 1f;
    public Camera FirstPersonCamera;
    public GameObject ManipulatorPrefab;
    public GameObject Furniture;
    // Update is called once per frame
    // void Update() 
    // {
    //     TrackableHit hit;
    //     TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

    //     if (Frame.Raycast(
    //         Screen.width / 2, Screen.height / 2, raycastFilter, out hit))
    //     {
    //             if ((hit.Trackable is DetectedPlane) &&
    //             Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
    //                 hit.Pose.rotation * Vector3.up) < 0)
    //         {
    //             Debug.Log("Looking at back of the current DetectedPlane");
    //             return;
    //         }

    //         var hitPlane = hit.Trackable as DetectedPlane;
    //         var planeDict = UpdateFloorOfTheHouse.planeWithTypeDict;
    //         if (planeDict.ContainsKey(hitPlane))
    //         {
    //             if (planeDict[hitPlane] == -2) //-2 để đánh dấu những plane đã auto đặt đồ
    //             {
    //                 return;
    //             }
    //             if (hitPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing
    //                 && hitPlane.ExtentX * hitPlane.ExtentZ > tableAreaThres)
    //             {
    //                 var model = Furniture;
    //                 Instantiate(model, hitPlane.CenterPose.position, hitPlane.CenterPose.rotation);

    //                 var manipulator =
    //                     Instantiate(ManipulatorPrefab, hitPlane.CenterPose.position, Quaternion.identity);

    //                 model.transform.parent = manipulator.transform;

    //                 var anchor = hitPlane.CreateAnchor(hitPlane.CenterPose);
    //                 manipulator.transform.parent = anchor.transform;

    //                 planeDict[hitPlane] = -2;
    //             }
    //         }
    //     }
    // }
}
