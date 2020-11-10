using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class EventTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        SlideUpPanel.wasClickedOnUI = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SlideUpPanel.wasClickedOnUI = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
