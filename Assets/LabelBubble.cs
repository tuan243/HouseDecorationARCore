using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LabelBubble : MonoBehaviour
{
    public Text bubbleText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetText(string text) 
    {
        bubbleText.text = text;
    } 
}
