using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using GoogleARCore.Examples.ObjectManipulation;

public class AutoPlacementFurniture : MonoBehaviour
{
    //Nếu diện tích của plane lớn hơn số này thì nó là bàn
    // float tableAreaThres = 1f;
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

    public void TryToPutLivingRoomFurniture(Vector3 lookingVector, Vector3 lookingPoint, TrackableHit hit = new TrackableHit())
    {
        var sofa = objectStorage.allFurnitures[4].furnitures[2];
        var table = objectStorage.allFurnitures[5].furnitures[6];
        var pottedPlant = objectStorage.allFurnitures[6].furnitures[8];
        var shell = objectStorage.allFurnitures[3].furnitures[0];
        
        var itemAlineVector = Vector3.Cross(lookingVector, Vector3.up).normalized;

        DetectedPlane anchorPlane;
        if (hit.Trackable != null)
        {
            anchorPlane = hit.Trackable as DetectedPlane;
        }
        else
        {
            Debug.Log("No Detected Plane hit!");
            anchorPlane = UpdateFloorOfTheHouse.floorDetectedPlane;
        }

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

                InstantiateFurniture(table, new Pose(tablePosition, tableRotation), anchorPlane);
                InstantiateFurniture(sofa, new Pose(sofaPosition, sofaRotation), anchorPlane);
                InstantiateFurniture(pottedPlant, new Pose(pottedPlantPosition, pottedPlantRotation), anchorPlane);
                InstantiateFurniture(shell, new Pose(shellPosition, shellRotation), anchorPlane);

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
        pottedPlantPosition = lookingPoint + 0.8f * leftVector - 0.6f * lookingVector;
        shellPosition = lookingPoint + 0.75f * leftVector + 1f * lookingVector;

        tableRotation = Quaternion.LookRotation(leftVector);
        sofaRotation = Quaternion.LookRotation(-leftVector);
        pottedPlantRotation = Quaternion.LookRotation(-leftVector);
        shellRotation = Quaternion.LookRotation(-leftVector);

        InstantiateFurniture(table, new Pose(tablePosition, tableRotation), anchorPlane);
        InstantiateFurniture(sofa, new Pose(sofaPosition, sofaRotation), anchorPlane);
        InstantiateFurniture(pottedPlant, new Pose(pottedPlantPosition, pottedPlantRotation), anchorPlane);
        InstantiateFurniture(shell, new Pose(shellPosition, shellRotation), anchorPlane);
    }

    bool TryToPutFlowerOnTheTable(Vector3 tableLocation, Vector3 lookVector)
    {
        // List<TrackableHit> hitResults = new List<TrackableHit>();
        TrackableHit hit = new TrackableHit();
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

        Vector3 raycastPoint = new Vector3(tableLocation.x, 0.3f, tableLocation.z);

        if (Frame.Raycast(raycastPoint, Vector3.down, out hit, 2.2f, raycastFilter))
        {
            if ((hit.Trackable is DetectedPlane) &&
                Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                    hit.Pose.rotation * Vector3.up) < 0)
            {
                Debug.Log("Hit at back of the current DetectedPlane");
                return false;
            }

            DetectedPlane hitPlane = hit.Trackable as DetectedPlane;

            if (Mathf.Abs(hitPlane.CenterPose.position.y - UpdateFloorOfTheHouse.floorY) < 0.1f) //plane is ground
            {
                Debug.Log("I don't put flower on the ground!");
                return false;
            }

            var itemPose = new Pose(hit.Pose.position, Quaternion.identity);
            var vaseCategoryIndex = objectStorage.allFurnitures.Count - 2;
            var flower = objectStorage.allFurnitures[vaseCategoryIndex].furnitures[0];

            InstantiateFurniture(flower, itemPose, hitPlane);

            TryToPutChairNearTable(hitPlane, lookVector);
            return true;
        }

        return false; //failed putting flower on the table :()
    }

    void TryToPutChairNearTable(DetectedPlane table, Vector3 lookVector)
    {
        List<Vector3> tableBoundPoints = new List<Vector3>();
        table.GetBoundaryPolygon(tableBoundPoints);

        if (tableBoundPoints.Count > 0)
        {
            Vector3 nearestPoint = tableBoundPoints[0];
            foreach(var point in tableBoundPoints)
            {
                if ((point - FirstPersonCamera.transform.position).sqrMagnitude <
                      (nearestPoint - FirstPersonCamera.transform.position).sqrMagnitude)
                {
                    nearestPoint = point;
                }
            }

            // nearestPoint = new Vector3(nearestPoint.x, UpdateFloorOfTheHouse.floorY, nearestPoint.z);
            nearestPoint += -0.2f * lookVector;

            TrackableHit hit = new TrackableHit();

            if (Frame.Raycast(nearestPoint, Vector3.down, out hit, 2f))
            {
                DetectedPlane hitPlane = hit.Trackable as DetectedPlane;
                Pose chairPose = new Pose(hit.Pose.position, Quaternion.LookRotation(lookVector));

                InstantiateFurniture(objectStorage.allFurnitures[1].furnitures[2], chairPose, hitPlane);
            }
            else 
            {
                Pose chairPose = new Pose(new Vector3(nearestPoint.x, UpdateFloorOfTheHouse.floorY, nearestPoint.z),
                                            Quaternion.LookRotation(lookVector));
                InstantiateFurniture(objectStorage.allFurnitures[1].furnitures[2], chairPose, UpdateFloorOfTheHouse.floorDetectedPlane);
            }
        }
    }

    public void AIButtonClicked()
    {
        var camToItemVector = Vector3.Normalize(new Vector3(FirstPersonCamera.transform.forward.x,
                                        0f,
                                        FirstPersonCamera.transform.forward.z));
        var camToItemVectorPoint = 2f * camToItemVector + new Vector3(FirstPersonCamera.transform.position.x,
                                                            UpdateFloorOfTheHouse.floorY,
                                                            FirstPersonCamera.transform.position.z);

        Vector3 tableLocation;
        if (IsItemNearBy(camToItemVectorPoint, 66, out tableLocation)) //66: dining table
        {
            if (TryToPutFlowerOnTheTable(tableLocation, camToItemVector))
            {
                return;
            }
        }

        TrackableHit hit = new TrackableHit();
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;
        if (Frame.Raycast(new Vector3(camToItemVectorPoint.x, 0.3f, camToItemVectorPoint.z), 
                                Vector3.down, out hit, 2f, raycastFilter))
        {
            if ((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0)
            {
                Debug.Log("Hit at back of the current DetectedPlane");
            }
            else 
            {
                DetectedPlane hitPlane = hit.Trackable as DetectedPlane;

                // if (Mathf.Abs(hitPlane.CenterPose.position.y - UpdateFloorOfTheHouse.floorY) < 0.2f) //plane is ground
                // {
                //     Debug.Log("No item in front of me!");
                //     camToItemVectorPoint.Set(camToItemVectorPoint.x, hit.Pose.position.y, camToItemVectorPoint.z);
                //     TryToPutLivingRoomFurniture(camToItemVector, camToItemVectorPoint, hit);
                // }
                // else
                // {
                //     Debug.Log("Not hit the ground!");
                // }
                camToItemVectorPoint.Set(camToItemVectorPoint.x, hit.Pose.position.y, camToItemVectorPoint.z);
                TryToPutLivingRoomFurniture(camToItemVector, camToItemVectorPoint, hit);
            }
        }
        else
        {
            TryToPutLivingRoomFurniture(camToItemVector, camToItemVectorPoint);
        }
    }

    void InstantiateFurniture(GameObject objectPrefab, Pose pose, DetectedPlane arPlane)
    {
        if (arPlane == null)
        {
            return;
        }
        var anchor = arPlane.CreateAnchor(pose);
        var manipulator = Instantiate(ManipulatorPrefab, pose.position, pose.rotation, anchor.transform);
        var gameObject = Instantiate(objectPrefab, pose.position, pose.rotation, manipulator.transform);

        manipulator.GetComponent<Manipulator>().Select();
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
