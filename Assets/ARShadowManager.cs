using GoogleARCore;
using System.Collections.Generic;
using UnityEngine;

public class ARShadowManager : MonoBehaviour
{
	// [SerializeField] Material m_surfaceMaterial;
    public GameObject ShadowPlane;
	List<DetectedPlane> m_newPlanes = new List<DetectedPlane>();

	void Update()
	{
		if (Session.Status != SessionStatus.Tracking)
		{
			return;
		}

		Session.GetTrackables(m_newPlanes, TrackableQueryFilter.New);

		foreach (var plane in m_newPlanes)
		{
			// var surfaceObj = new GameObject("ARSurface");
			// var arSurface = surfaceObj.AddComponent<ARSurface>();
			// arSurface.SetTrackedPlane(plane, m_surfaceMaterial);

            GameObject planeObject = Instantiate(ShadowPlane, Vector3.zero, Quaternion.identity, transform);
            planeObject.GetComponent<ShadowPlane>().SetTrackedPlane(plane);
		}
	}
}
