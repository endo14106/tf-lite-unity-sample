using System.Runtime.CompilerServices;
using System.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random=UnityEngine.Random;

public class GameControlScript : MonoBehaviour
{
    public Text TextFrame;
    public MoveNetSinglePoseSample move;
    //乱数パスワード
    private int pass;
    // Start is called before the first frame update
    void Start()
    {
        pass = Random.Range(100000, 999999);;
    }

    // Update is called once per frame
    void Update()
    {
        if(move.achieve >= 5){
            //TextFrame.text = string.Format("パスワード\n" + "{0}",move.achieve);
            TextFrame.text = string.Format("Congratulations!\n パスワード\n" + "{0}",pass);
        }
    }
}
