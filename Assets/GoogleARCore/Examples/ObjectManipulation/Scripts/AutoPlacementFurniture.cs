using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using GoogleARCore.Examples.ObjectManipulation;

public class AutoPlacementFurniture : MonoBehaviour
{
    //Nếu diện tích của plane lớn hơn số này thì nó là bàn
    float tableAreaThres = 1f;
    public Camera FirstPersonCamera;
    public GameObject ManipulatorPrefab;
    private ObjectStorage objectStorage;
    private ObjectDetection objectDetectionInstance;

    void Awake()
    {
        objectStorage = GameObject.Find("/ObjectStorage").GetComponent<ObjectStorage>();
        objectDetectionInstance = GameObject.Find("/SSD").GetComponent<ObjectDetection>();
    }

    Vector3 ProjectPointToPlane(Vector3 projectPoint, Vector3 pointInPlane, Vector3 planeNormal)
    {
        Vector3 v = projectPoint - pointInPlane;
        Vector3 d = Vector3.Project(v, planeNormal);
        return projectPoint - d;
    }

    public void TryToPutLivingRoomFurniture(Vector3 lookingVector, Vector3 lookingPoint)
    {
        var sofa = objectStorage.allFurnitures[4].furnitures[2];
        var table = objectStorage.allFurnitures[5].furnitures[6];
        var pottedPlant = objectStorage.allFurnitures[6].furnitures[8];
        var shell = objectStorage.allFurnitures[3].furnitures[0];
        
        var itemAlineVector = Vector3.Cross(lookingVector, Vector3.up).normalized;

        // var tablePosition = lookingPoint + 0.6f * itemAlineVector;
        // var sofaPosition = lookingPoint - 0.6f * itemAlineVector;

        // var tableRotation = Quaternion.LookRotation(-itemAlineVector);
        // var sofaRotation = Quaternion.LookRotation(itemAlineVector);
        Vector3 tablePosition;
        Vector3 sofaPosition;
        Vector3 pottedPlantPosition;
        Vector3 shellPosition;

        Quaternion tableRotation;
        Quaternion sofaRotation;
        Quaternion pottedPlantRotation;
        Quaternion shellRotation;

        var walls = UpdateFloorOfTheHouse.wallDetectedPlanes;
        var projectedPoint = Vector3.zero;
        foreach (DetectedPlane wall in walls)
        {
            Plane wallPlane = new Plane(wall.CenterPose.rotation * Vector3.up, wall.CenterPose.position);
            if (wallPlane.GetDistanceToPoint(lookingPoint) < 2f)
            {
                var planeNormal = wall.CenterPose.rotation * Vector3.up;

                projectedPoint = ProjectPointToPlane(lookingPoint, wall.CenterPose.position, planeNormal);
                var alignVector = Vector3.Cross(planeNormal, Vector3.up);//parallel to wall

                tablePosition = projectedPoint + planeNormal * 1.75f;
                sofaPosition = projectedPoint + planeNormal * 0.25f;
                pottedPlantPosition = sofaPosition - 0.5f * alignVector; //right of sofa
                shellPosition = sofaPosition + 1f * alignVector; //left of sofa

                tableRotation = Quaternion.LookRotation(planeNormal);
                sofaRotation = Quaternion.LookRotation(planeNormal);
                pottedPlantRotation = Quaternion.identity;
                shellRotation = Quaternion.LookRotation(planeNormal);

                InstantiateFurniture(table, new Pose(tablePosition, tableRotation), UpdateFloorOfTheHouse.floorDetectedPlane);
                InstantiateFurniture(sofa, new Pose(sofaPosition, sofaRotation), UpdateFloorOfTheHouse.floorDetectedPlane);
                InstantiateFurniture(pottedPlant, new Pose(pottedPlantPosition, pottedPlantRotation), UpdateFloorOfTheHouse.floorDetectedPlane);
                InstantiateFurniture(shell, new Pose(shellPosition, shellRotation), UpdateFloorOfTheHouse.floorDetectedPlane);

                var pictureFrame = objectStorage.allFurnitures[7].furnitures[0];
                var pictureFramePostion = projectedPoint + 1.7f * Vector3.up;
                var pictureFrameRotation = Quaternion.LookRotation(Vector3.up, planeNormal);
                var pictureFramePose = new Pose(pictureFramePostion, pictureFrameRotation);
                InstantiateFurniture(pictureFrame, pictureFramePose, wall);

                return;
            }
        }

        var leftVector = Vector3.Cross(lookingVector, Vector3.up);
        tablePosition = lookingPoint - 0.6f * leftVector;
        sofaPosition = lookingPoint + 0.6f * leftVector;
        pottedPlantPosition = sofaPosition - 0.5f * lookingVector;
        shellPosition = sofaPosition + 1f * lookingVector;

        tableRotation = Quaternion.LookRotation(leftVector);
        sofaRotation = Quaternion.LookRotation(-leftVector);
        pottedPlantRotation = Quaternion.LookRotation(-leftVector);
        shellRotation = Quaternion.LookRotation(-leftVector);

        InstantiateFurniture(table, new Pose(tablePosition, tableRotation), UpdateFloorOfTheHouse.floorDetectedPlane);
        InstantiateFurniture(sofa, new Pose(sofaPosition, sofaRotation), UpdateFloorOfTheHouse.floorDetectedPlane);
        InstantiateFurniture(pottedPlant, new Pose(pottedPlantPosition, pottedPlantRotation), UpdateFloorOfTheHouse.floorDetectedPlane);
        InstantiateFurniture(shell, new Pose(shellPosition, shellRotation), UpdateFloorOfTheHouse.floorDetectedPlane);
        // var superPoints = objectDetectionInstance.SuperPoints;
        // int i = 0;
        // var chairLocation = Vector3.zero;
        // bool nearChair = false;
        // LinkedListNode<SuperPoint> pointNode;
        // for (pointNode = superPoints.First; pointNode != null; pointNode = pointNode.Next)
        // {
        //     if ((pointNode.Value.loc - camToItemVectorPoint).sqrMagnitude < 3f)
        //     {
        //         var labelAndScoreSP = objectDetectionInstance.GetHighestScoreLabelOfSP(pointNode.Value);
        //         if (labelAndScoreSP.Item1 == 61 && labelAndScoreSP.Item2 > 0.7f) //if label is chair
        //         {
        //             // if (i == 0)
        //             // {
        //             //     chairLocation = pointNode.Value.loc;
        //             // }
        //             // else
        //             // {
        //             //     chairLocation = (chairLocation + pointNode.Value.loc / i) * ((float)i / (i + 1));
        //             // }
        //             chairLocation += pointNode.Value.loc;

        //             nearChair = true;
        //             i++;
        //         }
        //     }
        // }

        // if (i != 0)
        // {
        //     chairLocation = chairLocation / i;
        // }

        // chairLocation = new Vector3(chairLocation.x, UpdateFloorOfTheHouse.floorY, chairLocation.z);

        


        // if (!nearChair)
        // {
        //     var sofaPose = new Pose(sofaPosition, sofaRotation);
        //     var sofaObject = Instantiate(sofa, sofaPose.position, sofaPose.rotation);
        //     var sofaManipulator = Instantiate(ManipulatorPrefab, sofaPose.position, sofaPose.rotation);
        //     sofaObject.transform.parent = sofaManipulator.transform;
        //     var sofaAnchor = UpdateFloorOfTheHouse.floorDetectedPlane.CreateAnchor(sofaPose);
        //     sofaManipulator.transform.parent = sofaAnchor.transform;
        // }
        // var tablePose = new Pose(tablePosition, tableRotation);
        // var tableObject = Instantiate(table, tablePose.position, tablePose.rotation);

        // var tableManipulator = Instantiate(ManipulatorPrefab, tablePose.position, tablePose.rotation);
        // tableObject.transform.parent = tableManipulator.transform;
        // var tableAnchor = UpdateFloorOfTheHouse.floorDetectedPlane.CreateAnchor(tablePose);

        // tableManipulator.transform.parent = tableAnchor.transform;
    }

    void TryToPutFlowerOnTheTable(Vector3 tableLocation)
    {
        List<TrackableHit> hitResults = new List<TrackableHit>();
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

        Vector3 raycastPoint = new Vector3(tableLocation.x, 0.3f, tableLocation.z);

        if (Frame.RaycastAll(raycastPoint, Vector3.down, hitResults, 2.2f, raycastFilter))
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

                if (hitPlane.PlaneType != DetectedPlaneType.HorizontalUpwardFacing) 
                {
                    continue;
                }

                if (Mathf.Abs(hitPlane.CenterPose.position.y - UpdateFloorOfTheHouse.floorY) < 0.1f) //plane is ground
                {
                    Debug.Log("I don't put flower on the ground!");
                    return;
                }

                var itemPose = new Pose(hit.Pose.position, Quaternion.identity);
                var vaseCategoryIndex = objectStorage.allFurnitures.Count - 2;
                var flower = objectStorage.allFurnitures[vaseCategoryIndex].furnitures[0];

                InstantiateFurniture(flower, itemPose, hitPlane);
                return;
            }
        }
    }

    void InstantiateFurniture(GameObject objectPrefab, Pose pose, DetectedPlane arPlane)
    {
        Debug.Log($"Floor detected plane is null? {(UpdateFloorOfTheHouse.floorDetectedPlane == null).ToString()}");
        if (arPlane == null)
        {
            return;
        }
        var anchor = arPlane.CreateAnchor(pose);
        var manipulator = Instantiate(ManipulatorPrefab, pose.position, pose.rotation, anchor.transform);
        var gameObject = Instantiate(objectPrefab, pose.position, pose.rotation, manipulator.transform);

        manipulator.GetComponent<Manipulator>().Select();
    }
    public void AIButtonClicked()
    {
        var camToItemVector = Vector3.Normalize(new Vector3(FirstPersonCamera.transform.forward.x,
                                        0f,
                                        FirstPersonCamera.transform.forward.z));
        var camToItemVectorPoint = 2f * camToItemVector + new Vector3(FirstPersonCamera.transform.position.x,
                                                            UpdateFloorOfTheHouse.floorY,
                                                            FirstPersonCamera.transform.position.z);
        // var itemRotation = Quaternion.LookRotation(-camToItemVector); // hướng đồ vật vào mặt mình

        Vector3 tableLocation;
        if (IsItemNearBy(camToItemVectorPoint, 66, out tableLocation)) //66: dining table
        {
            TryToPutFlowerOnTheTable(tableLocation);
            return;
        }

        TrackableHit hit = new TrackableHit();
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;
        if (Frame.Raycast(new Vector3(camToItemVectorPoint.x, 0.3f, camToItemVectorPoint.z), 
                                Vector3.down, out hit, 2f, raycastFilter))
        {
            Debug.Log("yeah hitted");
            if ((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0)
            {
                Debug.Log("Hit at back of the current DetectedPlane");
            }
            else 
            {
                DetectedPlane hitPlane = hit.Trackable as DetectedPlane;

                if (Mathf.Abs(hitPlane.CenterPose.position.y - UpdateFloorOfTheHouse.floorY) < 0.2f) //plane is ground
                {
                    Debug.Log("No item in front of me!");
                    TryToPutLivingRoomFurniture(camToItemVector, camToItemVectorPoint);
                }
                else
                {
                    Debug.Log("Not hit the ground!");
                }

            }
        }
        else
        {
            TryToPutLivingRoomFurniture(camToItemVector, camToItemVectorPoint);
        }
    }

    bool IsItemNearBy(Vector3 point, int itemLabel, out Vector3 itemLocation, float minScore = 0f)
    {
        var superPoints = objectDetectionInstance.SuperPoints;
        int i = 0;
        itemLocation = Vector3.zero;
        bool nearItem = false;
        LinkedListNode<SuperPoint> pointNode;
        for (pointNode = superPoints.First; pointNode != null; pointNode = pointNode.Next)
        {
            if ((pointNode.Value.loc - point).magnitude < 2f)
            {
                var labelAndScoreSP = objectDetectionInstance.GetHighestScoreLabelOfSP(pointNode.Value);
                if (labelAndScoreSP.Item1 == 66 && labelAndScoreSP.Item2 > minScore) //if label is table
                {
                    // if (j == 0)
                    // {
                    //     tableLocation = pointNode.Value.loc;
                    // }
                    // else
                    // {
                    //     tableLocation = (tableLocation + pointNode.Value.loc / j) * ((float)j / (j + 1));
                    // }
                    itemLocation += pointNode.Value.loc;

                    nearItem = true;
                    i++;
                }
            }
        }

        if (i != 0)
        {
            itemLocation = itemLocation / i;
        }
        
        return nearItem;
    }
}
