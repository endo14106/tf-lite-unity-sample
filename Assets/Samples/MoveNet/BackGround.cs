using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackGround : MonoBehaviour
{
    // Start is called before the first frame update
    public Text Back;
    public MoveNetSinglePoseSample move;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(move.achieve == 100)
        {
            Back.text = string.Format("■");
        }
    }
}

