using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using UnityEngine.UI;

public class CurrentObjectManager : MonoBehaviour
{
    // Start is called before the first frame update
    private List<string> messageList = new List<string>() {
            "Reached maximum number of item. Try to remove some furniture.",
            "This furniture need to place on wall. Try to aim on the detected wall."
    };
    private List<Anchor> curObjects = new List<Anchor>();
    public static int maxObjectCount = 5;
    private int numberOfCoroutineRunning = 0;
    [SerializeField] private GameObject m_SnackBar = null;
    [SerializeField] private Text m_SnackBarText = null;
    private float snackBarShowTime = 3f;

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

    public bool CanPlaceMoreItem()
    {
        if (CurObjectCount >= maxObjectCount)
        {
            ShowSnackBarMessage(0);
            return false;
        }

        return true;
    }

    public void ShowSnackBarMessage(int messageType) 
    {
        IEnumerator coroutine = ShowSnackBar(messageType);
        StartCoroutine(coroutine);
    }

    IEnumerator ShowSnackBar(int messageType)
    {
        // Debug.Log("Coroutine ^-^");
        numberOfCoroutineRunning++;
        m_SnackBar.SetActive(true);
        m_SnackBarText.text = messageList[messageType];
        yield return new WaitForSeconds(3f);
        numberOfCoroutineRunning--;
        if (numberOfCoroutineRunning == 0)
        {
            m_SnackBar.SetActive(false);
        }
    }
}
