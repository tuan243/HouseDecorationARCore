using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

public class CurrentObjectManager : MonoBehaviour
{
    // Start is called before the first frame update
    private List<Anchor> curObjects = new List<Anchor>();
    public static int maxObjectCount = 5;
    public void AddToCurObjects(Anchor anchor)
    {
        curObjects.Add(anchor);
    }

    public int CurObjectCount {
        get {
            List<DetectedPlane> detectedPlanes = new List<DetectedPlane>();
            Session.GetTrackables<DetectedPlane>(detectedPlanes, TrackableQueryFilter.All);

            int count = 0;
            foreach (DetectedPlane plane in detectedPlanes)
            {
                List<Anchor> anchors = new List<Anchor>();
                plane.GetAllAnchors(anchors);
                count += anchors.Count;
            }
            return count;
        }
    }
}
