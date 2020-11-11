using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

public class UpdateFloorOfTheHouse : MonoBehaviour
{

    public static float floorY = 0f;
    public static float ceilingY = 0f;
    public static DetectedPlane floorDetectedPlane;
    public static DetectedPlane ceilingDetectedPlane;

    public static HashSet<DetectedPlane> wallDetectedPlanes = new HashSet<DetectedPlane>();

    public static Dictionary<DetectedPlane, int> planeWithTypeDict = new Dictionary<DetectedPlane, int>();

    private List<DetectedPlane> m_NewPlanes = new List<DetectedPlane>();

    private float difThreshold = 0.05f;
    private float lowestPossibleFloor = -3f;
    private float minWallArea = 1f;

    void Update()
    {
        // Check that motion tracking is tracking.
        if (Session.Status == SessionStatus.LostTracking || Session.Status == SessionStatus.NotTracking)
        {
            floorY = 0;
            wallDetectedPlanes.Clear();
        }

        if (Session.Status != SessionStatus.Tracking)
        {
            return;
        }

        Session.GetTrackables<DetectedPlane>(m_NewPlanes, TrackableQueryFilter.Updated);
        for (int i = 0; i < m_NewPlanes.Count; i++)
        {
            if (m_NewPlanes[i].SubsumedBy != null || m_NewPlanes[i].CenterPose.position.y < lowestPossibleFloor)
            {
                continue;
            }

            if (isARPlaneFloor(m_NewPlanes[i]))
            {
                floorY = m_NewPlanes[i].CenterPose.position.y;
                floorDetectedPlane = m_NewPlanes[i];
            }
            else if (isARPlaneWall(m_NewPlanes[i]))
            {
                wallDetectedPlanes.Add(m_NewPlanes[i]);
            }
            else if (isARPlaneCeiling(m_NewPlanes[i]))
            {
                ceilingY = m_NewPlanes[i].CenterPose.position.y;
                ceilingDetectedPlane = m_NewPlanes[i];
            }
        }

        List<DetectedPlane> allPlanes = new List<DetectedPlane>();
        Session.GetTrackables<DetectedPlane>(allPlanes);
        foreach (var plane in allPlanes)
        {
            if (plane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing)
            {
                //-1: ground;
                //0: undefined;
                //1: dining table;
                int type = 0;
                if (Mathf.Abs(plane.CenterPose.position.y - floorY) < 0.3)
                {
                    type = -1;
                }
                else
                {
                    type = 0;
                }
                if (!planeWithTypeDict.ContainsKey(plane))
                {
                    planeWithTypeDict.Add(plane, type);
                }
                else if (planeWithTypeDict[plane] != 1)
                {
                    planeWithTypeDict[plane] = type;
                }

            }
        }

        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);

    }

    //Tìm gần đúng tọa độ y của sàn nhà
    bool isARPlaneFloor(DetectedPlane aRPlane)
    {
        if (aRPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing)
        {
            float planeY = aRPlane.CenterPose.position.y;
            if (floorY - planeY > difThreshold)
            {
                return true;
            }
            else if (Mathf.Abs(floorY - planeY) < difThreshold)
            {
                if (aRPlane.ExtentX * aRPlane.ExtentZ > floorDetectedPlane.ExtentX * floorDetectedPlane.ExtentZ)
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool isARPlaneWall(DetectedPlane aRPlane)
    {
        if (aRPlane.PlaneType == DetectedPlaneType.Vertical)
        {
            if (aRPlane.ExtentX * aRPlane.ExtentZ > minWallArea)
            {
                return true;
            }
        }
        return false;
    }

    bool isARPlaneCeiling(DetectedPlane aRPlane)
    {
        if (aRPlane.PlaneType == DetectedPlaneType.HorizontalDownwardFacing)
        {
            float planeY = aRPlane.CenterPose.position.y;
            if (planeY - ceilingY > difThreshold)
            {
                return true;
            }
            else if (Mathf.Abs(planeY - ceilingY) < difThreshold)
            {
                if (aRPlane.ExtentX * aRPlane.ExtentZ > ceilingDetectedPlane.ExtentX * ceilingDetectedPlane.ExtentZ)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
