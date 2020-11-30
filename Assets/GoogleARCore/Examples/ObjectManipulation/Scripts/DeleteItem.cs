using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore.Examples.ObjectManipulation;

public class DeleteItem : MonoBehaviour
{
    public void DeleteButtonClick()
    {
        if (ManipulationSystem.Instance.SelectedObject != null)
        {
            Destroy(ManipulationSystem.Instance.SelectedObject.transform.parent.gameObject);
            ManipulationSystem.Instance.Deselect();
        }
    }
}
