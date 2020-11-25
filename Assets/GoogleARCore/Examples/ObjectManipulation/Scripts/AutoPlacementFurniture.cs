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
    //Auto đặt đồ phòng trống
    public void AutoPlaceCouchAndTable()
    {
        var sofa = objectStorage.allFurnitures[3].furnitures[1];
        var table = objectStorage.allFurnitures[4].furnitures[3];

        var camToItemVector = Vector3.Normalize(new Vector3(FirstPersonCamera.transform.forward.x,
                                0f,
                                FirstPersonCamera.transform.forward.z));
        var camToItemVectorPoint = 2f * camToItemVector + new Vector3(FirstPersonCamera.transform.position.x, 
                                                            UpdateFloorOfTheHouse.floorY, 
                                                            FirstPersonCamera.transform.position.z);
        var itemAlineVector = Vector3.Cross(camToItemVector, Vector3.up).normalized;

        var tablePosition = camToItemVectorPoint + 0.6f * itemAlineVector;
        var sofaPosition = camToItemVectorPoint - 0.6f * itemAlineVector;

        var tableRotation = Quaternion.LookRotation(-itemAlineVector);
        var sofaRotation = Quaternion.LookRotation(itemAlineVector);

        var walls = UpdateFloorOfTheHouse.wallDetectedPlanes;
        var projectedPoint = Vector3.zero;
        DetectedPlane nearWall = null;
        // bool nearWall = false;
        foreach (DetectedPlane wall in walls) {
            Plane wallPlane = new Plane(wall.CenterPose.rotation * Vector3.up, wall.CenterPose.position);
            if (wallPlane.GetDistanceToPoint(camToItemVectorPoint) < 2f)
            {
                var planeNormal = wall.CenterPose.rotation * Vector3.up;
                
                projectedPoint = ProjectPointToPlane(camToItemVectorPoint, wall.CenterPose.position, planeNormal);

                tablePosition = projectedPoint + planeNormal * 0.5f;
                sofaPosition = projectedPoint + planeNormal * 1.75f;

                tableRotation = Quaternion.LookRotation(planeNormal);
                sofaRotation = Quaternion.LookRotation(-planeNormal);

                nearWall = wall;
                break;
            }
        }

        var superPoints = objectDetectionInstance.SuperPoints;
        int i = 0;
        var chairLocation = Vector3.zero;
        bool nearChair = false;
        LinkedListNode<SuperPoint> pointNode; 
        for (pointNode = superPoints.First; pointNode != null; pointNode = pointNode.Next)
        {
            if ((pointNode.Value.loc - camToItemVectorPoint).sqrMagnitude < 3f)
            {
                var labelAndScoreSP = objectDetectionInstance.GetHighestScoreLabelOfSP(pointNode.Value);
                if (labelAndScoreSP.Item1 == 61 && labelAndScoreSP.Item2 > 0.7f) //if label is chair
                {
                    if (i == 0)
                    {
                        chairLocation = pointNode.Value.loc;
                    }
                    else 
                    {
                        chairLocation = (chairLocation + pointNode.Value.loc / i) * ((float)i / i + 1);
                    }

                    nearChair = true;
                    i++;
                } 
            }
        }

        chairLocation = new Vector3(chairLocation.x, UpdateFloorOfTheHouse.floorY, chairLocation.z);

        // if (nearChair)
        // {
        //     if (nearWall != null)
        //     {
        //         var wallNormal = nearWall.CenterPose.rotation * Vector3.up;
        //         var chairProjectToWallPoint = ProjectPointToPlane(chairLocation, nearWall.CenterPose.position, wallNormal);
        //         tablePosition = chairProjectToWallPoint + wallNormal * 0.5f;
        //         tableRotation = Quaternion.LookRotation(wallNormal);
        //     }
        //     else
        //     {
        //         tablePosition = chairLocation + itemAlineVector * 0.5f;
        //         tableRotation = Quaternion.LookRotation(-itemAlineVector);
        //     }
        // }


        if (!nearChair)
        {
            var sofaPose = new Pose(sofaPosition, sofaRotation);
            var sofaObject = Instantiate(sofa, sofaPose.position, sofaPose.rotation);
            var sofaManipulator = Instantiate(ManipulatorPrefab, sofaPose.position, sofaPose.rotation);
            sofaObject.transform.parent = sofaManipulator.transform;
            var sofaAnchor = UpdateFloorOfTheHouse.floorDetectedPlane.CreateAnchor(sofaPose);
            sofaManipulator.transform.parent = sofaAnchor.transform;
        }
        var tablePose = new Pose(tablePosition, tableRotation);
        var tableObject = Instantiate(table, tablePose.position, tablePose.rotation);

        var tableManipulator = Instantiate(ManipulatorPrefab, tablePose.position, tablePose.rotation);
        tableObject.transform.parent = tableManipulator.transform;
        var tableAnchor = UpdateFloorOfTheHouse.floorDetectedPlane.CreateAnchor(tablePose);
        
        tableManipulator.transform.parent = tableAnchor.transform;
    }
    public void AIButtonClicked() 
    {
        Debug.Log("AI is running!!! :)");
    }
}
