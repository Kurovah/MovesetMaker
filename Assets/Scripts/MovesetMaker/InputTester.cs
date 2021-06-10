using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputTester : MonoBehaviour
{
    public InputHandler ih;
    public RectTransform knobRect;
    public Text bufferText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        knobRect.anchoredPosition = ih.moveAxis.normalized * 40;


        bufferText.text = "";
        int ind = 0;
        foreach (InputTypes i in ih.buffer)
        {
            bufferText.text += ind + i.ToString() + "\n";
            ind++;
        }
    }
}
