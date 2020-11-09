using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Openmenu : MonoBehaviour
{
    public GameObject Panel;
    public GameObject buttonA;

    public void OpenPanel()
    {
        if(Panel.activeSelf == true)
        {
            // Panel.SetActive(true);
            Animator animator = Panel.GetComponent<Animator>();
            if (animator != null )
            {
                bool isOpen = animator.GetBool("open");
                animator.SetBool("open", !isOpen);
            }
            Animator anibut = buttonA.GetComponent<Animator>();
            if (anibut != null)
            {
                bool isMove = anibut.GetBool("move");
                anibut.SetBool("move", !isMove);
            }
        }
    }
}

