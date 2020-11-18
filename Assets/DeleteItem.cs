using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore.Examples.ObjectManipulation;

public class DeleteItem : MonoBehaviour
{
    public void DeleteButtonClick()
    {
        Destroy(ManipulationSystem.Instance.SelectedObject.transform.parent.gameObject);
        ManipulationSystem.Instance.Deselect();
        Debug.Log("is selected object null ? " + (ManipulationSystem.Instance.SelectedObject == null).ToString());
    }
}
