using System.Runtime.CompilerServices;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CountAchieve : MonoBehaviour
{
    // Start is called before the first frame update
    public Text Achieve;
    public MoveNetSinglePoseSample move;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update(){
        Achieve.text = string.Format("{0} / 5回", move.achieve);
        if(move.achieve == 5)
        {
            Achieve.enabled = false;
        }
    }
}
